using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 攻击判定体：一个跟随武器的胶囊（由刀根、刀尖两个点和半径定义），只在攻击的判定时间内开启。
    ///
    /// 两个要点：
    /// 1. 扫掠检测。快刀一帧能挥过很大角度，只检测当前姿态会漏掉中间的目标。
    ///    所以每帧在"上一帧姿态 → 这一帧姿态"之间插值出若干个中间胶囊，逐个检测。
    /// 2. 去重。一次挥砍会连续多帧碰到同一个目标，用集合记录已命中的对象，保证一刀只算一次。
    ///
    /// 检测放在 LateUpdate：此时动画已经更新完骨骼，武器的位置才是这一帧的最终位置。
    /// </summary>
    public sealed class Hitbox : MonoBehaviour
    {
        private const int MaxHitsPerQuery = 16;

        [Tooltip("刀根（靠近握把的一端）")]
        [SerializeField] private Transform basePoint;
        [Tooltip("刀尖")]
        [SerializeField] private Transform tipPoint;
        [SerializeField] private float radius = 0.15f;

        [Tooltip("只检测这些层上的 Hurtbox")]
        [SerializeField] private LayerMask hurtboxLayers;

        [Tooltip("两个相邻检测胶囊之间，刀尖最多移动多远（米）。越小越精确、开销越大")]
        [SerializeField] private float maxStepDistance = 0.25f;

        private readonly Collider[] overlapBuffer = new Collider[MaxHitsPerQuery];
        private readonly HashSet<DamageReceiver> alreadyHit = new HashSet<DamageReceiver>();

        private DamageReceiver ownerReceiver;
        private GameObject attacker;
        private float damage;
        private float guardDamage;
        private AttackImpact impact;
        private DamageReceiver onlyTarget;
        private Vector3 previousBase;
        private Vector3 previousTip;

        public bool IsActive { get; private set; }

        /// <summary>判定开启时触发（判定持续时长）。占位挥剑、刀光拖尾订阅它。</summary>
        public event Action<float> Activated;

        public event Action Deactivated;

        /// <summary>命中任何目标并完成结算后触发。</summary>
        public event Action<DamageResult> HitLanded;

        /// <summary>开启判定。</summary>
        /// <param name="owner">攻击者。它自己的 Hurtbox 会被忽略。</param>
        public void Begin(GameObject owner, float damageAmount, float guardDamageAmount, float activeDuration,
            AttackImpact attackImpact = AttackImpact.Normal, DamageReceiver targetFilter = null)
        {
            attacker = owner;
            ownerReceiver = owner.GetComponent<DamageReceiver>();
            damage = damageAmount;
            guardDamage = guardDamageAmount;
            impact = attackImpact;
            onlyTarget = targetFilter;

            alreadyHit.Clear();
            previousBase = basePoint.position;
            previousTip = tipPoint.position;
            IsActive = true;
            Activated?.Invoke(activeDuration);
        }

        public void End()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            Deactivated?.Invoke();
        }

        private void LateUpdate()
        {
            if (!IsActive)
            {
                return;
            }

            Vector3 currentBase = basePoint.position;
            Vector3 currentTip = tipPoint.position;

            float travel = Mathf.Max(
                Vector3.Distance(previousBase, currentBase),
                Vector3.Distance(previousTip, currentTip));
            int steps = Mathf.Max(1, Mathf.CeilToInt(travel / maxStepDistance));

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Query(Vector3.Lerp(previousBase, currentBase, t), Vector3.Lerp(previousTip, currentTip, t));
            }

            previousBase = currentBase;
            previousTip = currentTip;
        }

        private void Query(Vector3 capsuleBase, Vector3 capsuleTip)
        {
            int count = Physics.OverlapCapsuleNonAlloc(
                capsuleBase, capsuleTip, radius, overlapBuffer, hurtboxLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (!other.TryGetComponent(out Hurtbox hurtbox) || hurtbox.Owner == null)
                {
                    continue;
                }

                DamageReceiver target = hurtbox.Owner;
                if (target == ownerReceiver || (onlyTarget != null && target != onlyTarget) || !alreadyHit.Add(target))
                {
                    continue;
                }

                Vector3 hitPoint = other.ClosestPoint((capsuleBase + capsuleTip) * 0.5f);
                Vector3 direction = target.transform.position - attacker.transform.position;
                direction.y = 0f;

                DamageResult result = target.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    GuardDamage = guardDamage,
                    Impact = impact,
                    Attacker = attacker,
                    HitPoint = hitPoint,
                    Direction = direction.normalized
                });

                HitLanded?.Invoke(result);
            }
        }

        // 调试可视化：在 Scene 视图（或打开 Gizmos 的 Game 视图）里画出判定胶囊。
        // 灰色 = 未开启，红色 = 判定中。
        private void OnDrawGizmos()
        {
            if (basePoint == null || tipPoint == null)
            {
                return;
            }

            Gizmos.color = IsActive ? new Color(1f, 0.15f, 0.15f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
            Vector3 a = basePoint.position;
            Vector3 b = tipPoint.position;
            Gizmos.DrawWireSphere(a, radius);
            Gizmos.DrawWireSphere(b, radius);
            Gizmos.DrawLine(a, b);
        }
    }
}
