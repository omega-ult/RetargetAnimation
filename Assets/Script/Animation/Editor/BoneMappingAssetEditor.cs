using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(BoneMappingAsset))]
public class BoneMappingAssetEditor : Editor
{
    private SerializedProperty boneMappingsProperty;
    private ReorderableList boneMappingsList;
    
    private void OnEnable()
    {
        boneMappingsProperty = serializedObject.FindProperty("boneMappings");
        SetupBoneMappingsList();
    }
    
    private void SetupBoneMappingsList()
    {
        boneMappingsList = new ReorderableList(serializedObject, boneMappingsProperty, true, true, true, true);
        
        boneMappingsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "骨骼映射列表");
        };
        
        boneMappingsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = boneMappingsProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            // 获取所有需要的属性
            var sourcePath = element.FindPropertyRelative("sourcePath");
            var targetPath = element.FindPropertyRelative("targetPath");
            var matchChildren = element.FindPropertyRelative("matchChildren");
            var applyPosition = element.FindPropertyRelative("applyPosition");
            var applyRotation = element.FindPropertyRelative("applyRotation");
            var applyScale = element.FindPropertyRelative("applyScale");
            
            // 源骨骼 - 第一行
            float labelWidth = 60f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), "源骨骼");
            sourcePath.stringValue = EditorGUI.TextField(
                new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height),
                sourcePath.stringValue);
            
            // 目标骨骼 - 第二行
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), "目标骨骼");
            targetPath.stringValue = EditorGUI.TextField(
                new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height),
                targetPath.stringValue);
            
            // 匹配子骨骼 - 第三行
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            matchChildren.boolValue = EditorGUI.Toggle(
                new Rect(rect.x, rect.y, rect.width, rect.height),
                "匹配子骨骼", matchChildren.boolValue);
            
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            // 变换选项 - 第四行
            float toggleWidth = rect.width / 8f;
            var transformRect = new Rect(rect.x, rect.y, toggleWidth, rect.height);
            
            applyPosition.boolValue = EditorGUI.Toggle(
                transformRect,
                "位置", applyPosition.boolValue);
                
            transformRect.x += toggleWidth;
            applyRotation.boolValue = EditorGUI.Toggle(
                transformRect,
                "旋转", applyRotation.boolValue);
                
            transformRect.x += toggleWidth;
            applyScale.boolValue = EditorGUI.Toggle(
                transformRect,
                "缩放", applyScale.boolValue);
        };
        
        boneMappingsList.elementHeightCallback = (int index) =>
        {
            // 更新元素高度以适应四行内容
            return EditorGUIUtility.singleLineHeight * 4 + 8;
        };
        
        boneMappingsList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("sourcePath").stringValue = null;
            element.FindPropertyRelative("targetPath").stringValue = null;
            element.FindPropertyRelative("applyPosition").boolValue = true;
            element.FindPropertyRelative("applyRotation").boolValue = true;
            element.FindPropertyRelative("applyScale").boolValue = false;
            element.FindPropertyRelative("matchChildren").boolValue = true;
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("骨骼映射资产", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // 添加自动设置和清除的按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清除所有映射"))
        {
            if (EditorUtility.DisplayDialog("清除骨骼映射", 
                "确定要清除所有骨骼映射吗？", 
                "是", "取消"))
            {
                boneMappingsProperty.ClearArray();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        boneMappingsList.DoLayoutList();
        
        serializedObject.ApplyModifiedProperties();
    }
}