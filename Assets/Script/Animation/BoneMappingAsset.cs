using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Bone Mapping Asset", menuName = "Animation/Bone Mapping Asset")]
public class BoneMappingAsset : ScriptableObject
{
    [SerializeField] private List<BoneMapping> boneMappings = new List<BoneMapping>();
    
    public List<BoneMapping> BoneMappings => boneMappings;
    
    // 添加骨骼映射
    public void AddBoneMapping(BoneMapping mapping)
    {
        boneMappings.Add(mapping);
    }
    
    // 清除所有骨骼映射
    public void ClearBoneMappings()
    {
        boneMappings.Clear();
    }
    
    // 移除指定索引的骨骼映射
    public void RemoveBoneMapping(int index)
    {
        if (index >= 0 && index < boneMappings.Count)
        {
            boneMappings.RemoveAt(index);
        }
    }
    
    // 获取骨骼映射数量
    public int Count => boneMappings.Count;
}