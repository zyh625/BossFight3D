using System;
using UnityEngine;

namespace BossFight.Character
{
    /// <summary>短暂冻结角色的动作时钟和动画。持续时间以真实时间计算。</summary>
    [DefaultExecutionOrder(-90)]
    public sealed class LocalHitStop : MonoBehaviour
    {
        private Animator animator;
        private float savedAnimatorSpeed;
        private double freezeUntil;
        private double pausedAt;
        private double pausedDuration;
        private int resumedFrame = -1;

        public bool IsFrozen { get; private set; }
        public double ActionTime => (IsFrozen ? pausedAt : Time.timeAsDouble) - pausedDuration;
        public float DeltaTime => IsFrozen || resumedFrame == Time.frameCount ? 0f : Time.deltaTime;

        private void Start()
        {
            if (TryGetComponent(out CharacterVisual visual))
            {
                animator = visual.Animator;
            }
        }

        public void FreezeFor(float seconds)
        {
            if (!isActiveAndEnabled || seconds <= 0f)
            {
                return;
            }

            freezeUntil = Math.Max(freezeUntil, Time.unscaledTimeAsDouble + seconds);
            if (IsFrozen)
            {
                return;
            }

            IsFrozen = true;
            pausedAt = Time.timeAsDouble;
            if (animator != null)
            {
                savedAnimatorSpeed = animator.speed;
                animator.speed = 0f;
            }
        }

        private void Update()
        {
            if (IsFrozen && Time.unscaledTimeAsDouble >= freezeUntil)
            {
                Resume();
            }
        }

        private void OnDisable()
        {
            if (IsFrozen)
            {
                Resume();
            }
        }

        private void Resume()
        {
            IsFrozen = false;
            resumedFrame = Time.frameCount;
            pausedDuration += Time.timeAsDouble - pausedAt;
            freezeUntil = 0d;
            if (animator != null)
            {
                animator.speed = savedAnimatorSpeed;
            }
        }
    }
}
