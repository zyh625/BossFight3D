using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 一个角色受伤的唯一入口，也是伤害结算管线本身：
    /// DamageInfo → 依次经过本角色身上的 IDamageModifier → 扣血 → 广播 DamageResult。
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DamageReceiver : MonoBehaviour
    {
        private readonly List<IDamageModifier> modifiers = new List<IDamageModifier>();
        private readonly DamageContext context = new DamageContext();
        private Health health;

        public Health Health => health;

        /// <summary>每次结算完成后触发，无论是否真的掉血（被闪避、被格挡也会触发）。</summary>
        public event Action<DamageResult> Damaged;

        private void Awake()
        {
            health = GetComponent<Health>();
            GetComponents(modifiers);
            modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            context.Info = info;
            context.Damage = info.Amount;
            context.Outcome = HitOutcome.Hit;
            context.Cancelled = false;

            foreach (IDamageModifier modifier in modifiers)
            {
                modifier.Modify(context);
                if (context.Cancelled)
                {
                    break;
                }
            }

            float before = health.Current;
            if (!context.Cancelled)
            {
                health.ApplyDamage(context.Damage);
            }

            var result = new DamageResult
            {
                Info = context.Info,
                Outcome = context.Outcome,
                FinalDamage = before - health.Current,
                Target = this,
                Killed = before > 0f && health.IsDead
            };

            Damaged?.Invoke(result);
            return result;
        }
    }
}
