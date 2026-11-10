using BossFight.Character;
using BossFight.Combat;
using UnityEngine;

namespace BossFight.DevTools
{
    /// <summary>训练木桩：被打"死"之后过一会儿自动回满血，方便反复测试。</summary>
    [DefaultExecutionOrder(10)]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private float reviveDelay = 1.5f;

        [Header("陪练（Play 模式下在 Inspector 开启）")]
        [SerializeField] private bool sparringEnabled;
        [SerializeField] private DamageReceiver sparringTarget;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float windup = 0.65f;
        [SerializeField] private float attackInterval = 1.5f;
        [SerializeField] private float attackDamage = 12f;

        private float reviveTimer;
        private float attackTimer;
        private LocalHitStop hitStop;

        public bool IsWindingUp { get; private set; }
        public float WindupRemaining => IsWindingUp ? attackTimer : 0f;

        private void Awake()
        {
            hitStop = GetComponent<LocalHitStop>();
        }

        private void Update()
        {
            float dt = hitStop != null ? hitStop.DeltaTime : Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            if (health.IsDead)
            {
                IsWindingUp = false;
                attackTimer = 0f;
                reviveTimer += dt;
                if (reviveTimer >= reviveDelay)
                {
                    reviveTimer = 0f;
                    health.RestoreFull();
                }
                return;
            }

            if (!sparringEnabled || sparringTarget == null || sparringTarget.Health.IsDead)
            {
                IsWindingUp = false;
                attackTimer = 0f;
                return;
            }

            attackTimer -= dt;
            if (attackTimer > 0f)
            {
                return;
            }

            Vector3 direction = sparringTarget.transform.position - transform.position;
            direction.y = 0f;
            bool inRange = direction.sqrMagnitude <= attackRange * attackRange;
            if (!IsWindingUp)
            {
                IsWindingUp = inRange;
                attackTimer = inRange ? windup : 0f;
                return;
            }

            IsWindingUp = false;
            attackTimer = attackInterval;
            if (inRange)
            {
                sparringTarget.TakeDamage(new DamageInfo
                {
                    Amount = attackDamage,
                    Attacker = gameObject,
                    HitPoint = sparringTarget.transform.position + Vector3.up,
                    Direction = direction.normalized
                });
            }
        }
    }
}
