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
            
            // 计算每个属性的矩形区域
            float widthPerField = rect.width / 4f - 5f;
            
            Rect sourceRect = new Rect(rect.x, rect.y, widthPerField/2, rect.height);
            Rect targetRect = new Rect(rect.x + widthPerField + 5f, rect.y, widthPerField, rect.height);
            Rect matchRect = new Rect(rect.x + 2 * (widthPerField + 5f), rect.y, widthPerField, rect.height);
            
            EditorGUI.PropertyField(sourceRect, element.FindPropertyRelative("sourcePath"), new GUIContent("源骨骼"));
            EditorGUI.PropertyField(targetRect, element.FindPropertyRelative("targetPath"), new GUIContent("目标骨骼"));
            EditorGUI.PropertyField(matchRect, element.FindPropertyRelative("matchChildren"), new GUIContent("匹配子骨骼"));
            
            // 在下一行绘制变换选项
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            
            Rect posRect = new Rect(rect.x, rect.y, widthPerField, rect.height);
            Rect rotRect = new Rect(rect.x + widthPerField + 5f, rect.y, widthPerField, rect.height);
            Rect scaleRect = new Rect(rect.x + 2 * (widthPerField + 5f), rect.y, widthPerField, rect.height);
            
            EditorGUI.PropertyField(posRect, element.FindPropertyRelative("applyPosition"), new GUIContent("位置"));
            EditorGUI.PropertyField(rotRect, element.FindPropertyRelative("applyRotation"), new GUIContent("旋转"));
            EditorGUI.PropertyField(scaleRect, element.FindPropertyRelative("applyScale"), new GUIContent("缩放"));
        };
        
        boneMappingsList.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight * 2 + 6;
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