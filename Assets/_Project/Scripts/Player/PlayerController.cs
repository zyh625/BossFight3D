using BossFight.Character;
using BossFight.Combat;
using BossFight.Core;
using BossFight.Player.States;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 玩家的总入口：组装状态机和各状态共享的上下文，然后每帧按固定顺序驱动
    /// 输入缓冲 → 读取攻击按键 → 当前状态。它自己不包含任何玩法规则，规则都在状态里。
    /// </summary>
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class PlayerController : MonoBehaviour, IDamageModifier
    {
        [Header("组件")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private CharacterVisual visual;
        [SerializeField] private Stamina stamina;
        [SerializeField] private Hitbox weaponHitbox;

        [Header("数据")]
        [SerializeField] private PlayerCombatConfig combatConfig;

        [Tooltip("相机跟随/注视的点（胸口高度的空物体）")]
        [SerializeField] private Transform cameraTarget;

        private readonly StateMachine machine = new StateMachine();
        private PlayerContext context;
        private Health health;
        private DamageReceiver damageReceiver;
        private PlayerHitState hitState;

        int IDamageModifier.Order => 100;

        public Transform CameraTarget => cameraTarget;

        /// <summary>当前状态的名字，调试显示用。</summary>
        public string CurrentStateName => machine.Current != null ? machine.Current.GetType().Name : "-";

        /// <summary>蓄力进度 0~1，不在蓄力时为 0。</summary>
        public float ChargeRatio => machine.Current == context.Charge ? context.Charge.ChargeRatio : 0f;

        private void Awake()
        {
            health = GetComponent<Health>();
            damageReceiver = GetComponent<DamageReceiver>();
            var buffer = new InputBuffer(combatConfig.InputBufferTime);

            context = new PlayerContext
            {
                Transform = transform,
                GameObject = gameObject,
                Input = input,
                Movement = movement,
                Motor = motor,
                Visual = visual,
                Stamina = stamina,
                WeaponHitbox = weaponHitbox,
                Combat = combatConfig,
                HitStop = GetComponent<LocalHitStop>(),
                Buffer = buffer,
                Machine = machine
            };

            context.Locomotion = new PlayerLocomotionState(context);
            context.Attack = new PlayerAttackState(context);
            context.Charge = new PlayerChargeState(context);
            context.Roll = new PlayerRollState(context);
            hitState = new PlayerHitState(context);
        }

        private void Start()
        {
            context.ToLocomotion();
        }

        /// <summary>
        /// 左键按下的那一帧就产生指令（不等松开），所以轻攻击没有输入延迟。
        /// 同一个左键，按住右键时是切手技，否则是轻攻击。
        /// </summary>
        private void ReadAttackInput()
        {
            if (input.RollPressedThisFrame)
            {
                context.Buffer.Push(PlayerCommand.Roll);
            }
            else if (input.AttackPressedThisFrame)
            {
                context.Buffer.Push(input.ChargeHeld ? PlayerCommand.VariedAttack : PlayerCommand.LightAttack);
            }
        }

        private void Update()
        {
            if (health.IsDead)
            {
                return;
            }

            float dt = context.HitStop != null ? context.HitStop.DeltaTime : Time.deltaTime;

            context.Buffer.Tick(dt);
            if (!input.ChargeHeld)
            {
                context.ChargeHoldConsumed = false;
            }

            ReadAttackInput();
            if (dt > 0f)
            {
                machine.Tick(dt);
            }
        }

        void IDamageModifier.Modify(DamageContext damage)
        {
            if (!isActiveAndEnabled || health.IsDead || damage.Damage <= 0f)
            {
                return;
            }

            if (machine.Current == context.Roll && context.Roll.TryDodge())
            {
                damage.Outcome = HitOutcome.Dodged;
                damage.Cancelled = true;
            }
            else if (machine.Current == context.Attack && context.Attack.TrySeeThrough())
            {
                damage.Outcome = HitOutcome.Parried;
                damage.Cancelled = true;
            }
        }

        private void OnEnable()
        {
            health.Died += OnDied;
            damageReceiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
            damageReceiver.Damaged -= OnDamaged;
            weaponHitbox.End();
        }

        private void OnDied()
        {
            context.ToLocomotion();
        }

        private void OnDamaged(DamageResult result)
        {
            if (result.FinalDamage > 0f && !result.Killed)
            {
                context.Buffer.Clear();
                context.ChargeHoldConsumed = input.ChargeHeld;
                machine.ChangeState(hitState);
            }
        }
    }
}
