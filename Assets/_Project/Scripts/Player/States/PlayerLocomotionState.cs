using BossFight.Character;
using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    /// <summary>自由移动：待机 / 走 / 跑。是所有动作结束后回到的默认状态。</summary>
    public sealed class PlayerLocomotionState : IState
    {
        private const float MovingSpeedThreshold = 0.1f;

        private readonly PlayerContext ctx;

        // 体力跑空之后必须松开奔跑键才能再次奔跑，否则体力一恢复就又被扣掉，角色会一卡一卡地跑
        private bool sprintExhausted;

        public PlayerLocomotionState(PlayerContext context)
        {
            ctx = context;
        }

        public void Enter()
        {
            ctx.Visual.Play(AnimParams.LocomotionState, 0.15f);
        }

        public void Tick(float dt)
        {
            if (ctx.Input.SwitchWeaponPressedThisFrame)
            {
                ctx.CycleForm();
            }

            if (ctx.TryStartRoll() || ctx.TryStartJump() || ctx.TryStartVariedAttack() || ctx.TryStartDodgeCounter() || ctx.TryStartLightAttack(0) || ctx.TryStartCharge())
            {
                return;
            }

            Vector2 move = ctx.Input.Move;
            bool sprint = UpdateSprint(move, dt);

            ctx.Movement.Tick(move, sprint, dt);

            if (sprint)
            {
                ctx.Stamina.RegenMode = StaminaRegenMode.None;
            }
            else
            {
                ctx.Stamina.RegenMode = ctx.Motor.PlanarSpeed > MovingSpeedThreshold
                    ? StaminaRegenMode.Moving
                    : StaminaRegenMode.Idle;
            }
        }

        public void Exit()
        {
            ctx.Movement.ResetVelocity();
            ctx.Stamina.RegenMode = StaminaRegenMode.None;
        }

        private bool UpdateSprint(Vector2 move, float dt)
        {
            if (!ctx.Input.SprintHeld)
            {
                sprintExhausted = false;
                return false;
            }

            if ((sprintExhausted && !ctx.Stamina.HasFreeSpending) || move.sqrMagnitude < 0.01f)
            {
                return false;
            }

            if (ctx.Stamina.TryDrain(ctx.Combat.SprintStaminaPerSecond, dt))
            {
                return true;
            }

            sprintExhausted = true;
            return false;
        }
    }
}
