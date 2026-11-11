using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>
    /// 旋风斩：短暂蓄势后高速旋转，一边转一边追向玩家，每隔一小段时间结算一次伤害；
    /// 旋转期间会把飞来的飞镖弹回去。转完之后有一段明显的"转晕了"的收招，是给玩家的反击窗口。
    ///
    /// 多段伤害的做法：Hitbox 一次开启只会命中同一个目标一次，所以每隔 hitInterval 重新开启一次，
    /// 每一段都是一次独立的判定——玩家可以对其中任何一段翻滚或识破。
    /// </summary>
    internal sealed class BossWhirlwindState : IState, IBossWindup
    {
        private const float MinimumDistance = 1.1f;

        private readonly BossController boss;
        private BossSkillSettings.WhirlwindSettings settings;
        private float elapsed;
        private float angle;
        private float nextHitAt;

        public bool IsSpinning { get; private set; }
        public float WindupRemaining => Mathf.Max(0f, settings.windup - elapsed);

        public BossWhirlwindState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            settings = boss.Config.Skills.whirlwind;
            elapsed = 0f;
            angle = 0f;
            nextHitAt = settings.windup;
            IsSpinning = false;
            boss.Weapons.WhirlWindup(0f);
        }

        public void Tick(float dt)
        {
            elapsed += dt;

            if (elapsed < settings.windup)
            {
                boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed, dt);
                boss.Motor.Move(Vector3.zero, dt);
                boss.Weapons.WhirlWindup(elapsed / settings.windup);
                return;
            }

            float spinEnd = settings.windup + settings.duration;
            if (elapsed < spinEnd)
            {
                IsSpinning = true;

                // 追向玩家，但贴得很近时停下，避免把玩家顶着走
                Vector3 toTarget = boss.DirectionToTarget();
                float speed = toTarget.magnitude > MinimumDistance ? settings.moveSpeed : 0f;
                boss.Motor.Move(toTarget.normalized * speed, dt);

                angle = Mathf.Repeat(angle + settings.spinSpeed * dt, 360f);
                boss.Weapons.Whirl(angle);

                if (elapsed >= nextHitAt)
                {
                    nextHitAt += settings.hitInterval;
                    boss.Weapons.Hitbox.End();
                    boss.Weapons.Hitbox.Begin(boss.gameObject, settings.damage, 0f,
                        settings.hitInterval, targetFilter: boss.Target);
                }
                return;
            }

            if (IsSpinning)
            {
                IsSpinning = false;
                boss.Weapons.Hitbox.End();
                boss.Weapons.Dizzy();
            }

            boss.Motor.Move(Vector3.zero, dt);
            if (elapsed >= spinEnd + settings.recovery)
            {
                boss.ToApproach();
            }
        }

        public void Exit()
        {
            IsSpinning = false;
            boss.Weapons.Hitbox.End();
            boss.Weapons.Rest();
        }
    }
}
