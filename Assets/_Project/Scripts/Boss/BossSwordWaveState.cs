using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossSwordWaveState : IState, IBossWindup
    {
        private readonly BossController boss;
        private BossSkillSettings.SwordWaveSettings settings;
        private float elapsed;
        private bool fired;

        public float WindupRemaining => Mathf.Max(0f, settings.windup - elapsed);

        public BossSwordWaveState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            settings = boss.Config.Skills.swordWave;
            elapsed = 0f;
            fired = false;
            boss.Weapons.WaveCharge(0f);
        }

        public void Tick(float dt)
        {
            if (elapsed < settings.windup - boss.Config.AimLockLead)
            {
                boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed, dt);
            }
            boss.Motor.Move(Vector3.zero, dt);
            elapsed += dt;

            if (elapsed < settings.windup)
            {
                boss.Weapons.WaveCharge(elapsed / settings.windup);
                return;
            }
            if (!fired)
            {
                fired = true;
                boss.Weapons.Swing(1, 0.5f);
                boss.SkillVisuals.LaunchWave(boss, settings);
            }
            if (elapsed >= settings.windup + settings.recovery)
            {
                boss.ToApproach();
            }
        }

        public void Exit() { }
    }
}
