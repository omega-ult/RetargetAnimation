using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

[CustomEditor(typeof(RetargetingAnimationController))]
public class RetargetingAnimationControllerEditor : Editor
{
    private SerializedProperty sourceAnimatorProperty;
    private SerializedProperty blendAnimatorProperty;
    private SerializedProperty destinationAnimatorProperty;
    private SerializedProperty boneMappingAssetProperty;
    private SerializedProperty useAvatarMaskProperty;
    private SerializedProperty avatarMaskProperty;
    private SerializedProperty autoSetupBoneMappingsProperty;

    private void OnEnable()
    {
        sourceAnimatorProperty = serializedObject.FindProperty("sourceAnimator");
        blendAnimatorProperty = serializedObject.FindProperty("blendAnimator");
        destinationAnimatorProperty = serializedObject.FindProperty("destinationAnimator");
        boneMappingAssetProperty = serializedObject.FindProperty("boneMappingAsset");
        useAvatarMaskProperty = serializedObject.FindProperty("useAvatarMask");
        avatarMaskProperty = serializedObject.FindProperty("avatarMask");
        autoSetupBoneMappingsProperty = serializedObject.FindProperty("autoSetupBoneMappings");

    }
    private void AutoSetupBoneMappings()
    {
        RetargetingAnimationController controller = (RetargetingAnimationController)target;

        // Get the source and target animators
        var sourceAnimator = sourceAnimatorProperty.objectReferenceValue as Animator;
        var targetAnimator = blendAnimatorProperty.objectReferenceValue as Animator;

        if (sourceAnimator == null || targetAnimator == null)
        {
            EditorUtility.DisplayDialog("Error", "Source and Target Animators must be assigned first!", "OK");
            return;
        }

        // Get all transforms from source and target
        Dictionary<string, Transform> sourceBones = new Dictionary<string, Transform>();
        Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>();

        CollectBones(sourceAnimator.transform, sourceBones);
        CollectBones(targetAnimator.transform, targetBones);

        // // Create mappings for bones with matching names
        // foreach (var sourceEntry in sourceBones)
        // {
        //     if (targetBones.TryGetValue(sourceEntry.Key, out Transform targetTransform))
        //     {
        //         // Add a new mapping
        //         // BoneMapping mapping = new BoneMapping
        //         // {
        //         //     sourceTransform = sourceEntry.Value,
        //         //     targetTransform = targetTransform,
        //         //     applyPosition = true,
        //         //     applyRotation = true,
        //         //     applyScale = false
        //         // };
        //
        //         // tempBoneMappings.Add(mapping);
        //     }
        // }

        // // 应用更改到资产
        // ApplyChangesToAsset();

        // EditorUtility.DisplayDialog("Auto-Setup Complete",
        //     $"Created {tempBoneMappings.Count} bone mappings based on matching names.", "OK");
    }

    private void CollectBones(Transform root, Dictionary<string, Transform> boneDict)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            // Add this bone to the dictionary
            string boneName = current.name;
            if (!boneDict.ContainsKey(boneName))
            {
                boneDict.Add(boneName, current);
            }

            // Add all children to the queue
            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animator References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sourceAnimatorProperty);
        EditorGUILayout.PropertyField(blendAnimatorProperty);
        EditorGUILayout.PropertyField(destinationAnimatorProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bone Mapping Asset", EditorStyles.boldLabel);

        // 显示BoneMappingAsset字段
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(boneMappingAssetProperty);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            // UpdateTempBoneMappings();
            // SetupBoneMappingsList();
        }

        // 添加创建新资产的按钮
        if (boneMappingAssetProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("请分配或创建一个骨骼映射资产。", MessageType.Info);

            if (GUILayout.Button("创建新的骨骼映射资产"))
            {
                // 创建保存对话框
                string path = EditorUtility.SaveFilePanelInProject(
                    "创建骨骼映射资产",
                    "New Bone Mapping Asset",
                    "asset",
                    "请指定骨骼映射资产的保存位置");

                if (!string.IsNullOrEmpty(path))
                {
                    // 创建新资产
                    BoneMappingAsset asset = ScriptableObject.CreateInstance<BoneMappingAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();

                    // 分配到属性
                    boneMappingAssetProperty.objectReferenceValue = asset;
                    serializedObject.ApplyModifiedProperties();

                    // 更新临时列表和ReorderableList
                    // UpdateTempBoneMappings();
                    // SetupBoneMappingsList();
                }
            }
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bone Mapping Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoSetupBoneMappingsProperty);

            EditorGUILayout.Space();

            // Add buttons for auto-setup and clear
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Setup Mappings"))
            {
                if (EditorUtility.DisplayDialog("Auto-Setup Bone Mappings",
                        "This will clear existing mappings and create new ones based on matching bone names. Continue?",
                        "Yes", "Cancel"))
                {
                    AutoSetupBoneMappings();
                }
            }

            // if (GUILayout.Button("Clear All Mappings"))
            // {
            //     if (EditorUtility.DisplayDialog("Clear Bone Mappings",
            //             "Are you sure you want to clear all bone mappings?",
            //             "Yes", "Cancel"))
            //     {
            //         // tempBoneMappings.Clear();
            //         ApplyChangesToAsset();
            //         // boneMappingsList.ClearSelection();
            //     }
            // }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            // boneMappingsList.DoLayoutList();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Avatar Mask Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useAvatarMaskProperty);

        if (useAvatarMaskProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(avatarMaskProperty);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}