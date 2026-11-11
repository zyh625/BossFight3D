using BossFight.Character;
using BossFight.Combat;
using BossFight.Core;
using BossFight.Player.States;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 玩家各个状态共享的"工具箱"：组件引用、配置、输入意图，以及状态之间互相切换的入口。
    /// 状态只依赖它，不直接依赖 PlayerController，也不互相 new。
    /// </summary>
    public sealed class PlayerContext
    {
        public Transform Transform;
        public GameObject GameObject;
        public PlayerInputReader Input;
        public PlayerMovement Movement;
        public CharacterMotor Motor;
        public CharacterVisual Visual;
        public Stamina Stamina;
        public Hitbox WeaponHitbox;
        public Hitbox FistHitbox;
        public DartLauncher Darts;
        public PlayerCombatConfig Combat;
        public LocalHitStop HitStop;
        public double ActionTime => HitStop != null ? HitStop.ActionTime : Time.timeAsDouble;

        public InputBuffer Buffer;
        public StateMachine Machine;

        public PlayerLocomotionState Locomotion;
        public PlayerAttackState Attack;
        public PlayerChargeState Charge;
        public PlayerRollState Roll;
        public PlayerJumpState Jump;
        public PlayerThrowState Throw;

        /// <summary>当前的武器形态。</summary>
        public WeaponForm Form { get; private set; }

        public event System.Action<WeaponForm> FormChanged;

        /// <summary>当前形态用来打人的判定体。飞镖形态没有近战判定，返回剑的（不会被开启）。</summary>
        public Hitbox ActiveHitbox => Form == WeaponForm.Fist && FistHitbox != null ? FistHitbox : WeaponHitbox;

        /// <summary>当前形态的蓄力招式数据（飞镖形态的蓄力是投掷，不走攻击数据）。</summary>
        public AttackData ChargedAttack => Form == WeaponForm.Fist && Combat.FistHeavy != null ? Combat.FistHeavy : Combat.HeavyAttack;

        /// <summary>识破窗口：拳套更宽。</summary>
        public float SeeThroughWindow =>
            Combat.SeeThroughWindow * (Form == WeaponForm.Fist ? Combat.FistSeeThroughWindowScale : 1f);

        /// <summary>按 F：切换到下一种形态。只在自由移动时允许，所以不会在出招中途换武器。</summary>
        public void CycleForm()
        {
            Form = (WeaponForm)(((int)Form + 1) % 3);
            // 没有配置对应内容的形态直接跳过
            if ((Form == WeaponForm.Dart && Darts == null)
                || (Form == WeaponForm.Fist && (FistHitbox == null || Combat.FistComboLength == 0)))
            {
                CycleForm();
                return;
            }

            ChargeHoldConsumed = Input.ChargeHeld;
            FormChanged?.Invoke(Form);
        }

        // 切手技或耗尽体力的重击消耗掉当前这次右键长按。
        public bool ChargeHoldConsumed { get; set; }

        private double dodgeCounterUntil;
        private Transform dodgeCounterTarget;

        public void ToLocomotion()
        {
            Machine.ChangeState(Locomotion);
        }

        /// <summary>
        /// 如果缓冲里有轻攻击指令，就尝试打出连段的第 comboIndex 段。
        /// 指令无论成功与否都会被消耗：体力不够时这次按键作废，而不是等体力恢复后突然出招。
        /// </summary>
        public bool TryStartLightAttack(int comboIndex)
        {
            if (Form == WeaponForm.Dart)
            {
                return TryStartThrow();
            }

            bool fist = Form == WeaponForm.Fist;
            int length = fist ? Combat.FistComboLength : Combat.LightComboLength;
            if (fist && length > 0)
            {
                // 拳套的连打没有终结技：左、右、左、右……一直循环
                comboIndex %= length;
            }

            if (comboIndex >= length || !Buffer.TryConsume(PlayerCommand.LightAttack))
            {
                return false;
            }

            AttackData data = fist ? Combat.GetFist(comboIndex) : Combat.GetLight(comboIndex);
            if (!Stamina.TrySpend(data.StaminaCost, out float spent))
            {
                return false;
            }

            Attack.Setup(data, comboIndex, 1f, spent);
            Machine.ChangeState(Attack);
            return true;
        }

        /// <summary>飞镖形态的左键：投出一支普通飞镖。</summary>
        private bool TryStartThrow()
        {
            if (!Buffer.TryConsume(PlayerCommand.LightAttack) || !Stamina.TrySpend(Combat.DartStaminaCost))
            {
                return false;
            }

            Throw.Setup(0f);
            Machine.ChangeState(Throw);
            return true;
        }

        public bool TryStartVariedAttack(float staminaCredit = 0f)
        {
            // 切手技是近战招式，飞镖形态没有；按键照样被消耗，避免它留在缓冲里
            if (!Buffer.TryConsume(PlayerCommand.VariedAttack) || Combat.VariedAttack == null || Form == WeaponForm.Dart)
            {
                return false;
            }

            float remainingCost = Combat.VariedAttack.StaminaCost - staminaCredit;
            if (!Stamina.TrySpend(Mathf.Max(0f, remainingCost), out float spent))
            {
                return false;
            }
            float refund = Mathf.Max(0f, -remainingCost);
            Stamina.Refund(refund);
            ChargeHoldConsumed = Input.ChargeHeld;
            Attack.Setup(Combat.VariedAttack, -1, 1f, staminaCredit + spent - refund);
            Machine.ChangeState(Attack);
            return true;
        }

        /// <summary>按住右键且还能支付蓄力消耗时，进入蓄力。</summary>
        public bool TryStartCharge()
        {
            if (ChargeHoldConsumed || (!Input.ChargeHeld && !Input.ChargePressedThisFrame)
                || (Stamina.Current <= 0f && !Stamina.HasFreeSpending))
            {
                return false;
            }

            Machine.ChangeState(Charge);
            return true;
        }

        /// <summary>完美闪避后开启一小段"可以反击"的时间，并记住被躲开的是谁。</summary>
        public void OpenDodgeCounter(Transform attacker)
        {
            dodgeCounterUntil = ActionTime + Combat.DodgeCounterWindow;
            dodgeCounterTarget = attacker;
        }

        /// <summary>
        /// 反击窗口内缓冲里有左键指令，就打出闪避反击。翻滚中途也可以直接接，不用等翻滚结束。
        /// </summary>
        public bool TryStartDodgeCounter()
        {
            if (Combat.DodgeCounter == null || Form == WeaponForm.Dart || ActionTime > dodgeCounterUntil
                || !Buffer.TryConsume(PlayerCommand.LightAttack))
            {
                return false;
            }

            dodgeCounterUntil = 0d;
            Attack.SetupDodgeCounter(Combat.DodgeCounter, dodgeCounterTarget);
            Machine.ChangeState(Attack);
            return true;
        }

        public bool TryStartRoll()
        {
            if (!Buffer.TryConsume(PlayerCommand.Roll) || !Stamina.TrySpend(Combat.RollStaminaCost, out float spent))
            {
                return false;
            }

            ChargeHoldConsumed = Input.ChargeHeld;
            Roll.Setup(spent);
            Machine.ChangeState(Roll);
            return true;
        }

        public bool TryStartJump()
        {
            if (!Motor.IsGrounded || !Buffer.TryConsume(PlayerCommand.Jump) || !Motor.TryJump(Combat.JumpHeight))
            {
                return false;
            }

            ChargeHoldConsumed = Input.ChargeHeld;
            Machine.ChangeState(Jump);
            return true;
        }
    }
}
