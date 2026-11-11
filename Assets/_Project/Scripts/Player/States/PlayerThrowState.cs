using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    /// <summary>
    /// 投掷飞镖。时间线和近战攻击一样是"前摇 → 出手 → 后摇"，只是出手的那一刻不是开启判定体，
    /// 而是让发射器发出一支飞镖。后摇过了衔接点可以接下一次投掷、翻滚或移动，所以连点左键能连续投掷。
    /// </summary>
    public sealed class PlayerThrowState : IState
    {
        private readonly PlayerContext ctx;
        private float chargeRatio;
        private float elapsed;
        private bool released;

        public PlayerThrowState(PlayerContext context)
        {
            ctx = context;
        }

        /// <param name="charge">蓄力进度 0~1，0 是普通飞镖。</param>
        public void Setup(float charge)
        {
            chargeRatio = charge;
        }

        public void Enter()
        {
            elapsed = 0f;
            released = false;
            ctx.Stamina.RegenMode = StaminaRegenMode.None;

            // 出手前先转向瞄准方向
            ctx.Transform.rotation = Quaternion.LookRotation(ctx.Darts.GetAimDirection());
            ctx.Visual.Play("Throw", 0.05f, ctx.Combat.ThrowWindup + ctx.Combat.ThrowRecovery);
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            ctx.Motor.Move(Vector3.zero, dt);

            if (!released && elapsed >= ctx.Combat.ThrowWindup)
            {
                released = true;
                ctx.Darts.Fire(chargeRatio);
            }

            if (!released)
            {
                return;
            }

            float sinceRelease = elapsed - ctx.Combat.ThrowWindup;
            if (sinceRelease >= ctx.Combat.ThrowRecovery * 0.5f)
            {
                if (ctx.TryStartRoll() || ctx.TryStartJump() || ctx.TryStartLightAttack(0) || ctx.TryStartCharge())
                {
                    return;
                }

                float threshold = ctx.Combat.MoveCancelThreshold;
                if (ctx.Input.Move.sqrMagnitude >= threshold * threshold)
                {
                    ctx.ToLocomotion();
                    return;
                }
            }

            if (sinceRelease >= ctx.Combat.ThrowRecovery)
            {
                ctx.ToLocomotion();
            }
        }

        public void Exit() { }
    }
}
