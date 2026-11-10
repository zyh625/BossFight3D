using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>格挡资源。破防硬直结束后，格挡恢复到全满才会退出虚弱。</summary>
    public sealed class GuardMeter : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float max = 100f;
        [SerializeField, Min(0f)] private float recoveryDelay = 4f;
        [SerializeField, Min(0f)] private float recoveryRate = 10f;
        [SerializeField, Min(0.01f)] private float weaknessDuration = 6f;

        private float delay;

        public float Max => max;
        public float Current { get; private set; }
        public bool IsWeak { get; private set; }

        private void Awake()
        {
            Current = max;
        }

        internal void Absorb(float amount)
        {
            Current = Mathf.Max(0f, Current - Mathf.Max(0f, amount));
        }

        internal void DelayRecovery()
        {
            delay = recoveryDelay;
        }

        internal void BeginWeakness()
        {
            Current = 0f;
            IsWeak = true;
        }

        internal void Tick(float dt)
        {
            if (IsWeak)
            {
                Current = Mathf.Min(max, Current + max / weaknessDuration * dt);
                IsWeak = Current < max;
                return;
            }

            float recoveryTime = Mathf.Max(0f, dt - delay);
            delay = Mathf.Max(0f, delay - dt);
            Current = Mathf.Min(max, Current + recoveryRate * recoveryTime);
        }
    }
}
