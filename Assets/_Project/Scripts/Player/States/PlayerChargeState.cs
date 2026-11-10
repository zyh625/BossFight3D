using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    /// <summary>
    /// 蓄力：按住右键期间停留在这里，可以缓慢移动。
    /// 蓄力持续消耗体力，松键或耗尽时按当前蓄力进度打出重击。
    /// </summary>
    public sealed class PlayerChargeState : IState
    {
        private readonly PlayerContext ctx;
        private float chargeTime;
        private float staminaSpent;
        private bool presented;

        public PlayerChargeState(PlayerContext context)
        {
            ctx = context;
        }

        /// <summary>当前蓄力进度 0~1。调试显示和之后的蓄力特效会读它。</summary>
        public float ChargeRatio => Mathf.Clamp01(chargeTime / ctx.Combat.FullChargeTime);
        public float DisplayRatio => presented ? ChargeRatio : 0f;

        public void Enter()
        {
            chargeTime = 0f;
            staminaSpent = 0f;
            presented = false;
            ctx.Stamina.RegenMode = StaminaRegenMode.None;
        }

        public void Tick(float dt)
        {
            if (ctx.TryStartRoll() || ctx.TryStartJump() || ctx.TryStartVariedAttack(presented ? 0f : staminaSpent))
            {
                return;
            }
            if (ctx.ChargeHoldConsumed)
            {
                if (!presented)
                {
                    ctx.Stamina.Refund(staminaSpent);
                }
                ctx.ToLocomotion();
                return;
            }

            // 只累加真正蓄力的时间。耗尽发生在帧内时，最后一帧只计剩余体力可支付的部分。
            float drainRate = ctx.Combat.ChargeStaminaPerSecond;
            float chargingDt = ctx.Stamina.HasFreeSpending ? dt : Mathf.Min(dt, ctx.Stamina.Current / drainRate);
            chargeTime += chargingDt;
            staminaSpent += ctx.Stamina.Drain(drainRate * dt);

            if (!ctx.Input.ChargeHeld || (ctx.Stamina.Current <= 0f && !ctx.Stamina.HasFreeSpending))
            {
                ReleaseHeavy();
                return;
            }

            if (!presented && chargeTime >= ctx.Combat.ChargePresentationDelay)
            {
                presented = true;
                ctx.Visual.Play(ctx.Combat.ChargeAnimationState, 0.1f);
            }

            ctx.Movement.Tick(ctx.Input.Move, false, dt, ctx.Combat.ChargeMoveMultiplier);
        }

        private void ReleaseHeavy()
        {
            ctx.ChargeHoldConsumed = ctx.Input.ChargeHeld;
            float multiplier = Mathf.Lerp(1f, ctx.Combat.FullChargeDamageMultiplier, ChargeRatio);
            ctx.Attack.Setup(ctx.Combat.HeavyAttack, -1, multiplier, staminaSpent);
            ctx.Machine.ChangeState(ctx.Attack);
        }

        public void Exit()
        {
            ctx.Movement.ResetVelocity();
        }
    }
}
