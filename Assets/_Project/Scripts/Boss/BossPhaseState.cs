using BossFight.Character;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossPhaseState : IState
    {
        private readonly BossController boss;
        private BossSkillSettings.PhaseSettings settings;
        private float elapsed;

        public float Remaining => Mathf.Max(0f, settings.duration - elapsed);

        public BossPhaseState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            settings = boss.Config.Skills.phase;
            elapsed = 0f;
            boss.Guard.Restore();
            boss.Weapons.SetPhaseTwo();
            boss.Weapons.Windup(2, 1f);

            Vector3 direction = boss.DirectionToTarget();
            if (direction.sqrMagnitude <= settings.burstRadius * settings.burstRadius
                && boss.Target.TryGetComponent(out CharacterMotor motor))
            {
                Vector3 pushDirection = direction.sqrMagnitude > 0f ? direction : boss.transform.forward;
                motor.Push(pushDirection, settings.knockbackDistance, settings.knockbackDuration);
            }
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            boss.Motor.Move(Vector3.zero, dt);
            float expansion = Mathf.Clamp01(elapsed / (settings.duration * 0.35f));
            boss.SkillVisuals?.ShowRing(boss.transform.position, settings.burstRadius * expansion,
                new Color(1f, 0.12f, 0.65f) * Mathf.Lerp(1f, 0.35f, elapsed / settings.duration));
            if (elapsed >= settings.duration)
            {
                boss.ToApproach();
            }
        }

        public void Exit() => boss.SkillVisuals?.HideRing();
    }
}
