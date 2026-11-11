using BossFight.Character;
using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    public enum BossStatus
    {
        Approach,
        Combo,
        Staggered,
        Broken,
        Dead,
        FightOver,
        BlinkSlash,
        SwordWave,
        Retreat,
        PhaseTransition,
        Whirlwind,
        Lunge,
        /// <summary>正在把双剑合成长剑。</summary>
        Transform
    }

    [RequireComponent(typeof(DamageReceiver), typeof(CharacterMotor), typeof(GuardMeter))]
    public sealed class BossController : MonoBehaviour, IDamageModifier, IProjectileReflector
    {
        [SerializeField] private BossCombatConfig config;
        [SerializeField] private BossWeaponRig weapons;
        [SerializeField] private BossSkillVisuals skillVisuals;
        [SerializeField] private DamageReceiver target;

        private readonly StateMachine machine = new StateMachine();
        private LocalHitStop hitStop;
        private Health health;
        private IState approach;
        private BossComboState combo;
        private BossBlinkSlashState blinkSlash;
        private BossSwordWaveState swordWave;
        private BossRetreatState retreat;
        private BossWhirlwindState whirlwind;
        private BossLungeState lunge;
        private BossFormState form;
        private double longswordUntil;
        private double longswordReadyAt;
        private BossPhaseState phaseTransition;
        private BossStaggerState stagger;
        private IState stopped;
        private BossMove activeMove;

        public DamageReceiver Receiver { get; private set; }
        public GuardMeter Guard { get; private set; }
        public BossStatus Status { get; private set; }
        public bool IsPhaseTwo { get; private set; }
        /// <summary>二阶段的长剑形态：攻击更远、连招变为四连击、不格挡也不会被打断。</summary>
        public bool IsLongsword { get; private set; }

        /// <summary>旋风斩旋转期间会把飞镖弹回去。</summary>
        public bool ReflectsProjectiles => Status == BossStatus.Whirlwind && whirlwind.IsSpinning;

        // 长剑形态不格挡：每一击都同时扣血和扣格挡条，换来的是它不会被打断
        public bool CanGuard => Status == BossStatus.Approach && !IsLongsword && !Guard.IsWeak && Guard.Current > 0f;
        public int StrikeNumber => Status == BossStatus.Combo ? combo.Index + 1 : 0;
        public int ComboLength => config.GetComboLength(IsLongsword);
        internal AttackData GetStrike(int index) => config.GetStrike(index, IsLongsword);
        internal float AttackDistance => IsLongsword ? config.Skills.longsword.attackDistance : config.AttackDistance;
        internal float SkillScale => IsLongsword ? config.Skills.longsword.skillScale : 1f;
        public float WindupRemaining => machine.Current is IBossWindup windup ? windup.WindupRemaining : 0f;
        public bool IsStriking => Status is BossStatus.Combo or BossStatus.Whirlwind or BossStatus.Lunge
            && weapons.Hitbox.IsActive;
        public float PhaseTransitionRemaining => Status == BossStatus.PhaseTransition ? phaseTransition.Remaining : 0f;

        public BossCombatConfig Config => config;
        internal BossWeaponRig Weapons => weapons;
        internal BossSkillVisuals SkillVisuals => skillVisuals;
        internal DamageReceiver Target => target;
        internal CharacterMotor Motor { get; private set; }
        internal BossBrain Brain { get; private set; }
        internal double ActionTime => hitStop != null ? hitStop.ActionTime : Time.timeAsDouble;

        int IDamageModifier.Order => 200;

        private void Awake()
        {
            Receiver = GetComponent<DamageReceiver>();
            health = GetComponent<Health>();
            Guard = GetComponent<GuardMeter>();
            Motor = GetComponent<CharacterMotor>();
            hitStop = GetComponent<LocalHitStop>();
            Brain = new BossBrain(config);
            approach = new BossApproachState(this);
            combo = new BossComboState(this);
            blinkSlash = new BossBlinkSlashState(this);
            swordWave = new BossSwordWaveState(this);
            retreat = new BossRetreatState(this);
            whirlwind = new BossWhirlwindState(this);
            lunge = new BossLungeState(this);
            form = new BossFormState(this);
            phaseTransition = new BossPhaseState(this);
            stagger = new BossStaggerState(this);
            stopped = new BossStoppedState(this);
        }

        private void OnEnable()
        {
            Receiver.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            Receiver.Damaged -= OnDamaged;
            health.Died -= OnDied;
            weapons.Hitbox.End();
            if (skillVisuals != null)
            {
                skillVisuals.Clear();
            }
        }

        private void Start()
        {
            if (!health.IsDead)
            {
                ToApproach();
            }
        }

        private void Update()
        {
            if (Status is BossStatus.Dead or BossStatus.FightOver)
            {
                return;
            }

            if (target == null || target.Health.IsDead)
            {
                ChangeState(BossStatus.FightOver, stopped);
                return;
            }

            float dt = hitStop != null ? hitStop.DeltaTime : Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            if (Status is not (BossStatus.Broken or BossStatus.PhaseTransition))
            {
                Guard.Tick(dt);
            }
            machine.Tick(dt);
        }

        /// <summary>到目标的水平距离。调试显示和测试使用。</summary>
        public float DirectionToTargetDistance => target != null ? DirectionToTarget().magnitude : 0f;

        internal Vector3 DirectionToTarget()
        {
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            return direction;
        }

        internal void ToApproach()
        {
            if (Status == BossStatus.Broken)
            {
                Guard.BeginWeakness();
            }

            ChangeState(BossStatus.Approach, approach);
        }

        internal void ToMove(BossMove move)
        {
            switch (move)
            {
                case BossMove.Combo: ChangeState(BossStatus.Combo, combo, move); break;
                case BossMove.BlinkSlash: ChangeState(BossStatus.BlinkSlash, blinkSlash, move); break;
                case BossMove.SwordWave: ChangeState(BossStatus.SwordWave, swordWave, move); break;
                case BossMove.Retreat: ChangeState(BossStatus.Retreat, retreat, move); break;
                case BossMove.Whirlwind: ChangeState(BossStatus.Whirlwind, whirlwind, move); break;
                case BossMove.Lunge: ChangeState(BossStatus.Lunge, lunge, move); break;
            }
        }

        /// <summary>二阶段、玩家离得够远、冷却结束时，开始合剑。由追击状态在选招之前询问。</summary>
        internal bool TryBeginTransform(float distance)
        {
            var settings = config.Skills.longsword;
            if (!settings.enabled || !IsPhaseTwo || IsLongsword || skillVisuals == null || !config.HasLongswordCombo
                || distance < settings.minDistance || ActionTime < longswordReadyAt)
            {
                return false;
            }

            ChangeState(BossStatus.Transform, form);
            return true;
        }

        internal void BeginLongsword()
        {
            IsLongsword = true;
            longswordUntil = ActionTime + config.Skills.longsword.duration;
            weapons.SetLongsword(true, config.Skills.longsword.reachScale);
        }

        internal void ExpireLongswordIfDue()
        {
            if (IsLongsword && ActionTime >= longswordUntil)
            {
                EndLongsword();
            }
        }

        private void EndLongsword()
        {
            IsLongsword = false;
            longswordReadyAt = ActionTime + config.Skills.longsword.cooldown;
            weapons.SetLongsword(false, 1f);
        }

        private void ChangeState(BossStatus status, IState state, BossMove move = BossMove.None)
        {
            Brain.FinishMove(activeMove, ActionTime);
            activeMove = move;
            Status = status;
            machine.ChangeState(state);
        }

        void IDamageModifier.Modify(DamageContext damage)
        {
            if (!isActiveAndEnabled || Receiver.Health.IsDead || damage.Damage <= 0f)
            {
                return;
            }

            if (Status == BossStatus.PhaseTransition || (Status == BossStatus.Retreat && retreat.IsEvading))
            {
                damage.Outcome = Status == BossStatus.Retreat ? HitOutcome.Dodged : HitOutcome.Invulnerable;
                damage.Cancelled = true;
            }
            else if (CanGuard)
            {
                Guard.Absorb(damage.Info.GuardDamage);
                damage.Damage *= config.GuardedDamageScale;
                damage.Outcome = HitOutcome.Blocked;
            }
            else if (Status == BossStatus.Broken)
            {
                bool finisher = (damage.Info.Impact & AttackImpact.ChargedHeavy) != 0;
                damage.Damage *= finisher ? config.BrokenHeavyDamageScale : config.BrokenDamageScale;
                if (finisher)
                {
                    damage.Info.Impact |= AttackImpact.GuardBreakFinisher;
                }
            }
            else if (IsLongsword)
            {
                Guard.Absorb(damage.Info.GuardDamage);
            }
            else if (CanCounterDrainGuard(damage.Info))
            {
                Guard.Absorb(damage.Info.GuardDamage);
            }
        }

        private void OnDamaged(DamageResult result)
        {
            if (result.Killed)
            {
                return;
            }

            if (result.FinalDamage <= 0f && result.Outcome != HitOutcome.Blocked)
            {
                return;
            }
            Guard.DelayRecovery();

            if (!IsPhaseTwo && target != null && !target.Health.IsDead
                && Receiver.Health.Current <= Receiver.Health.Max * config.Skills.phase.healthThreshold)
            {
                IsPhaseTwo = true;
                ChangeState(BossStatus.PhaseTransition, phaseTransition);
                Brain.EnterPhaseTwo(ActionTime);
            }
            else if (Guard.Current <= 0f && Status != BossStatus.Broken
                && (result.Outcome == HitOutcome.Blocked || IsLongsword || CanCounterDrainGuard(result.Info)))
            {
                // 长剑形态下格挡条被打空：形态被打散，并进入破防
                if (IsLongsword)
                {
                    EndLongsword();
                }
                ChangeState(BossStatus.Broken, stagger);
            }
            else if (!IsLongsword && Guard.IsWeak && result.FinalDamage > 0f
                && result.Info.Impact != AttackImpact.Normal
                && (Status == BossStatus.Approach || WindupRemaining > 0f))
            {
                ChangeState(BossStatus.Staggered, stagger);
            }
            else if (Brain.RecordHitAndTryRetreat(CanGuard && skillVisuals != null && target != null
                && config.Skills.retreat.rule.InRange(DirectionToTarget().magnitude), ActionTime))
            {
                ToMove(BossMove.Retreat);
            }
        }

        private void OnDied() => ChangeState(BossStatus.Dead, stopped);

        private bool CanCounterDrainGuard(DamageInfo info)
        {
            // 识破的强化反击能在 Boss 出招时打掉格挡条。
            return (Status is BossStatus.Combo or BossStatus.BlinkSlash or BossStatus.SwordWave
                    or BossStatus.Whirlwind or BossStatus.Lunge)
                && !Guard.IsWeak && (info.Impact & AttackImpact.Counter) != 0;
        }
    }
}
