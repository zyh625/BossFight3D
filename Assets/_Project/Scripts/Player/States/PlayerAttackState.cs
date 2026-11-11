using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Player.States
{
    public enum PlayerAttackKind
    {
        None,
        Light,
        Combination,
        ChargedHeavy,
        /// <summary>完美闪避后的突进斩。</summary>
        DodgeCounter
    }

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
        private AttackImpact impact;
        private bool canSeeThrough;
        private bool hitboxOpened;
        private Vector3 aimDirection;
        private float lungeSpeed;
        private float lungeRemaining;
        private Transform counterTarget;
        private bool isDodgeCounter;

        public PlayerAttackKind Kind { get; private set; }
        public bool IsWindingUp => !hitboxOpened;

        /// <summary>前摇进度 0~1。占位挥剑和之后的预备动作表现会读它。</summary>
        public float WindupRatio => windup > 0f ? Mathf.Clamp01(elapsed / windup) : 1f;

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
            counterTarget = null;
        }

        /// <summary>闪避反击：朝刚才被躲开的那个攻击者突进。之后按左键从连段第一段接上。</summary>
        public void SetupDodgeCounter(AttackData attack, Transform target)
        {
            Setup(attack, -1, 1f);
            counterTarget = target;
            isDodgeCounter = true;
        }

        public void Enter()
        {
            startedAt = ctx.ActionTime;
            elapsed = 0f;
            hitboxOpened = false;
            // 闪避反击是闪避的奖励，不是识破的出手，所以它不能再触发识破
            canSeeThrough = comboIndex < 0 && !isDodgeCounter;
            windup = canSeeThrough ? Mathf.Max(data.Windup, ctx.Combat.SeeThroughWindow) : data.Windup;
            guardDamage = data.GuardDamage;
            bool heavy = data == ctx.Combat.HeavyAttack;
            Kind = isDodgeCounter ? PlayerAttackKind.DodgeCounter
                : heavy ? PlayerAttackKind.ChargedHeavy
                : comboIndex >= 0 ? PlayerAttackKind.Light : PlayerAttackKind.Combination;
            impact = heavy ? AttackImpact.ChargedHeavy : AttackImpact.Normal;
            if (heavy)
            {
                ctx.Motor.TryJump(ctx.Combat.HeavyLeapHeight);
            }

            Vector2 move = ctx.Input.Move;
            if (!ctx.Movement.TryGetLockDirection(out aimDirection))
            {
                aimDirection = move.sqrMagnitude > 0.01f
                    ? ctx.Movement.ToWorldDirection(move).normalized
                    : ctx.Transform.forward;
            }

            if (isDodgeCounter)
            {
                // 翻滚时按着的方向键是躲避方向，不能拿来当攻击方向：直接转向被躲开的攻击者
                if (counterTarget != null)
                {
                    Vector3 toTarget = counterTarget.position - ctx.Transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        aimDirection = toTarget.normalized;
                    }
                }
                ctx.Transform.rotation = Quaternion.LookRotation(aimDirection);
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
            impact |= AttackImpact.Counter;
            lungeSpeed = data.Active > 0f ? lungeRemaining / data.Active : 0f;
            ctx.Stamina.GrantFreeSpending(ctx.Combat.FreeStaminaDuration);
            ctx.Stamina.Refund(staminaSpent);
            ctx.Visual.Play(data.AnimationState, 0f, data.Duration, data.Windup);
            return true;
        }

        public void Tick(float dt)
        {
            if (Kind == PlayerAttackKind.Light && IsWindingUp && ctx.ActionTime - startedAt <= ctx.Combat.ChargePresentationDelay
                && ctx.TryStartVariedAttack(staminaSpent))
            {
                return;
            }

            float activeEnd = windup + data.Active;

            // 翻滚是保命手段，取消时机比其他动作宽：轻攻击的前摇里随时可以滚（体力不退），
            // 任何攻击只要判定结束就可以滚，不必等到后摇的衔接点。
            // 切手技和重击的前摇不能滚——它们是"识破"的出手，出了就要承担风险。
            bool grounded = Kind != PlayerAttackKind.ChargedHeavy || ctx.Motor.IsGrounded;
            bool rollWindow = (Kind == PlayerAttackKind.Light && !hitboxOpened) || elapsed >= activeEnd;
            if (grounded && rollWindow && ctx.TryStartRoll())
            {
                return;
            }

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
                ctx.WeaponHitbox.Begin(ctx.GameObject, data.Damage * damageMultiplier, guardDamage, data.Active, impact);
                return;
            }

            if (elapsed >= activeEnd)
            {
                ctx.WeaponHitbox.End();
            }

            float skippedWindup = data.Windup - windup;
            bool canChain = Kind != PlayerAttackKind.ChargedHeavy || ctx.Motor.IsGrounded;
            if (canChain && elapsed >= data.CancelTime - skippedWindup
                && (ctx.TryStartRoll() || ctx.TryStartJump() || ctx.TryStartVariedAttack()
                    || ctx.TryStartLightAttack(comboIndex + 1) || ctx.TryStartCharge()))
            {
                return;
            }

            // 过了衔接点之后，推方向键也能提前结束后摇——否则"打完想走却走不动"
            bool wantsMove = elapsed >= data.CancelTime - skippedWindup
                && ctx.Input.Move.sqrMagnitude >= ctx.Combat.MoveCancelThreshold * ctx.Combat.MoveCancelThreshold;
            if (canChain && (wantsMove || elapsed >= data.Duration - skippedWindup))
            {
                ctx.ToLocomotion();
            }
        }

        public void Exit()
        {
            ctx.WeaponHitbox.End();
            isDodgeCounter = false;
        }
    }
}
