using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class RetargetingPlayableBehaviour : PlayableBehaviour
{
    private bool _initialized = false;

    public RuntimeAnimatorController SourceAnimatorController => SourceAnimator?.runtimeAnimatorController;
    public Animator SourceAnimator { get; set; }
    public Animator BlendAnimator { get; set; }
    public Animator TargetAnimator { get; set; }
    public List<BoneMapping> BoneMappings { get; set; }
    public bool UseAvatarMask { get; set; }
    public AvatarMask AvatarMask { get; set; }

    private Dictionary<string, bool> _maskMap = new();


    public AnimatorControllerPlayable SourcePlayable { get; private set; }
    public AnimatorControllerPlayable BlendPlayable { get; private set; }


    public Dictionary<Transform, BoneMapping> RuntimeBoneMappings { get; set; } = new();

    // 源模型和目标模型的根变换
    private Transform _sourceRoot;
    private Transform _blendRoot;
    private Transform _targetRoot;

    public override void OnPlayableCreate(Playable playable)
    {
        base.OnPlayableCreate(playable);
        playable.SetInputCount(2);
    }

    public override void OnGraphStart(Playable playable)
    {
        if ((SourceAnimatorController == null && SourceAnimator == null) || BoneMappings == null ||
            BoneMappings.Count == 0)
            return;

        // 创建源动画Playable并连接到输入端口0
        var playableGraph = playable.GetGraph();
        SourcePlayable = AnimatorControllerPlayable.Create(playableGraph, SourceAnimator.runtimeAnimatorController);
        BlendPlayable = AnimatorControllerPlayable.Create(playableGraph, BlendAnimator.runtimeAnimatorController);
        playableGraph.Connect(SourcePlayable, 0, playable, 0);
        playableGraph.Connect(BlendPlayable, 0, playable, 1);

        // 设置权重确保输入生效
        playable.SetInputWeight(0, 1.0f);
        playable.SetInputWeight(1, 1.0f);
        // 获取源和目标的根变换
        _sourceRoot = SourceAnimator.transform;
        _blendRoot = BlendAnimator.transform;
        _targetRoot = TargetAnimator.transform;

        if (_sourceRoot == null || _targetRoot == null)
        {
            Debug.LogError("Could not find source or target root transform!");
            return;
        }

        RuntimeBoneMappings.Clear();

        // 构建骨骼映射字典
        foreach (var mapping in BoneMappings.Where(mapping =>
                     !string.IsNullOrEmpty(mapping.sourcePath) && !string.IsNullOrEmpty(mapping.targetPath)))
        {
            Debug.Log($"Processing mapping: {mapping.sourcePath} -> {mapping.targetPath}");

            // 查找并缓存Transform引用
            mapping.sourceTransform = FindTransformInHierarchy(_sourceRoot, mapping.sourcePath);
            mapping.targetTransform = FindTransformInHierarchy(_blendRoot, mapping.targetPath);

            // 如果找到了有效的Transform，添加到Transform映射字典
            if (mapping.sourceTransform == null || mapping.targetTransform == null) continue;
            RuntimeBoneMappings[mapping.sourceTransform] = new BoneMapping()
            {
                sourcePath = mapping.sourcePath,
                targetPath = mapping.targetPath,
                sourceTransform = mapping.sourceTransform,
                targetTransform = mapping.targetTransform,
                applyPosition = mapping.applyPosition,
                applyRotation = mapping.applyRotation,
                applyScale = mapping.applyScale,
                matchChildren = mapping.matchChildren
            };

            // 如果需要匹配子骨骼，递归添加所有子骨骼的映射
            if (mapping.matchChildren)
            {
                AddChildrenMappings(mapping.sourceTransform, mapping.targetTransform,
                    mapping.applyPosition, mapping.applyRotation, mapping.applyScale);
            }

            Debug.Log($"Added mapping: {mapping.sourcePath} -> {mapping.targetPath}");
        }

        
        TraverseAndMatchTransforms(_sourceRoot, _targetRoot, (sourceTransform, targetTransform) =>
        {
            // 这里处理匹配的节点
            // Debug.Log($"匹配的节点: {sourceTransform.name} -> {targetTransform.name}");
            if (RuntimeBoneMappings.TryGetValue(sourceTransform, out var boneMapping))
            {
                // 如果已经存在映射，则更新
                boneMapping.sourceTransform = targetTransform;
            }
            else
            {
                // 如果不存在映射，则添加新的映射
                boneMapping = new BoneMapping
                {
                    sourceTransform = targetTransform,
                    targetTransform = sourceTransform,
                    applyPosition = true,
                    applyRotation = true,
                    applyScale = false
                };
            }
            RuntimeBoneMappings[targetTransform] = boneMapping;
            RuntimeBoneMappings.Remove(sourceTransform);
        });

        _initialized = true;
    }


    public override void OnGraphStop(Playable playable)
    {
        _initialized = false;
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!_initialized || _sourceRoot == null || _targetRoot == null)
            return;

        // 处理所有已映射的骨骼
        foreach (var entry in RuntimeBoneMappings)
        {
            var targetTransform = entry.Key;
            var sourceTransform = entry.Value.targetTransform;

            if (sourceTransform == null || targetTransform == null)
                continue;

            // // 检查是否应该基于Avatar Mask应用动画
            // if (UseAvatarMask && AvatarMask != null)
            // {
            //     var relativePath = CalculateTransformPath(targetTransform, _targetRoot);
            //     if (_maskMap.TryGetValue(relativePath, out bool active) && !active)
            //         continue; // 跳过在遮罩中禁用的骨骼
            // }

            // 查找对应的BoneMapping以获取应用设置
            var applyPosition = true;
            var applyRotation = true;
            var applyScale = false;

            var mapping = entry.Value;
            applyPosition = mapping.applyPosition;
            applyRotation = mapping.applyRotation;
            applyScale = mapping.applyScale;

            // 应用变换
            if (applyPosition)
            {
                targetTransform.localPosition = sourceTransform.localPosition;
            }

            if (applyRotation)
                targetTransform.localRotation = sourceTransform.localRotation;

            if (applyScale)
                targetTransform.localScale = sourceTransform.localScale;
        }
    }
