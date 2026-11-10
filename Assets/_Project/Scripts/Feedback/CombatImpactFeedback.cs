using BossFight.Character;
using BossFight.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>订阅角色的受击结果，驱动局部顿帧和镜头脉冲。</summary>
    [RequireComponent(typeof(DamageReceiver), typeof(LocalHitStop), typeof(CinemachineImpulseSource))]
    public sealed class CombatImpactFeedback : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float hitStopDuration = 0.035f;
        [SerializeField, Min(0f)] private float hitImpulse = 0.12f;
        [SerializeField, Min(0f)] private float parryImpulse = 0.22f;

        private DamageReceiver receiver;
        private LocalHitStop localHitStop;
        private CinemachineImpulseSource impulse;

        private void Awake()
        {
            receiver = GetComponent<DamageReceiver>();
            localHitStop = GetComponent<LocalHitStop>();
            impulse = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            receiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            receiver.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageResult result)
        {
            if (result.Outcome == HitOutcome.Parried)
            {
                // 识破后立即进入反击判定，命中时再顿帧。
                impulse.GenerateImpulseWithForce(parryImpulse);
                return;
            }

            if (result.FinalDamage <= 0f)
            {
                return;
            }

            localHitStop.FreezeFor(hitStopDuration);
            if (result.Info.Attacker != null && result.Info.Attacker.TryGetComponent(out LocalHitStop attacker))
            {
                attacker.FreezeFor(hitStopDuration);
            }
            impulse.GenerateImpulseWithForce(hitImpulse);
        }
    }
}
