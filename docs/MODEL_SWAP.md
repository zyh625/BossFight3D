# 模型替换指南

目标：把占位人偶换成你用 Blender 做的模型时，**不改任何代码、不丢任何动画**。

## 1. 为什么能直接换

```
Player（逻辑根）        CharacterController、状态机、Hurtbox、所有脚本
├─ Visual              模型容器 —— 只有这里面的东西会被替换
│   └─ Model           带 Humanoid Avatar 的模型，Animator 在这一层
├─ Sockets             武器/特效挂点，运行时挂到骨骼上
└─ CameraTarget        相机跟随点
```

- 逻辑和碰撞都在根物体上，模型只是"皮"。
- 动画全部是 **Humanoid** 类型。Unity 会把动画从"人形骨骼的抽象描述"重定向到任何 Humanoid 模型上，所以给 Y Bot 用的动画可以直接给你的模型用。
- 代码里取骨骼只用 `Animator.GetBoneTransform(HumanBodyBones.RightHand)` 这种枚举，不出现骨骼名和层级路径，所以你的骨骼叫什么名字都可以。
- 攻击判定（Hitbox）挂在武器 Socket 或逻辑物体上，与模型网格无关。

## 2. 替换步骤

1. 把导出的 FBX 放进工程（例如 `Assets/_Project/Art/Characters/MyHero.fbx`）。
2. 选中 FBX → Inspector → **Rig** 页：Animation Type 选 **Humanoid**，Avatar Definition 选 **Create From This Model** → Apply。
3. 点 **Configure...** 检查骨骼映射：所有必需骨骼是绿色，角色处于 T-Pose（不是的话用 Pose → Enforce T-Pose）。
4. 打开 `Assets/_Project/Prefabs/Player.prefab`，删掉 `Visual` 下的 `Model`，把你的 FBX 拖到 `Visual` 下。
5. 选中新模型，在它的 Animator 组件上：Controller 指定为 `Assets/_Project/Generated/Player.controller`，**取消勾选 Apply Root Motion**。
6. 运行检查：脚是否贴地、武器是否在手里。
   - 脚不贴地：调整模型在 `Visual` 下的本地 Y 坐标，或回 Blender 把原点放到脚底。
   - 握剑姿势不对：在预制体里调整 `Sockets/Socket_WeaponR` 的本地位置和旋转（它就是"武器相对手骨的偏移"）。

> 注意：`Tools/BossFight/M1/3 Build Player Prefab` 会按 Y Bot 整体重建预制体，换上自己的模型之后不要再执行它。
> 到你准备换模型的那个里程碑，我会把搭建脚本改成"可指定模型路径"。

## 3. Blender 制作规范

### 比例与朝向

| 项目 | 要求 |
|---|---|
| 单位 | Scene Properties → Units：Metric，Unit Scale = 1.0 |
| 身高 | 玩家约 **1.8 m**（碰撞胶囊高 1.8、半径 0.35）。Boss 也按 1.8 m 做，1.5 倍体型在 Unity 里缩放 |
| 原点 | 在**两脚之间的地面上**（世界原点） |
| 朝向 | 在 Blender 里面朝 **-Y**（即正视图 Numpad 1 看到的是角色正面），头顶朝 +Z |
| 姿势 | 绑定姿势用 **T-Pose**（手臂水平、掌心向下、双脚并拢朝前） |
| 变换 | 导出前对网格和骨架都执行 Ctrl+A → **All Transforms**（缩放必须是 1，旋转必须是 0） |

### 骨骼

Humanoid 至少需要这 15 根骨骼，缺一根就无法生成 Avatar：

- Hips（必须是根骨骼或根骨骼的直接后代）、Spine、Head
- 左右各：UpperArm、LowerArm、Hand、UpperLeg、LowerLeg、Foot

建议额外具备（动画质量会明显更好）：Chest、Neck、左右 Shoulder、左右 Toes。
手指可选；本项目握剑需要手指时，每根手指 3 节。

其他要求：

- **一个模型只有一个骨架（Armature）**，所有网格都绑定到它。
- 骨骼层级必须是标准的人形父子关系（Hips → Spine → Chest → Neck → Head；Chest → Shoulder → UpperArm → LowerArm → Hand；Hips → UpperLeg → LowerLeg → Foot → Toes）。
- 骨骼名字随意，但建议左右用 `.L` / `.R` 或 `Left` / `Right` 区分，Unity 自动映射更准确。
- 披风、头发、衣摆等附加骨骼可以有，挂在对应的人形骨骼下即可，Humanoid 会忽略它们（它们不会被动画重定向驱动，需要另外用物理或程序化驱动）。
- 不要有带缩放的骨骼动画；IK 控制骨、极向量骨等非形变骨不要导出（见导出设置）。

### 网格与材质

- 低多边形风格，角色面数建议 3k~15k 三角面。
- 每个顶点最多受 **4 根骨骼**影响（Unity 默认上限）。
- 材质尽量少（1~3 个）。需要发光的部位单独分一个材质，方便在 Unity 里做 Emission + Bloom。
- **武器不要和角色合并**：剑做成单独的 FBX，原点放在握把处，剑身朝 +Z（Unity 坐标），由 Socket 挂到手上。

### FBX 导出设置（File → Export → FBX）

| 选项 | 值 |
|---|---|
| Include → Limit to | Selected Objects（只选骨架和网格） |
| Include → Object Types | Armature、Mesh |
| Transform → Scale | 1.0 |
| Transform → Apply Scalings | **FBX Units Scale** |
| Transform → Forward / Up | **-Z Forward**，**Y Up** |
| Transform → Apply Unit | 勾选 |
| Transform → Apply Transform | 不勾选（实验性功能，带骨骼时容易出问题） |
| Geometry → Apply Modifiers | 勾选（注意：有形态键时修改器不会被应用） |
| Armature → Only Deform Bones | **勾选** |
| Armature → Add Leaf Bones | **不勾选** |
| Bake Animation | 只导出模型时不勾选 |

### 导入 Unity 后自检

- 选中 FBX，Inspector 里 Scale Factor 为 1，模型拖进场景后 Transform 的 Scale 是 (1,1,1)、Rotation 是 (0,0,0)。
- 模型站在原点时脚底在 y = 0，面朝 Unity 的 +Z。
- Rig → Configure 里骨骼全绿。
