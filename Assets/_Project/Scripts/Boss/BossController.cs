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
        FightOver
    }

    [RequireComponent(typeof(DamageReceiver), typeof(CharacterMotor), typeof(GuardMeter))]
    public sealed class BossController : MonoBehaviour, IDamageModifier
    {
        [SerializeField] private BossCombatConfig config;
        [SerializeField] private BossWeaponRig weapons;
        [SerializeField] private DamageReceiver target;

        private readonly StateMachine machine = new StateMachine();
        private LocalHitStop hitStop;
        private IState approach;
        private BossComboState combo;
        private BossStaggerState stagger;
        private IState stopped;

        public DamageReceiver Receiver { get; private set; }
        public GuardMeter Guard { get; private set; }
        public BossStatus Status { get; private set; }
        public bool CanGuard => Status == BossStatus.Approach && !Guard.IsWeak && Guard.Current > 0f;
        public int StrikeNumber => Status == BossStatus.Combo ? combo.Index + 1 : 0;
        public int ComboLength => config.ComboLength;
        public float WindupRemaining => Status == BossStatus.Combo ? combo.WindupRemaining : 0f;
        public bool IsStriking => Status == BossStatus.Combo && weapons.Hitbox.IsActive;

        internal BossCombatConfig Config => config;
        internal BossWeaponRig Weapons => weapons;
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

            if (Status != BossStatus.Broken)
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

        internal void ToCombo()
        {
            ChangeState(BossStatus.Combo, combo);
        }

        private void ChangeState(BossStatus status, IState state)
        {
            Status = status;
            machine.ChangeState(state);
        }

        void IDamageModifier.Modify(DamageContext damage)
        {
            if (!isActiveAndEnabled || Receiver.Health.IsDead || damage.Damage <= 0f)
            {
                return;
            }

            if (CanGuard)
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

            if (result.FinalDamage > 0f || result.Outcome == HitOutcome.Blocked)
            {
                Guard.DelayRecovery();
            }

            if (Guard.Current <= 0f && (result.Outcome == HitOutcome.Blocked || CanCounterDrainGuard(result.Info)))
            {
                ChangeState(BossStatus.Broken, stagger);
            }
            else if (Guard.IsWeak && result.FinalDamage > 0f
                && result.Info.Impact != AttackImpact.Normal
                && (Status == BossStatus.Approach || (Status == BossStatus.Combo && combo.WindupRemaining > 0f)))
            {
                ChangeState(BossStatus.Staggered, stagger);
            }
        }

        private bool CanCounterDrainGuard(DamageInfo info)
        {
            // 识破的强化反击能在 Boss 出招时打掉格挡条。
            return Status == BossStatus.Combo && !Guard.IsWeak && (info.Impact & AttackImpact.Counter) != 0;
        }
    }
}
