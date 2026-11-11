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
        [Tooltip("这段时间内优先识别快速双键，暂不展示蓄力动作和进度")]
        [SerializeField, Min(0f)] private float chargePresentationDelay = 0.12f;
        [SerializeField, Min(0.01f)] private float heavyLeapHeight = 0.85f;

        [Header("切手技（按住右键时按下左键）")]
        [SerializeField] private AttackData variedAttack;

        [Header("拳套形态")]
        [Tooltip("左右拳交替的连打，打完最后一段从头循环")]
        [SerializeField] private AttackData[] fistCombo;
        [Tooltip("蓄力冲拳：向前瞬移一段距离后出拳")]
        [SerializeField] private AttackData fistHeavy;
        [Tooltip("拳套形态的识破窗口是剑的多少倍")]
        [SerializeField, Min(1f)] private float fistSeeThroughWindowScale = 1.5f;
        [Tooltip("拳套识破成功时仍然要承受这个比例的伤害（赤手去接，总要付出点代价）")]
        [SerializeField, Range(0f, 1f)] private float fistSeeThroughChip = 0.15f;

        [Header("飞镖形态")]
        [SerializeField, Min(0f)] private float dartDamage = 12f;
        [SerializeField, Min(0f)] private float dartGuardDamage = 6f;
        [SerializeField, Min(0.1f)] private float dartSpeed = 32f;
        [SerializeField, Min(1f)] private float dartRange = 30f;
        [SerializeField, Min(0f)] private float dartStaminaCost = 6f;
        [Tooltip("蓄力飞镖：刚开始蓄力就松开时的伤害")]
        [SerializeField, Min(0f)] private float dartChargedMinDamage = 18f;
        [Tooltip("蓄力飞镖：满蓄力的伤害")]
        [SerializeField, Min(0f)] private float dartChargedMaxDamage = 34f;
        [SerializeField, Min(0f)] private float dartChargedGuardDamage = 22f;
        [SerializeField, Min(0.1f)] private float dartChargedSpeed = 52f;
        [Tooltip("投掷的前摇（秒）")]
        [SerializeField, Min(0f)] private float throwWindup = 0.08f;
        [Tooltip("投掷的后摇（秒），过半后可以接下一次投掷")]
        [SerializeField, Min(0f)] private float throwRecovery = 0.24f;

        [Header("闪避反击（完美闪避后短时间内按左键）")]
        [Tooltip("一记快速的突进斩。不消耗体力：它是完美闪避的奖励")]
        [SerializeField] private AttackData dodgeCounter;
        [Tooltip("完美闪避之后，这么长时间内按左键可以打出闪避反击（秒）")]
        [SerializeField, Min(0f)] private float dodgeCounterWindow = 0.6f;

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
        [Tooltip("翻滚进行到这个时间之后，可以直接接攻击或再次翻滚（秒）。必须晚于无敌帧结束")]
        [SerializeField, Min(0f)] private float rollCancelTime = 0.42f;

        [Header("跳跃")]
        [SerializeField, Min(0.01f)] private float jumpHeight = 1.35f;
        [SerializeField, Range(0f, 1f)] private float airMoveMultiplier = 0.85f;

        [Header("受击")]
        [SerializeField, Min(0f)] private float hitStunDuration = 0.4f;
        [SerializeField, Min(0f)] private float hitStunRollCancelTime = 0.25f;

        [Header("通用")]
        [Tooltip("输入缓冲时长（秒）")]
        [SerializeField] private float inputBufferTime = 0.35f;
        [Tooltip("攻击前摇期间，角色转向输入方向的角速度（度/秒）")]
        [SerializeField] private float attackTurnSpeed = 1440f;
        [Tooltip("攻击后摇过了衔接点之后，移动输入超过这个幅度（0~1）就提前结束后摇")]
        [SerializeField, Range(0.1f, 1f)] private float moveCancelThreshold = 0.5f;
        [Tooltip("奔跑每秒消耗的体力")]
        [SerializeField] private float sprintStaminaPerSecond = 12f;
        [Tooltip("调试用：玩家所有体力消耗乘以这个倍率。正式数值是 1，调小便于反复测试")]
        [SerializeField, Range(0f, 1f)] private float staminaCostMultiplier = 1f;

        public int LightComboLength => lightCombo != null ? lightCombo.Length : 0;
        public AttackData GetLight(int index) => lightCombo[index];
        public AttackData HeavyAttack => heavyAttack;
        public float ChargeStaminaPerSecond => chargeStaminaPerSecond;
        public float FullChargeTime => fullChargeTime;
        public float FullChargeDamageMultiplier => fullChargeDamageMultiplier;
        public float ChargeMoveMultiplier => chargeMoveMultiplier;
        public string ChargeAnimationState => chargeAnimationState;
        public float ChargePresentationDelay => chargePresentationDelay;
        public float HeavyLeapHeight => heavyLeapHeight;
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
        public float RollCancelTime => rollCancelTime;
        public float MoveCancelThreshold => moveCancelThreshold;
        public float JumpHeight => jumpHeight;
        public float AirMoveMultiplier => airMoveMultiplier;
        public float HitStunDuration => hitStunDuration;
        public float HitStunRollCancelTime => hitStunRollCancelTime;
        public float InputBufferTime => inputBufferTime;
        public float AttackTurnSpeed => attackTurnSpeed;
        public float SprintStaminaPerSecond => sprintStaminaPerSecond;
        public float StaminaCostMultiplier => staminaCostMultiplier;
        public int FistComboLength => fistCombo != null ? fistCombo.Length : 0;
        public AttackData GetFist(int index) => fistCombo[index];
        public AttackData FistHeavy => fistHeavy;
        public float FistSeeThroughWindowScale => fistSeeThroughWindowScale;
        public float FistSeeThroughChip => fistSeeThroughChip;
        public float DartDamage => dartDamage;
        public float DartGuardDamage => dartGuardDamage;
        public float DartSpeed => dartSpeed;
        public float DartRange => dartRange;
        public float DartStaminaCost => dartStaminaCost;
        public float DartChargedMinDamage => dartChargedMinDamage;
        public float DartChargedMaxDamage => dartChargedMaxDamage;
        public float DartChargedGuardDamage => dartChargedGuardDamage;
        public float DartChargedSpeed => dartChargedSpeed;
        public float ThrowWindup => throwWindup;
        public float ThrowRecovery => throwRecovery;
        public AttackData DodgeCounter => dodgeCounter;
        public float DodgeCounterWindow => dodgeCounterWindow;
    }
}
