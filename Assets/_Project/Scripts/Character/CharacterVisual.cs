using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Character
{
    /// <summary>
    /// 逻辑与模型之间的唯一接口。其他脚本想播动画、想拿骨骼，都通过它，
    /// 不直接持有 Animator，也不知道模型内部的层级——所以换模型只需要替换 Visual 子物体。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CharacterVisual : MonoBehaviour
    {
        [Tooltip("模型的容器（Visual 子物体）。模型作为它的子物体，可以整体替换")]
        [SerializeField] private Transform visualRoot;

        public Transform VisualRoot => visualRoot;
        public Animator Animator { get; private set; }

        /// <summary>是否有可用的 Humanoid 骨骼（占位胶囊体没有）。</summary>
        public bool HasHumanoid => Animator != null && Animator.avatar != null && Animator.isHuman;

        private bool CanAnimate => Animator != null && Animator.runtimeAnimatorController != null;

        // 动画片段时长表（键 = 片段名的哈希；约定 Animator 里的状态名与片段名相同）
        private readonly Dictionary<int, float> clipLengths = new Dictionary<int, float>();

        private void Awake()
        {
            Animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : null;

            if (CanAnimate)
            {
                foreach (AnimationClip clip in Animator.runtimeAnimatorController.animationClips)
                {
                    clipLengths[Animator.StringToHash(clip.name)] = clip.length;
                }
            }
        }

        /// <summary>
        /// 切换到指定的动画状态。没有模型、或 Animator 里没有这个状态时什么都不做，
        /// 所以缺动画不会影响玩法逻辑。
        /// </summary>
        /// <param name="fitDuration">
        /// 大于 0 时调整播放速度，让动画刚好在这么长时间内播完——时序以数据为准，动画去适配数据。
        /// </param>
        /// <param name="startAt">换算前的动作时间，用于识破时跳到挥击姿势。</param>
        public void Play(string stateName, float crossFade, float fitDuration = 0f, float startAt = 0f)
        {
            if (!CanAnimate || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            int hash = Animator.StringToHash(stateName);
            if (!Animator.HasState(0, hash))
            {
                return;
            }

            float speed = 1f;
            if (fitDuration > 0f && clipLengths.TryGetValue(hash, out float length))
            {
                speed = length / fitDuration;
            }

            Animator.SetFloat(AnimParams.ActionSpeed, speed);
            Animator.CrossFadeInFixedTime(hash, crossFade, 0, startAt * speed);
        }

        /// <summary>按 Humanoid 骨骼枚举取骨骼，没有 Humanoid 骨骼时返回 null。</summary>
        public Transform GetBone(HumanBodyBones bone)
        {
            return HasHumanoid ? Animator.GetBoneTransform(bone) : null;
        }

        public void SetFloat(int parameterHash, float value, float dampTime, float dt)
        {
            if (CanAnimate)
            {
                Animator.SetFloat(parameterHash, value, dampTime, dt);
            }
        }
    }
}
