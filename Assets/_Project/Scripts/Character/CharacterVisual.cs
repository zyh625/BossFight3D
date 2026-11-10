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

        private void Awake()
        {
            Animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : null;
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
