using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 示例脚本，展示如何使用动画重定向系统
/// </summary>
public class RetargetingExample : MonoBehaviour
{
    [Header("角色引用")]
    [Tooltip("模板角色A，用于制作动画")]
    [SerializeField] private Animator templateCharacter;
    
    [Tooltip("目标角色B，将接收重定向的动画")]
    [SerializeField] private Animator targetCharacter;
    
    [Header("重定向设置")]
    [Tooltip("是否在启动时自动设置骨骼映射")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Tooltip("是否使用Avatar Mask来控制哪些骨骼接收动画")]
    [SerializeField] private bool useAvatarMask = false;
    
    [Tooltip("Avatar Mask用于控制哪些骨骼接收动画")]
    [SerializeField] private AvatarMask avatarMask;
    
    // 重定向控制器组件的引用
    private RetargetingAnimationController retargetingController;
    
    private void Awake()
    {
        // 获取或添加重定向控制器组件
        retargetingController = GetComponent<RetargetingAnimationController>();
        if (retargetingController == null)
        {
            retargetingController = gameObject.AddComponent<RetargetingAnimationController>();
        }
    }
    
    private void Start()
    {
        // 设置重定向控制器的基本属性
        SetupRetargetingController();
        
        // 如果启用了自动设置，则自动创建骨骼映射
        if (autoSetupOnStart)
        {
            AutoSetupBoneMappings();
        }
    }
    
    /// <summary>
    /// 设置重定向控制器的基本属性
    /// </summary>
    private void SetupRetargetingController()
    {
        if (templateCharacter == null || targetCharacter == null)
        {
            Debug.LogError("模板角色或目标角色未设置！");
            return;
        }
        
        // 通过反射设置私有字段，因为这些属性没有公开的setter
        var sourceAnimatorField = typeof(RetargetingAnimationController).GetField("sourceAnimator", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        var targetAnimatorField = typeof(RetargetingAnimationController).GetField("targetAnimator", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        var useAvatarMaskField = typeof(RetargetingAnimationController).GetField("useAvatarMask", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        var avatarMaskField = typeof(RetargetingAnimationController).GetField("avatarMask", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (sourceAnimatorField != null && targetAnimatorField != null)
        {
            sourceAnimatorField.SetValue(retargetingController, templateCharacter);
            targetAnimatorField.SetValue(retargetingController, targetCharacter);
        }
        
        if (useAvatarMaskField != null && avatarMaskField != null)
        {
            useAvatarMaskField.SetValue(retargetingController, useAvatarMask);
            avatarMaskField.SetValue(retargetingController, avatarMask);
        }
    }
    
    /// <summary>
    /// 自动设置骨骼映射
    /// </summary>
    private void AutoSetupBoneMappings()
    {
        // 清除现有的骨骼映射
        retargetingController.ClearBoneMappings();
        
        // 获取模板角色和目标角色的所有骨骼
        Dictionary<string, Transform> templateBones = new Dictionary<string, Transform>();
        Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>();
        
        CollectBones(templateCharacter.transform, templateBones);
        CollectBones(targetCharacter.transform, targetBones);
        
        // 为具有匹配名称的骨骼创建映射
        foreach (var templateEntry in templateBones)
        {
            if (targetBones.TryGetValue(templateEntry.Key, out Transform targetTransform))
            {
                // 添加骨骼映射
                retargetingController.AddBoneMapping(
                    templateEntry.Value,  // 源骨骼
                    targetTransform,       // 目标骨骼
                    true,                  // 应用位置
                    true,                  // 应用旋转
                    false                  // 不应用缩放
                );
            }
        }
        
        Debug.Log($"自动设置了 {templateBones.Count} 个骨骼映射");
    }
    
    /// <summary>
    /// 收集骨骼层次结构中的所有骨骼
    /// </summary>
    private void CollectBones(Transform root, Dictionary<string, Transform> boneDict)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);
        
        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            
            // 将此骨骼添加到字典中
            string boneName = current.name;
            if (!boneDict.ContainsKey(boneName))
            {
                boneDict.Add(boneName, current);
            }
            
            // 将所有子骨骼添加到队列中
            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }
    }
    
    /// <summary>
    /// 示例方法：在运行时切换Avatar Mask
    /// </summary>
    public void SetAvatarMask(AvatarMask newMask)
    {
        avatarMask = newMask;
        useAvatarMask = (newMask != null);
        retargetingController.SetAvatarMask(newMask);
    }
    
    /// <summary>
    /// 示例方法：添加单个骨骼映射
    /// </summary>
    public void AddSingleBoneMapping(Transform sourceBone, Transform targetBone)
    {
        if (sourceBone != null && targetBone != null)
        {
            retargetingController.AddBoneMapping(sourceBone, targetBone);
        }
    }
}