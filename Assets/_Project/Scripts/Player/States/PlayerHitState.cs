using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    public sealed class PlayerHitState : IState
    {
        private readonly PlayerContext ctx;
        private float elapsed;

        public PlayerHitState(PlayerContext context)
        {
            ctx = context;
        }

        public void Enter()
        {
            elapsed = 0f;
            ctx.Movement.ResetVelocity();
            ctx.Stamina.RegenMode = StaminaRegenMode.None;
            ctx.Visual.Play("Hit", 0.04f, ctx.Combat.HitStunDuration);
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            if (elapsed >= ctx.Combat.HitStunRollCancelTime && ctx.TryStartRoll())
            {
                return;
            }

            if (elapsed >= ctx.Combat.HitStunDuration)
            {
                ctx.ToLocomotion();
                return;
            }

            ctx.Motor.Move(Vector3.zero, dt);
        }

        public void Exit()
        {
        }
    }
}
