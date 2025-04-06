using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Serialization;

[AddComponentMenu("Animation/Retargeting Animation Controller")]
public class RetargetingAnimationController : MonoBehaviour
{
    [SerializeField] private Animator sourceAnimator;
    [FormerlySerializedAs("targetAnimator")] [SerializeField] private Animator blendAnimator;
    [SerializeField] private Animator destinationAnimator;
    [SerializeField] private BoneMappingAsset boneMappingAsset;
    [SerializeField] private bool useAvatarMask = false;
    [SerializeField] private AvatarMask avatarMask;
    [SerializeField] private bool autoSetupBoneMappings = false;

    // 临时存储自动生成的骨骼映射
    private List<BoneMapping> tempBoneMappings = new List<BoneMapping>();

    private PlayableGraph playableGraph;
    private ScriptPlayable<RetargetingPlayableBehaviour> retargetingPlayable;
    private ScriptPlayableOutput playableOutput;

    private void Start()
    {
        if (sourceAnimator == null || blendAnimator == null || destinationAnimator == null)
        {
            Debug.LogError("Source or Target Animator is not assigned!");
            return;
        }

        if (autoSetupBoneMappings && (boneMappingAsset == null || boneMappingAsset.Count == 0))
        {
            SetupDefaultBoneMappings();
        }

        InitializePlayableGraph();
    }

    private void SetupDefaultBoneMappings()
    {
        // Try to automatically map bones with the same names
        Transform sourceRoot = sourceAnimator.transform;
        Transform targetRoot = blendAnimator.transform;

        Dictionary<string, Transform> sourceBones = new Dictionary<string, Transform>();
        Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>();

        // Collect all bones from source
        CollectBones(sourceRoot, sourceBones);

        // Collect all bones from target
        CollectBones(targetRoot, targetBones);

        // Clear temporary bone mappings
        tempBoneMappings.Clear();

        // 创建主要骨骼映射列表（用于自动匹配子骨骼的根骨骼）
        List<string> mainBones = new List<string>
            { "Hips", "Spine", "Head", "LeftArm", "RightArm", "LeftLeg", "RightLeg" };

        // 首先添加主要骨骼的映射，并设置matchChildren为true
        foreach (string boneName in mainBones)
        {
            if (sourceBones.TryGetValue(boneName, out Transform sourceTransform) &&
                targetBones.TryGetValue(boneName, out Transform targetTransform))
            {
                BoneMapping mapping = new BoneMapping
                {
                    sourcePath = GetRelativePath(sourceTransform, sourceRoot),
                    targetPath = GetRelativePath(targetTransform, targetRoot),
                    applyPosition = true,
                    applyRotation = true,
                    applyScale = false,
                    matchChildren = true // 自动匹配子骨骼
                };

                tempBoneMappings.Add(mapping);
            }
        }

        // 为未包含在主要骨骼子层级中的骨骼创建单独的映射
        HashSet<Transform> mappedSourceBones = new HashSet<Transform>();
        HashSet<Transform> mappedTargetBones = new HashSet<Transform>();

        // 收集已映射的骨骼（包括子骨骼）
        foreach (var mapping in tempBoneMappings)
        {
            if (mapping.matchChildren)
            {
                Transform sourceTransform = FindTransformInHierarchy(sourceRoot, mapping.sourcePath);
                Transform targetTransform = FindTransformInHierarchy(targetRoot, mapping.targetPath);

                if (sourceTransform != null && targetTransform != null)
                {
                    // 添加自身和所有子骨骼到已映射集合
                    CollectTransformAndChildren(sourceTransform, mappedSourceBones);
                    CollectTransformAndChildren(targetTransform, mappedTargetBones);
                }
            }
        }

        // 为未映射的骨骼创建单独的映射
        foreach (var sourceEntry in sourceBones)
        {
            // 跳过已映射的骨骼
            if (mappedSourceBones.Contains(sourceEntry.Value))
                continue;

            if (targetBones.TryGetValue(sourceEntry.Key, out Transform targetTransform) &&
                !mappedTargetBones.Contains(targetTransform))
            {
                BoneMapping mapping = new BoneMapping
                {
                    sourcePath = GetRelativePath(sourceEntry.Value, sourceRoot),
                    targetPath = GetRelativePath(targetTransform, targetRoot),
                    applyPosition = true,
                    applyRotation = true,
                    applyScale = false,
                    matchChildren = false // 不自动匹配子骨骼
                };

                tempBoneMappings.Add(mapping);
            }
        }

        // Create a new bone mapping asset if one doesn't exist
        if (boneMappingAsset == null)
        {
            boneMappingAsset = ScriptableObject.CreateInstance<BoneMappingAsset>();
            Debug.Log("Created new BoneMappingAsset");

            // Note: This asset is created at runtime and will not be saved to disk
            // In the editor, users should create and assign a BoneMappingAsset manually
        }

        // Clear existing mappings and add the new ones
        boneMappingAsset.ClearBoneMappings();
        foreach (var mapping in tempBoneMappings)
        {
            boneMappingAsset.AddBoneMapping(mapping);
        }

        Debug.Log($"Auto-setup created {tempBoneMappings.Count} bone mappings");
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

    // 获取骨骼相对于根骨骼的路径
    private string GetRelativePath(Transform bone, Transform root)
    {
        if (bone == root)
            return "";

        if (bone.parent == root)
            return bone.name;

        return GetRelativePathRecursive(bone, root);
    }

    private string GetRelativePathRecursive(Transform bone, Transform root)
    {
        if (bone == root)
            return "";

        if (bone.parent == root)
            return bone.name;

        if (bone.parent == null)
            return bone.name;

        return GetRelativePathRecursive(bone.parent, root) + "/" + bone.name;
    }

    // 收集Transform及其所有子Transform
    private void CollectTransformAndChildren(Transform transform, HashSet<Transform> collection)
    {
        if (transform == null || collection == null)
            return;

        collection.Add(transform);

        for (int i = 0; i < transform.childCount; i++)
        {
            CollectTransformAndChildren(transform.GetChild(i), collection);
        }
    }

    // 在层级结构中查找指定路径的Transform
    private Transform FindTransformInHierarchy(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path))
            return root;

