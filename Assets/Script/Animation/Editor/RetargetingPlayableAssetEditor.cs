using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

[CustomEditor(typeof(BoneMappingAsset))]
public class RetargetingPlayableAssetEditor : Editor
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
            EditorGUI.LabelField(rect, "Bone Mappings");
        };
        
        boneMappingsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = boneMappingsProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            // Calculate rects for each property
            float labelWidth = 60f;
            float fieldWidth = (rect.width - labelWidth) / 2f - 5f;
            
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Rect sourceRect = new Rect(rect.x + labelWidth, rect.y, fieldWidth, rect.height);
            Rect targetRect = new Rect(rect.x + labelWidth + fieldWidth + 10f, rect.y, fieldWidth, rect.height);
            
            EditorGUI.LabelField(labelRect, "Match " + index);
            EditorGUI.PropertyField(sourceRect, element.FindPropertyRelative("sourcePath"), GUIContent.none);
            EditorGUI.PropertyField(targetRect, element.FindPropertyRelative("targetPath"), GUIContent.none);
            
            // Draw transform options on the next line
            rect.y += EditorGUIUtility.singleLineHeight + 2;
            
            Rect posRect = new Rect(rect.x, rect.y, rect.width / 3f - 5f, rect.height);
            Rect rotRect = new Rect(rect.x + rect.width / 3f, rect.y, rect.width / 3f - 5f, rect.height);
            Rect scaleRect = new Rect(rect.x + 2f * rect.width / 3f, rect.y, rect.width / 3f - 5f, rect.height);
            
            EditorGUI.PropertyField(posRect, element.FindPropertyRelative("applyPosition"), new GUIContent("Position"));
            EditorGUI.PropertyField(rotRect, element.FindPropertyRelative("applyRotation"), new GUIContent("Rotation"));
            EditorGUI.PropertyField(scaleRect, element.FindPropertyRelative("applyScale"), new GUIContent("Scale"));
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
            element.FindPropertyRelative("sourcePath").stringValue = "";
            element.FindPropertyRelative("targetPath").stringValue = "";
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
        boneMappingsList.DoLayoutList();
        
        EditorGUILayout.Space();
        
        serializedObject.ApplyModifiedProperties();
    }
}