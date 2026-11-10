using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    /// <summary>轻击、切手技和蓄力重击共用时序。识破会跳过本次攻击剩余的前摇。</summary>
    public sealed class PlayerAttackState : IState
    {
        private readonly PlayerContext ctx;

        private AttackData data;
        private int comboIndex;
        private float damageMultiplier;
        private float staminaSpent;

        private double startedAt;
        private float elapsed;
        private float windup;
        private float guardDamage;
        private bool canSeeThrough;
        private bool hitboxOpened;
        private Vector3 aimDirection;
        private float lungeSpeed;
        private float lungeRemaining;

        public PlayerAttackState(PlayerContext context)
        {
            ctx = context;
        }

        /// <param name="index">在轻攻击连段中的序号；切手技或重击传 -1。</param>
        public void Setup(AttackData attack, int index, float damageScale, float spent = 0f)
        {
            data = attack;
            comboIndex = index;
            damageMultiplier = damageScale;
            staminaSpent = spent;
        }

        public void Enter()
        {
            startedAt = ctx.ActionTime;
            elapsed = 0f;
            hitboxOpened = false;
            canSeeThrough = comboIndex < 0;
            windup = canSeeThrough ? Mathf.Max(data.Windup, ctx.Combat.SeeThroughWindow) : data.Windup;
            guardDamage = data.GuardDamage;

            Vector2 move = ctx.Input.Move;
            if (!ctx.Movement.TryGetLockDirection(out aimDirection))
            {
                aimDirection = move.sqrMagnitude > 0.01f
                    ? ctx.Movement.ToWorldDirection(move).normalized
                    : ctx.Transform.forward;
            }

            lungeRemaining = data.LungeDistance;
            float lungeTime = windup + data.Active;
            lungeSpeed = lungeTime > 0f ? data.LungeDistance / lungeTime : 0f;

            ctx.Stamina.RegenMode = StaminaRegenMode.None;
            ctx.Visual.Play(data.AnimationState, data.CrossFade, windup + data.Active + data.Recovery);
        }

        public bool TrySeeThrough()
        {
            if (!canSeeThrough || hitboxOpened || ctx.ActionTime - startedAt >= ctx.Combat.SeeThroughWindow)
            {
                return false;
            }

            canSeeThrough = false;
            if (ctx.Movement.TryGetLockDirection(out Vector3 facing))
            {
                ctx.Transform.rotation = Quaternion.LookRotation(facing);
            }
            windup = elapsed;
            damageMultiplier *= ctx.Combat.CounterDamageMultiplier;
            guardDamage += ctx.Combat.CounterBonusGuardDamage;
            lungeSpeed = data.Active > 0f ? lungeRemaining / data.Active : 0f;
            ctx.Stamina.GrantFreeSpending(ctx.Combat.FreeStaminaDuration);
            ctx.Stamina.Refund(staminaSpent);
            ctx.Visual.Play(data.AnimationState, 0f, data.Duration, data.Windup);
            return true;
        }

        public void Tick(float dt)
        {
            float activeEnd = windup + data.Active;
            float lungeTime = Mathf.Clamp(activeEnd - elapsed, 0f, dt);
            elapsed += dt;

            if (elapsed < windup)
            {
                if (ctx.Movement.TryGetLockDirection(out Vector3 facing))
                {
                    aimDirection = facing;
                }
                ctx.Motor.Face(aimDirection, ctx.Combat.AttackTurnSpeed, dt);
            }

            // 位移只计算前摇和判定区间，重力仍按完整的一帧更新。
            float travel = Mathf.Min(lungeRemaining, lungeSpeed * lungeTime);
            lungeRemaining -= travel;
            Vector3 velocity = dt > 0f ? ctx.Transform.forward * (travel / dt) : Vector3.zero;
            ctx.Motor.Move(velocity, dt);

            // 即使卡顿跳过整个判定区间，也给 LateUpdate 留一帧检测。
            if (!hitboxOpened && elapsed >= windup)
            {
                hitboxOpened = true;
                ctx.WeaponHitbox.Begin(ctx.GameObject, data.Damage * damageMultiplier, guardDamage, data.Active);
                return;
            }

            if (elapsed >= activeEnd)
            {
                ctx.WeaponHitbox.End();
            }

            float skippedWindup = data.Windup - windup;
            if (elapsed >= data.CancelTime - skippedWindup
                && (ctx.TryStartRoll() || ctx.TryStartVariedAttack()
                    || ctx.TryStartLightAttack(comboIndex + 1) || ctx.TryStartCharge()))
            {
                return;
            }

            if (elapsed >= data.Duration - skippedWindup)
            {
                ctx.ToLocomotion();
            }
        }

        public void Exit()
        {
            ctx.WeaponHitbox.End();
        }
    }
}
