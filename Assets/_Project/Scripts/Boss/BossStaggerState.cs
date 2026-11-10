using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossStaggerState : IState
    {
        private readonly BossController boss;
        private float remaining;

        public BossStaggerState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            bool broken = boss.Status == BossStatus.Broken;
            remaining = broken ? boss.Config.BrokenDuration : boss.Config.HitStun;
            boss.Weapons.Stagger(broken);
        }

        public void Tick(float dt)
        {
            remaining -= dt;
            boss.Motor.Move(Vector3.zero, dt);
            if (remaining <= 0f)
            {
                boss.ToApproach();
            }
        }

        public void Exit() { }
    }

    internal sealed class BossStoppedState : IState
    {
        private readonly BossController boss;

        public BossStoppedState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            boss.Weapons.Rest();
            if (boss.Status == BossStatus.Dead)
            {
                boss.Weapons.Fall();
            }
        }

        public void Tick(float dt) { }
        public void Exit() { }
    }
}
