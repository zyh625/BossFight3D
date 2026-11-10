using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossRetreatState : IState
    {
        private readonly BossController boss;
        private BossSkillSettings.RetreatSettings settings;
        private Vector3 direction;
        private float elapsed;

        public bool IsEvading => elapsed < settings.duration;

        public BossRetreatState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            settings = boss.Config.Skills.retreat;
            elapsed = 0f;
            Vector3 towardTarget = boss.DirectionToTarget().normalized;
            direction = towardTarget.sqrMagnitude > 0f ? -towardTarget : -boss.transform.forward;
            boss.Weapons.Rest();
            boss.SkillVisuals.StartTrail();
        }

        public void Tick(float dt)
        {
            float moveTime = Mathf.Clamp(settings.duration - elapsed, 0f, dt);
            boss.Motor.Move(direction * (settings.distance / settings.duration * (moveTime / dt)), dt);
            elapsed += dt;

            if (!IsEvading)
            {
                boss.SkillVisuals.EndTrail();
            }
            if (elapsed >= settings.duration + settings.recovery)
            {
                boss.ToApproach();
            }
        }

        public void Exit() => boss.SkillVisuals.EndTrail();
    }
}
