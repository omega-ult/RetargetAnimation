using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[Serializable]
public class BoneMapping
{
    // 使用字符串路径代替直接的Transform引用
    public string sourcePath;
    public string targetPath;
    
    // 运行时缓存的Transform引用
    [NonSerialized] public Transform sourceTransform;
    [NonSerialized] public Transform targetTransform;
    
    // 应用变换的选项
    public bool applyPosition = true;
    public bool applyRotation = true;
    public bool applyScale = false;
    
    // 是否递归匹配子骨骼
    public bool matchChildren = true;
}
