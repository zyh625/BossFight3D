using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>
    /// 二阶段的变形：把双剑合成一把长剑。变形本身只是一小段演出，结束时通知控制器进入长剑形态，
    /// 之后攻击距离、连招和技能范围的变化都由控制器上的"是否长剑形态"决定，招式状态不需要各自判断形态。
    /// </summary>
    internal sealed class BossFormState : IState
    {
        private readonly BossController boss;
        private float elapsed;
        private float duration;

        public BossFormState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            elapsed = 0f;
            duration = Mathf.Max(0.01f, boss.Config.Skills.longsword.transformTime);
            boss.Weapons.Merge(0f);
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed, dt);
            boss.Motor.Move(Vector3.zero, dt);
            boss.Weapons.Merge(Mathf.Clamp01(elapsed / duration));

            if (elapsed >= duration)
            {
                boss.BeginLongsword();
                boss.ToApproach();
            }
        }

        public void Exit() { }
    }
}
