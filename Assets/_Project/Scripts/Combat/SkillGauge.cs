using System;
using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 特殊技能条：慢动作的"燃料"。平时缓慢恢复，完美闪避和识破成功时额外恢复一截，
    /// 所以打得越好，能用的慢动作越多。它只管数值——谁来消耗、怎么消耗由使用者决定。
    /// </summary>
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class SkillGauge : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float max = 100f;
        [Tooltip("开局时的初始值")]
        [SerializeField, Min(0f)] private float startValue = 100f;

        [Header("恢复")]
        [Tooltip("没有在消耗时，每秒自然恢复多少")]
        [SerializeField, Min(0f)] private float passiveRegen = 5f;
        [SerializeField, Min(0f)] private float gainOnPerfectDodge = 15f;
        [SerializeField, Min(0f)] private float gainOnSeeThrough = 20f;

        private DamageReceiver receiver;

        public float Max => max;
        public float Current { get; private set; }

        /// <summary>为 true 时暂停自然恢复（正在使用技能）。</summary>
        public bool IsDraining { get; set; }

        public event Action<float, float> Changed;

        private void Awake()
        {
            receiver = GetComponent<DamageReceiver>();
            Current = Mathf.Min(startValue, max);
        }

        private void OnEnable()
        {
            receiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            receiver.Damaged -= OnDamaged;
        }

        private void Update()
        {
            if (!IsDraining && Current < max)
            {
                Set(Current + passiveRegen * Time.deltaTime);
            }
        }

        /// <summary>消耗指定数量，返回消耗之后是否还有剩余。</summary>
        public bool Drain(float amount)
        {
            Set(Current - amount);
            return Current > 0f;
        }

        private void OnDamaged(DamageResult result)
        {
            switch (result.Outcome)
            {
                case HitOutcome.Dodged:
                    Set(Current + gainOnPerfectDodge);
                    break;
                case HitOutcome.Parried:
                    Set(Current + gainOnSeeThrough);
                    break;
            }
        }

        private void Set(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, max);
            if (!Mathf.Approximately(clamped, Current))
            {
                Current = clamped;
                Changed?.Invoke(Current, max);
            }
        }
    }
}
