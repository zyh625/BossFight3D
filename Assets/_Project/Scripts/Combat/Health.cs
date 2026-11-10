using System;
using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>生命值。只管数值和事件，不知道伤害从哪来、也不知道谁在显示它。</summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private float max = 100f;

        public float Max => max;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>数值变化时触发（当前值, 最大值）。血条订阅它。</summary>
        public event Action<float, float> Changed;

        public event Action Died;

        private void Awake()
        {
            Current = max;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current, max);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }

        public void RestoreFull()
        {
            Current = max;
            Changed?.Invoke(Current, max);
        }
    }
}