        return root.Find(path);
    }

    private void InitializePlayableGraph()
    {
        // Create the PlayableGraph
        playableGraph = PlayableGraph.Create("RetargetingGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // Create the retargeting playable
        // 创建带有1个输入端口的ScriptPlayable
        retargetingPlayable = ScriptPlayable<RetargetingPlayableBehaviour>.Create(playableGraph);
        var behaviour = retargetingPlayable.GetBehaviour();


        // 获取骨骼映射列表
        List<BoneMapping> mappings = boneMappingAsset != null ? boneMappingAsset.BoneMappings : new List<BoneMapping>();

        // 设置行为参数
        behaviour.BoneMappings = mappings;
        behaviour.UseAvatarMask = useAvatarMask;
        behaviour.AvatarMask = avatarMask;

        // 设置源Animator
        behaviour.SourceAnimator = sourceAnimator;
        behaviour.BlendAnimator = blendAnimator;
        behaviour.TargetAnimator = destinationAnimator;

        // Create the output and connect it to the target animator
        // AnimationPlayableOutput
        playableOutput = ScriptPlayableOutput.Create(playableGraph, "AnimationOutput");
        playableOutput.SetSourcePlayable(retargetingPlayable);


        // Start the graph
        playableGraph.Play();
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }

    // Helper method to add a bone mapping at runtime
    public void AddBoneMapping(Transform sourceTransform, Transform targetTransform,
        bool applyPosition = true, bool applyRotation = true, bool applyScale = false)
    {
        if (boneMappingAsset == null)
        {
            boneMappingAsset = ScriptableObject.CreateInstance<BoneMappingAsset>();
            Debug.Log("Created new BoneMappingAsset at runtime");
        }

        BoneMapping mapping = new BoneMapping
        {
            sourceTransform = sourceTransform,
            targetTransform = targetTransform,
            applyPosition = applyPosition,
            applyRotation = applyRotation,
            applyScale = applyScale
        };

        boneMappingAsset.AddBoneMapping(mapping);

        // Update the playable behaviour if already initialized
        if (retargetingPlayable.IsValid())
        {
            var behaviour = retargetingPlayable.GetBehaviour();
            behaviour.BoneMappings = boneMappingAsset.BoneMappings;
        }
    }

    // Helper method to clear all bone mappings
    public void ClearBoneMappings()
    {
        if (boneMappingAsset != null)
        {
            boneMappingAsset.ClearBoneMappings();

            // Update the playable behaviour if already initialized
            if (retargetingPlayable.IsValid())
            {
                var behaviour = retargetingPlayable.GetBehaviour();
                behaviour.BoneMappings = boneMappingAsset.BoneMappings;
            }
        }
    }

    // Helper method to set the avatar mask at runtime
    public void SetAvatarMask(AvatarMask mask)
    {
        avatarMask = mask;
        useAvatarMask = (mask != null);

        // Update the playable behaviour if already initialized
        if (retargetingPlayable.IsValid())
        {
            var behaviour = retargetingPlayable.GetBehaviour();
            behaviour.UseAvatarMask = useAvatarMask;
            behaviour.AvatarMask = avatarMask;
        }
    }

    // Helper method to set the bone mapping asset at runtime
    public void SetBoneMappingAsset(BoneMappingAsset asset)
    {
        boneMappingAsset = asset;

        // Update the playable behaviour if already initialized
        if (retargetingPlayable.IsValid())
        {
            var behaviour = retargetingPlayable.GetBehaviour();
            behaviour.BoneMappings = boneMappingAsset != null ? boneMappingAsset.BoneMappings : new List<BoneMapping>();
        }
    }
}