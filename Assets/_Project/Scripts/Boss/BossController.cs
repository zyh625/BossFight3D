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
        PhaseTransition
    }

    [RequireComponent(typeof(DamageReceiver), typeof(CharacterMotor), typeof(GuardMeter))]
    public sealed class BossController : MonoBehaviour, IDamageModifier
    {
        [SerializeField] private BossCombatConfig config;
        [SerializeField] private BossWeaponRig weapons;
        [SerializeField] private BossSkillVisuals skillVisuals;
        [SerializeField] private DamageReceiver target;

        private readonly StateMachine machine = new StateMachine();
        private LocalHitStop hitStop;
        private IState approach;
        private BossComboState combo;
        private BossBlinkSlashState blinkSlash;
        private BossSwordWaveState swordWave;
        private BossRetreatState retreat;
        private BossPhaseState phaseTransition;
        private BossStaggerState stagger;
        private IState stopped;
        private BossMove activeMove;

        public DamageReceiver Receiver { get; private set; }
        public GuardMeter Guard { get; private set; }
        public BossStatus Status { get; private set; }
        public bool IsPhaseTwo { get; private set; }
        public bool CanGuard => Status == BossStatus.Approach && !Guard.IsWeak && Guard.Current > 0f;
        public int StrikeNumber => Status == BossStatus.Combo ? combo.Index + 1 : 0;
        public int ComboLength => config.ComboLength;
        public float WindupRemaining => machine.Current is IBossWindup windup ? windup.WindupRemaining : 0f;
        public bool IsStriking => Status == BossStatus.Combo && weapons.Hitbox.IsActive;
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
            Guard = GetComponent<GuardMeter>();
            Motor = GetComponent<CharacterMotor>();
            hitStop = GetComponent<LocalHitStop>();
            Brain = new BossBrain(config);
            approach = new BossApproachState(this);
            combo = new BossComboState(this);
            blinkSlash = new BossBlinkSlashState(this);
            swordWave = new BossSwordWaveState(this);
            retreat = new BossRetreatState(this);
            phaseTransition = new BossPhaseState(this);
            stagger = new BossStaggerState(this);
            stopped = new BossStoppedState(this);
        }

        private void OnEnable()
        {
            Receiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            Receiver.Damaged -= OnDamaged;
            weapons.Hitbox.End();
            if (skillVisuals != null)
            {
                skillVisuals.Clear();
            }
        }

        private void Start()
        {
            ToApproach();
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
            }
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
                damage.Damage *= (damage.Info.Impact & AttackImpact.ChargedHeavy) != 0
                    ? config.BrokenHeavyDamageScale
                    : config.BrokenDamageScale;
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
                ChangeState(BossStatus.Dead, stopped);
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
            else if (Guard.Current <= 0f && (result.Outcome == HitOutcome.Blocked || CanCounterDrainGuard(result.Info)))
            {
                ChangeState(BossStatus.Broken, stagger);
            }
            else if (Guard.IsWeak && result.FinalDamage > 0f
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

        private bool CanCounterDrainGuard(DamageInfo info)
        {
            // 识破的强化反击能在 Boss 出招时打掉格挡条。
            return (Status is BossStatus.Combo or BossStatus.BlinkSlash or BossStatus.SwordWave)
                && !Guard.IsWeak && (info.Impact & AttackImpact.Counter) != 0;
        }
    }
}
