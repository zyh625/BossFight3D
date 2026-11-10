using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossApproachState : IState
    {
        private readonly BossController boss;

        public BossApproachState(BossController boss) => this.boss = boss;

        public void Enter() => boss.Weapons.Rest();

        public void Tick(float dt)
        {
            Vector3 direction = boss.DirectionToTarget();
            float distance = direction.magnitude;
            boss.Motor.Face(direction, boss.Config.TurnSpeed, dt);
            if (boss.Brain.CanStartCombo(distance, boss.ActionTime))
            {
                boss.ToCombo();
                return;
            }

            // 略微进入出招范围，避免浮点误差使角色停在判定距离之外。
            float stopDistance = Mathf.Max(0f, boss.Config.AttackDistance - 0.05f);
            float speed = Mathf.Clamp((distance - stopDistance) / dt, 0f, boss.Config.ChaseSpeed);
            boss.Motor.Move(direction.normalized * speed, dt);
        }

        public void Exit() { }
    }
}