// ... 现有代码 ...

    private void TraverseAndMatchTransforms(Transform sourceRoot, Transform targetRoot,
        Action<Transform, Transform> onMatch)
    {
        if (sourceRoot == null || targetRoot == null || onMatch == null)
            return;

        // 创建子节点名称到Transform的映射
        var sourceChildren = new Dictionary<string, Transform>();
        var targetChildren = new Dictionary<string, Transform>();

        // 收集源Transform的子节点
        for (int i = 0; i < sourceRoot.childCount; i++)
        {
            var child = sourceRoot.GetChild(i);
            sourceChildren[child.name] = child;
        }

        // 收集目标Transform的子节点
        for (int i = 0; i < targetRoot.childCount; i++)
        {
            var child = targetRoot.GetChild(i);
            targetChildren[child.name] = child;
        }

        // 遍历所有匹配的节点
        foreach (var sourceEntry in sourceChildren)
        {
            if (targetChildren.TryGetValue(sourceEntry.Key, out var targetChild))
            {
                // 节点名称匹配，执行回调
                onMatch(sourceEntry.Value, targetChild);

                // 递归处理子节点
                TraverseAndMatchTransforms(sourceEntry.Value, targetChild, onMatch);
            }
        }
    }

// ... 现有代码 ...


    // 在层级结构中查找指定路径的Transform
    private Transform FindTransformInHierarchy(Transform root, string boneName)
    {
        return string.IsNullOrEmpty(boneName) ? root : TraverseFindTransform(root, boneName);
    }

    // 通过ID查找Transform（递归查找所有子层级）
    private Transform TraverseFindTransform(Transform root, string id)
    {
        if (root.name == id)
            return root;

        foreach (Transform child in root)
        {
            var result = TraverseFindTransform(child, id);
            if (result != null)
                return result;
        }

        return null;
    }

    // 递归添加子骨骼的映射
    private void AddChildrenMappings(Transform sourceParent, Transform targetParent,
        bool applyPosition, bool applyRotation, bool applyScale)
    {
        // 获取源和目标的所有子骨骼
        Dictionary<string, Transform> sourceChildren = new Dictionary<string, Transform>();
        Dictionary<string, Transform> targetChildren = new Dictionary<string, Transform>();

        // 收集源骨骼的子骨骼
        for (int i = 0; i < sourceParent.childCount; i++)
        {
            Transform child = sourceParent.GetChild(i);
            sourceChildren[child.name] = child;
        }

        // 收集目标骨骼的子骨骼
        for (int i = 0; i < targetParent.childCount; i++)
        {
            Transform child = targetParent.GetChild(i);
            targetChildren[child.name] = child;
        }

        // 为名称匹配的子骨骼创建映射
        foreach (var sourceEntry in sourceChildren)
        {
            if (targetChildren.TryGetValue(sourceEntry.Key, out Transform targetChild))
            {
                // 记录子骨骼的映射
                RuntimeBoneMappings[sourceEntry.Value] = new BoneMapping
                {
                    sourceTransform = sourceEntry.Value,
                    targetTransform = targetChild,
                    applyPosition = applyPosition,
                    applyRotation = applyRotation,
                    applyScale = applyScale
                };

                // 递归处理子骨骼的子骨骼
                AddChildrenMappings(sourceEntry.Value, targetChild, applyPosition, applyRotation, applyScale);
            }
        }
    }

    // Runtime replacement for AnimationUtility.CalculateTransformPath
    private string CalculateTransformPath(Transform transform, Transform root)
    {
        if (transform == root)
            return "";

        if (transform.parent == null)
            return transform.name;

        return CalculateTransformPathRecursive(transform, root);
    }

    private string CalculateTransformPathRecursive(Transform transform, Transform root)
    {
        if (transform == root)
            return "";

        if (transform.parent == root)
            return transform.name;

        if (transform.parent == null)
            return transform.name;

        return CalculateTransformPathRecursive(transform.parent, root) + "/" + transform.name;
    }
}