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

        // 切手技或耗尽体力的重击消耗掉当前这次右键长按。
        public bool ChargeHoldConsumed { get; set; }

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
            if (comboIndex >= Combat.LightComboLength || !Buffer.TryConsume(PlayerCommand.LightAttack))
            {
                return false;
            }

            AttackData data = Combat.GetLight(comboIndex);
            if (!Stamina.TrySpend(data.StaminaCost, out float spent))
            {
                return false;
            }

            Attack.Setup(data, comboIndex, 1f, spent);
            Machine.ChangeState(Attack);
            return true;
        }

        public bool TryStartVariedAttack(float staminaCredit = 0f)
        {
            if (!Buffer.TryConsume(PlayerCommand.VariedAttack) || Combat.VariedAttack == null)
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
