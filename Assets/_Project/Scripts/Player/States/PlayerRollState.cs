using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    public sealed class PlayerRollState : IState
    {
        private readonly PlayerContext ctx;
        private Vector3 direction;
        private double startedAt;
        private float staminaSpent;
        private float elapsed;

        public PlayerRollState(PlayerContext context)
        {
            ctx = context;
        }

        public void Setup(float spent)
        {
            staminaSpent = spent;
        }

        public void Enter()
        {
            startedAt = ctx.ActionTime;
            elapsed = 0f;
            Vector2 move = ctx.Input.Move;
            direction = move.sqrMagnitude > 0.01f
                ? ctx.Movement.ToWorldDirection(move).normalized
                : ctx.Movement.TryGetLockDirection(out Vector3 facing) ? -facing : ctx.Transform.forward;
            ctx.Transform.rotation = Quaternion.LookRotation(direction);
            ctx.Stamina.RegenMode = StaminaRegenMode.None;
            ctx.Visual.Play("Roll", 0.05f, ctx.Combat.RollDuration);
        }

        public void Tick(float dt)
        {
            // 完美闪避之后，翻滚的任意时刻都可以直接接闪避反击
            if (ctx.TryStartDodgeCounter())
            {
                return;
            }

            float duration = ctx.Combat.RollDuration;
            float moveTime = Mathf.Min(dt, duration - elapsed);
            float speed = dt > 0f ? ctx.Combat.RollDistance / duration * (moveTime / dt) : 0f;
            ctx.Motor.Move(direction * speed, dt);
            elapsed += dt;

            // 翻滚的收尾阶段可以直接接下一个动作，不用等整个翻滚播完
            if (elapsed >= ctx.Combat.RollCancelTime
                && (ctx.TryStartRoll() || ctx.TryStartVariedAttack() || ctx.TryStartLightAttack(0)))
            {
                return;
            }

            if (elapsed >= duration)
            {
                ctx.ToLocomotion();
            }
        }

        public bool TryDodge()
        {
            double sinceStart = ctx.ActionTime - startedAt;
            if (sinceStart < ctx.Combat.RollInvulnerabilityStart || sinceStart >= ctx.Combat.RollInvulnerabilityEnd)
            {
                return false;
            }

            // 一次翻滚可能躲过多段攻击，只返还一次。
            float refund = staminaSpent;
            staminaSpent = 0f;
            ctx.Stamina.Refund(refund);
            return true;
        }

        public void Exit()
        {
        }
    }
}
