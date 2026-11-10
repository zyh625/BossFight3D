using System;
using UnityEngine;

namespace BossFight.Combat
{
    [Flags]
    public enum AttackImpact
    {
        Normal = 0,
        Counter = 1 << 0,
        ChargedHeavy = 1 << 1,
        GuardBreakFinisher = 1 << 2
    }

    /// <summary>一次命中的"输入"：攻击方想造成什么。由 Hitbox 生成。</summary>
    public struct DamageInfo
    {
        /// <summary>生命伤害。</summary>
        public float Amount;

        /// <summary>对格挡条（韧性）的伤害。M4 的 Boss 格挡条使用。</summary>
        public float GuardDamage;
        public AttackImpact Impact;

        public GameObject Attacker;

        /// <summary>命中点（世界坐标），用于生成火花、伤害数字。</summary>
        public Vector3 HitPoint;

        /// <summary>攻击方指向受击方的水平方向，用于受击朝向和击退。</summary>
        public Vector3 Direction;
    }

    /// <summary>一次命中的结算类型。</summary>
    public enum HitOutcome
    {
        /// <summary>正常命中。</summary>
        Hit,
        /// <summary>被格挡：伤害被削减。</summary>
        Blocked,
        /// <summary>攻击被识破：无伤害。</summary>
        Parried,
        /// <summary>被无敌帧躲过：无伤害。</summary>
        Dodged,
        /// <summary>目标处于转阶段保护，命中不会扣血。</summary>
        Invulnerable
    }

    /// <summary>一次命中的"输出"：经过结算管线之后实际发生了什么。特效、音效、UI 都读它。</summary>
    public struct DamageResult
    {
        public DamageInfo Info;
        public HitOutcome Outcome;

        /// <summary>实际扣掉的生命值。</summary>
        public float FinalDamage;

        public DamageReceiver Target;
        public bool Killed;

        public bool IsGuardBreakFinisher => FinalDamage > 0f && (Info.Impact & AttackImpact.GuardBreakFinisher) != 0;
    }

    /// <summary>
    /// 结算过程中在各个修饰器之间传递的可变数据。
    /// 修饰器可以改伤害数值、改结算类型，或者把 Cancelled 设为 true 来终止后续结算。
    /// </summary>
    public sealed class DamageContext
    {
        public DamageInfo Info;
        public float Damage;
        public HitOutcome Outcome;
        public bool Cancelled;
    }
}
