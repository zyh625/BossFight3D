# 素材来源与许可证

只使用可以免费商用的素材。每次引入新素材都在这里登记。

## 当前使用的素材

| 素材 | 来源 | 许可证 | 用途 | 是否进仓库 |
|---|---|---|---|---|
| Unity URP 3D 模板的渲染设置（`Assets/Settings/`） | Unity 编辑器自带模板 `com.unity.template.urp-blank` 17.0.14 | Unity Companion License | 渲染管线配置 | 是 |
| Y Bot 角色 + 动画（见下方清单） | [Mixamo](https://www.mixamo.com/)（Adobe） | Mixamo 条款：可免费用于个人和商业项目，可随游戏发布；**不允许把原始素材单独再分发** | 占位人偶和动作 | **否**（`.gitignore` 已忽略） |

场地、材质、占位胶囊体均为 Editor 脚本程序化生成，没有外部素材。

## Mixamo 下载清单

需要登录 Adobe 账号（免费）。下载后**按下表改文件名**放到指定目录，然后在 Unity 里执行
`Tools/BossFight/M1/Build All`，导入设置（Humanoid、循环、根运动）由脚本自动完成。

动画名是 Mixamo 站内搜索用的名字，如果站内名称有出入，选一个观感接近的即可——代码只认下表的**文件名**。

### 角色

| 站内名称 | 下载设置 | 保存为 |
|---|---|---|
| Y Bot（Characters 页） | Format: **FBX for Unity (.fbx)**，Pose: **T-pose** | `Assets/_Project/Art/Mixamo/Characters/YBot.fbx` |

### 玩家动画（目录：`Assets/_Project/Art/Mixamo/Animations/Player/`）

下载设置统一为：Format **FBX for Unity (.fbx)**，Skin **Without Skin**，Frames per Second 30，Keyframe Reduction none。
移动类动画必须勾选 **In Place**（原地播放），因为位移由代码控制。

| 里程碑 | 站内搜索 | 额外设置 | 保存为 |
|---|---|---|---|
| M1 | Idle（或 Breathing Idle） | — | `Idle.fbx` |
| M1 | Walking | 勾选 In Place | `Walk.fbx` |
| M1 | Running（或 Fast Run） | 勾选 In Place | `Run.fbx` |

（后续里程碑的动画会追加到这张表里。）
