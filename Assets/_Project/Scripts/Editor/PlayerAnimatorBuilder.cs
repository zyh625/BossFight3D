using BossFight.Character;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 用代码生成玩家的 Animator Controller。
    /// 一个 Locomotion 混合树（待机/走/跑，按 Speed 参数混合）+ 若干动作状态（攻击等）。
    /// 状态之间不连转移线，全部由代码 CrossFade 切换。
    /// </summary>
    internal static class PlayerAnimatorBuilder
    {
        // 混合树阈值（m/s）：角色实际速度等于阈值时，播放的就是对应的动画。
        // 走/跑速度在 PlayerMovementConfig 里是 3.5 / 6.5，3.5 会落在走和跑之间，看起来是小跑。
        private const float WalkThreshold = 1.8f;
        private const float RunThreshold = 5.5f;

        /// <summary>
        /// 动作类状态：状态名 = 动画文件名。对应的 FBX 存在才会加入，缺了也不影响玩法
        /// （CharacterVisual.Play 找不到状态时什么都不做）。
        /// </summary>
        private static readonly string[] ActionStates = { "Light1", "Light2", "Light3", "HeavyCharge", "Heavy", "Roll", "Hit" };

        /// <returns>三个动画齐全并生成成功时返回 true。</returns>
        public static bool Build()
        {
            AnimationClip idle = Load("Idle");
            AnimationClip walk = Load("Walk");
            AnimationClip run = Load("Run");
            if (idle == null || walk == null || run == null)
            {
                Debug.LogWarning($"[BossFight] {ProjectPaths.MixamoPlayerAnimations} 下需要 Idle.fbx、Walk.fbx、Run.fbx，" +
                                 "缺少其中之一，未生成 Animator Controller");
                return false;
            }

            ProjectPaths.EnsureFolderFor(ProjectPaths.PlayerAnimatorController);
            AssetDatabase.DeleteAsset(ProjectPaths.PlayerAnimatorController);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ProjectPaths.PlayerAnimatorController);

            controller.AddParameter(AnimParams.SpeedName, AnimatorControllerParameterType.Float);

            controller.CreateBlendTreeInController(AnimParams.LocomotionState, out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = AnimParams.SpeedName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, WalkThreshold);
            tree.AddChild(run, RunThreshold);

            // 播放速度由 ActionSpeed 参数控制：代码按"数据里的动作时长"算出倍率，让动画去适配数据
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = AnimParams.ActionSpeedName,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            });

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (string stateName in ActionStates)
            {
                AnimationClip clip = Load(stateName);
                if (clip == null)
                {
                    continue;
                }

                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;
                state.speedParameterActive = true;
                state.speedParameter = AnimParams.ActionSpeedName;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static AnimationClip Load(string name)
        {
            return MixamoImportSetup.LoadClip($"{ProjectPaths.MixamoPlayerAnimations}/{name}.fbx");
        }
    }
}
