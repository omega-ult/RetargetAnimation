# 动画重定向系统 (Animation Retargeting System)

这个系统允许你将一个角色的动画重定向到另一个角色上，特别适用于当源角色的骨骼是目标角色骨骼的子集时。你可以像使用AvatarMask一样，选择性地指定哪些骨骼应该接收动画数据。

## 主要功能

- 将源角色的动画数据映射到目标角色的骨骼上
- 支持选择性地应用位置、旋转和缩放变换
- 支持使用AvatarMask来控制哪些骨骼接收动画
- 提供自动骨骼映射功能，基于骨骼名称匹配
- 可以在运行时动态添加或修改骨骼映射

## 组件说明

### RetargetingPlayableAsset

这是一个PlayableAsset，可以在Timeline中使用，用于创建动画重定向的Playable实例。

### RetargetingPlayableBehaviour

这是实际执行动画重定向逻辑的行为类，负责从源动画中提取变换数据，并将其应用到目标骨骼上。

### RetargetingAnimationController

这是一个MonoBehaviour组件，可以添加到场景中的GameObject上，用于在运行时应用动画重定向。它提供了一个简单的接口来设置源角色和目标角色之间的骨骼映射。

## 使用方法

### 基本设置

1. 将`RetargetingAnimationController`组件添加到场景中的一个GameObject上
2. 设置源Animator（模板角色A）和目标Animator（角色B）
3. 设置骨骼映射，可以手动添加或使用自动设置功能
4. 可选：设置AvatarMask来控制哪些骨骼接收动画

### 骨骼映射

每个骨骼映射包含以下设置：
- 源变换（Source Transform）：模板角色A上的骨骼
- 目标变换（Target Transform）：角色B上的骨骼
- 应用位置（Apply Position）：是否应用位置变换
- 应用旋转（Apply Rotation）：是否应用旋转变换
- 应用缩放（Apply Scale）：是否应用缩放变换

### 示例用法

查看`RetargetingExample.cs`脚本，了解如何在代码中使用动画重定向系统。

```csharp
// 获取重定向控制器
var retargetingController = GetComponent<RetargetingAnimationController>();

// 添加骨骼映射
retargetingController.AddBoneMapping(
    sourceTransform,  // 源骨骼
    targetTransform,  // 目标骨骼
    true,             // 应用位置
    true,             // 应用旋转
    false             // 不应用缩放
);

// 设置Avatar Mask
retargetingController.SetAvatarMask(myAvatarMask);
```

## 高级用法

### 在Timeline中使用

1. 在Project窗口中右键点击，选择`Create > Playables > Animation Retargeting Asset`
2. 设置源动画片段和骨骼映射
3. 将创建的资产拖放到Timeline轨道上

### 运行时动态修改

你可以在运行时动态添加、修改或清除骨骼映射：

```csharp
// 添加新的骨骼映射
retargetingController.AddBoneMapping(newSourceBone, newTargetBone);

// 清除所有骨骼映射
retargetingController.ClearBoneMappings();

// 更改Avatar Mask
retargetingController.SetAvatarMask(newAvatarMask);
```