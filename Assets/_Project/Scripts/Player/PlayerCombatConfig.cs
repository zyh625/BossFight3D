using BossFight.Combat;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>玩家战斗相关的调参数据（剑形态）。</summary>
    [CreateAssetMenu(menuName = "BossFight/Player Combat Config", fileName = "PlayerCombatConfig")]
    public sealed class PlayerCombatConfig : ScriptableObject
    {
        [Header("轻攻击连段（按顺序）")]
        [SerializeField] private AttackData[] lightCombo;

        [Header("蓄力重击（松开右键或体力耗尽时打出）")]
        [Tooltip("伤害为零蓄力时的基础值。体力在蓄力阶段持续扣除")]
        [SerializeField] private AttackData heavyAttack;
        [Min(0.01f)]
        [SerializeField] private float chargeStaminaPerSecond = 35f;
        [Tooltip("按住右键这么久达到满蓄力（秒）")]
        [Min(0.01f)]
        [SerializeField] private float fullChargeTime = 1f;
        [Tooltip("满蓄力时的伤害倍率（45 × 1.55 ≈ 70）")]
        [SerializeField] private float fullChargeDamageMultiplier = 1.55f;
        [Tooltip("蓄力时的移动速度倍率")]
        [Range(0f, 1f)]
        [SerializeField] private float chargeMoveMultiplier = 0.35f;
        [SerializeField] private string chargeAnimationState = "HeavyCharge";

        [Header("切手技（按住右键时按下左键）")]
        [SerializeField] private AttackData variedAttack;

        [Header("识破")]
        [Min(0f)]
        [SerializeField] private float seeThroughWindow = 0.2f;
        [Min(1f)]
        [SerializeField] private float counterDamageMultiplier = 1.3f;
        [Min(0f)]
        [SerializeField] private float counterBonusGuardDamage = 25f;
        [Min(0f)]
        [SerializeField] private float freeStaminaDuration = 3f;

        [Header("翻滚")]
        [Min(0f)]
        [SerializeField] private float rollStaminaCost = 20f;
        [Min(0.01f)]
        [SerializeField] private float rollDuration = 0.6f;
        [Min(0f)]
        [SerializeField] private float rollDistance = 4.5f;
        [Min(0f)]
        [SerializeField] private float rollInvulnerabilityStart = 0.05f;
        [Min(0f)]
        [SerializeField] private float rollInvulnerabilityEnd = 0.35f;

        [Header("受击")]
        [SerializeField, Min(0f)] private float hitStunDuration = 0.4f;
        [SerializeField, Min(0f)] private float hitStunRollCancelTime = 0.25f;

        [Header("通用")]
        [Tooltip("输入缓冲时长（秒）")]
        [SerializeField] private float inputBufferTime = 0.2f;
        [Tooltip("攻击前摇期间，角色转向输入方向的角速度（度/秒）")]
        [SerializeField] private float attackTurnSpeed = 720f;
        [Tooltip("奔跑每秒消耗的体力")]
        [SerializeField] private float sprintStaminaPerSecond = 12f;

        public int LightComboLength => lightCombo != null ? lightCombo.Length : 0;
        public AttackData GetLight(int index) => lightCombo[index];
        public AttackData HeavyAttack => heavyAttack;
        public float ChargeStaminaPerSecond => chargeStaminaPerSecond;
        public float FullChargeTime => fullChargeTime;
        public float FullChargeDamageMultiplier => fullChargeDamageMultiplier;
        public float ChargeMoveMultiplier => chargeMoveMultiplier;
        public string ChargeAnimationState => chargeAnimationState;
        public AttackData VariedAttack => variedAttack;
        public float SeeThroughWindow => seeThroughWindow;
        public float CounterDamageMultiplier => counterDamageMultiplier;
        public float CounterBonusGuardDamage => counterBonusGuardDamage;
        public float FreeStaminaDuration => freeStaminaDuration;
        public float RollStaminaCost => rollStaminaCost;
        public float RollDuration => rollDuration;
        public float RollDistance => rollDistance;
        public float RollInvulnerabilityStart => rollInvulnerabilityStart;
        public float RollInvulnerabilityEnd => rollInvulnerabilityEnd;
        public float HitStunDuration => hitStunDuration;
        public float HitStunRollCancelTime => hitStunRollCancelTime;
        public float InputBufferTime => inputBufferTime;
        public float AttackTurnSpeed => attackTurnSpeed;
        public float SprintStaminaPerSecond => sprintStaminaPerSecond;
    }
}
