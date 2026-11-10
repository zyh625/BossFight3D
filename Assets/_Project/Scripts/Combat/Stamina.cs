using System;
using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>当前动作允许的体力恢复方式。由角色的状态每帧设置。</summary>
    public enum StaminaRegenMode
    {
        /// <summary>不恢复（攻击、翻滚、奔跑等动作中）。</summary>
        None,
        /// <summary>移动中：恢复较慢。</summary>
        Moving,
        /// <summary>静止：恢复较快。</summary>
        Idle
    }

    /// <summary>
    /// 体力。消耗后要等一小段延迟才开始恢复，恢复速度取决于当前的 RegenMode。
    /// 规则：体力不足以支付时动作不能执行（TrySpend 返回 false）。
    /// </summary>
    public sealed class Stamina : MonoBehaviour
    {
        [SerializeField] private float max = 100f;

        [Header("恢复（点/秒）")]
        [SerializeField] private float idleRegen = 30f;
        [SerializeField] private float movingRegen = 18f;

        [Tooltip("任何消耗之后，要等这么久才开始恢复（秒）")]
        [SerializeField] private float regenDelay = 0.6f;

        private float delayRemaining;

        /// <summary>
        /// 所有消耗乘以这个倍率（1 = 正式数值）。由 PlayerCombatConfig 在启动时写入，
        /// 用来在调试阶段临时把体力压力调低，而不用逐个改每一招的消耗。
        /// </summary>
        public float CostMultiplier { get; set; } = 1f;
        private double freeSpendingUntil;

        public float Max => max;
        public float Current { get; private set; }
        public float FreeSpendingRemaining => Mathf.Max(0f, (float)(freeSpendingUntil - Time.timeAsDouble));
        public bool HasFreeSpending => FreeSpendingRemaining > 0f;
        public StaminaRegenMode RegenMode { get; set; } = StaminaRegenMode.Idle;

        public event Action<float, float> Changed;

        private void Awake()
        {
            Current = max;
        }

        private void Update()
        {
            if (delayRemaining > 0f)
            {
                delayRemaining -= Time.deltaTime;
                return;
            }

            float rate = RegenMode switch
            {
                StaminaRegenMode.Idle => idleRegen,
                StaminaRegenMode.Moving => movingRegen,
                _ => 0f
            };

            if (rate > 0f && Current < max)
            {
                Set(Current + rate * Time.deltaTime);
            }
        }

        public bool CanAfford(float cost)
        {
            return HasFreeSpending || Current >= cost * CostMultiplier;
        }

        /// <summary>一次性消耗（攻击、翻滚）。体力不够时不扣，返回 false。</summary>
        public bool TrySpend(float cost)
        {
            return TrySpend(cost, out _);
        }

        public bool TrySpend(float cost, out float spent)
        {
            spent = 0f;
            if (!CanAfford(cost))
            {
                return false;
            }

            spent = Drain(cost);
            return true;
        }

        /// <summary>持续消耗（奔跑），每帧调用。体力耗尽时返回 false。</summary>
        public bool TryDrain(float perSecond, float dt)
        {
            if (Current <= 0f && !HasFreeSpending)
            {
                return false;
            }

            Drain(perSecond * dt);
            return true;
        }

        /// <summary>扣到零为止，并返回实际支付的数值。</summary>
        public float Drain(float amount)
        {
            float spent = Mathf.Min(amount * CostMultiplier, Current);
            if (spent <= 0f || HasFreeSpending)
            {
                return 0f;
            }

            delayRemaining = regenDelay;
            Set(Current - spent);
            return spent;
        }

        public void Refund(float amount)
        {
            if (amount > 0f)
            {
                Set(Current + amount);
            }
        }

        public void GrantFreeSpending(float seconds)
        {
            freeSpendingUntil = Math.Max(freeSpendingUntil, Time.timeAsDouble + seconds);
        }

        private void Set(float value)
        {
            Current = Mathf.Clamp(value, 0f, max);
            Changed?.Invoke(Current, max);
        }
    }
}
