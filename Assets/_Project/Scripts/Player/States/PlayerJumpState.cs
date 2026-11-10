using BossFight.Combat;
using BossFight.Core;

namespace BossFight.Player.States
{
    public sealed class PlayerJumpState : IState
    {
        private readonly PlayerContext ctx;

        public PlayerJumpState(PlayerContext context) => ctx = context;

        public void Enter()
        {
            ctx.Stamina.RegenMode = StaminaRegenMode.None;
            ctx.Visual.Play("Jump", 0.06f);
        }

        public void Tick(float dt)
        {
            ctx.Movement.Tick(ctx.Input.Move, false, dt, ctx.Combat.AirMoveMultiplier);
            if (ctx.Motor.IsGrounded)
            {
                ctx.ToLocomotion();
            }
        }

        public void Exit() { }
    }
}
