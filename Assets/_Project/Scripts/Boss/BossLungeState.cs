using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>
    /// 后撤突进：后撤瞬身收招之后有一定概率接这一招——短暂蓄势，然后沿直线高速冲向玩家并挥出交叉斩。
    /// 它让"后撤"从单纯的躲避变成进攻的起手：玩家追上去贪刀会迎面撞上突进。
    /// 突进方向在出手瞬间锁定，所以横向翻滚可以躲开。
    /// </summary>
    internal sealed class BossLungeState : IState, IBossWindup
    {
        private const int CrossSlash = 2;
        private const float MinimumSwingTime = 0.12f;

        private readonly BossController boss;
        private BossSkillSettings.LungeSettings settings;
        private Vector3 direction;
        private float elapsed;
        private float dashDistance;
        private float travelled;
        private float swingTime;
        private float recoveryElapsed;
        private bool launched;

        public float WindupRemaining => Mathf.Max(0f, settings.windup - elapsed);

        public BossLungeState(BossController boss) => this.boss = boss;

        public void Enter()
        {
            settings = boss.Config.Skills.lunge;
            elapsed = 0f;
            travelled = 0f;
            swingTime = 0f;
            recoveryElapsed = 0f;
            launched = false;
            boss.Weapons.Windup(CrossSlash, 0f);
        }

        public void Tick(float dt)
        {
            elapsed += dt;

            if (elapsed < settings.windup)
            {
                boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed * 2f, dt);
                boss.Motor.Move(Vector3.zero, dt);
                boss.Weapons.Windup(CrossSlash, elapsed / settings.windup);
                return;
            }

            if (!launched)
            {
                launched = true;
                Vector3 toTarget = boss.DirectionToTarget();
                direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : boss.transform.forward;
                dashDistance = Mathf.Clamp(toTarget.magnitude - settings.stopDistance, 0f, settings.maxDistance);
                boss.transform.rotation = Quaternion.LookRotation(direction);

                float expectedTime = Mathf.Max(MinimumSwingTime, dashDistance / settings.speed);
                boss.Weapons.Hitbox.Begin(boss.gameObject, settings.damage, 0f, expectedTime, targetFilter: boss.Target);
            }

            bool dashing = travelled < dashDistance;
            if (dashing || swingTime < MinimumSwingTime)
            {
                float step = dashing ? Mathf.Min(settings.speed * dt, dashDistance - travelled) : 0f;
                boss.Motor.Move(direction * (step / dt), dt);
                travelled += step;
                swingTime += dt;

                float progress = dashDistance > 0f ? travelled / dashDistance : swingTime / MinimumSwingTime;
                boss.Weapons.Swing(CrossSlash, Mathf.Clamp01(progress));
                return;
            }

            if (boss.Weapons.Hitbox.IsActive)
            {
                boss.Weapons.Hitbox.End();
                boss.Weapons.Rest();
            }

            recoveryElapsed += dt;
            boss.Motor.Move(Vector3.zero, dt);
            if (recoveryElapsed >= settings.recovery)
            {
                boss.ToApproach();
            }
        }

        public void Exit()
        {
            boss.Weapons.Hitbox.End();
        }
    }
}
