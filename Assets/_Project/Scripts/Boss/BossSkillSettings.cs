using System;
using UnityEngine;

namespace BossFight.Boss
{
    public enum BossMove
    {
        None,
        Combo,
        BlinkSlash,
        SwordWave,
        Retreat,
        /// <summary>旋风斩：旋转着追向玩家，反弹飞镖。</summary>
        Whirlwind,
        /// <summary>后撤之后接的突进斩。不会被主动选中，只由后撤派生。</summary>
        Lunge
    }

    [Serializable]
    public struct BossMoveRule
    {
        [Min(0f)] public float minDistance;
        [Min(0f), Tooltip("0 表示不限最远距离")] public float maxDistance;
        [Min(0f)] public float cooldown;
        [Min(0f)] public float weight;

        public BossMoveRule(float minDistance, float maxDistance, float cooldown, float weight = 1f)
        {
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.cooldown = cooldown;
            this.weight = weight;
        }

        public bool InRange(float distance) => distance >= minDistance && (maxDistance == 0f || distance <= maxDistance);
    }

    [Serializable]
    public sealed class BossSkillSettings
    {
        [Serializable]
        public sealed class BlinkSlashSettings
        {
            public BossMoveRule rule = new BossMoveRule(7f, 0f, 8f);
            [Min(0f)] public float windup = 0.8f;
            [Min(0f)] public float hover = 0.4f;
            [Min(0f)] public float height = 4f;
            [Min(0f)] public float active = 0.2f;
            [Min(0f)] public float recovery = 1.2f;
            [Min(0f)] public float radius = 2.5f;
            [Min(0f)] public float damage = 32f;
        }

        [Serializable]
        public sealed class SwordWaveSettings
        {
            public BossMoveRule rule = new BossMoveRule(4f, 14f, 6f);
            [Min(0f)] public float windup = 0.7f;
            [Min(0f)] public float recovery = 0.8f;
            [Min(0.01f)] public float speed = 18f;
            [Min(0.01f)] public float width = 1.6f;
            [Min(0.01f)] public float range = 14f;
            [Min(0f)] public float nearDamage = 28f;
            [Min(0f)] public float farDamage = 10f;
        }

        [Serializable]
        public sealed class RetreatSettings
        {
            public BossMoveRule rule = new BossMoveRule(0f, 0f, 4f);
            [Min(0.01f)] public float duration = 0.25f;
            [Min(0f)] public float distance = 5f;
            [Min(0f)] public float recovery = 0.3f;
            [Min(0f)] public float chainWindow = 2f;
            [Range(0f, 1f)] public float baseChance = 0.10f;
            [Range(0f, 1f)] public float chancePerHit = 0.15f;
            [Range(0f, 1f)] public float maxChance = 0.60f;
        }

        [Serializable]
        public sealed class PhaseSettings
        {
            [Range(0f, 1f)] public float healthThreshold = 0.5f;
            [Min(0.01f)] public float duration = 2f;
            [Min(0f)] public float burstRadius = 4f;
            [Min(0f)] public float knockbackDistance = 2.5f;
            [Min(0.01f)] public float knockbackDuration = 0.22f;
            [Range(0.01f, 1f)] public float cooldownScale = 0.65f;
            [Range(0.01f, 1f)] public float quickStrikeWindupScale = 0.85f;
        }

        [Serializable]
        public sealed class WhirlwindSettings
        {
            public BossMoveRule rule = new BossMoveRule(4.2f, 5.8f, 9f);
            [Min(0f)] public float windup = 0.6f;
            [Min(0.01f), Tooltip("旋转持续多久")] public float duration = 2.2f;
            [Min(0f), Tooltip("旋转时追向玩家的速度")] public float moveSpeed = 4.6f;
            [Min(0f), Tooltip("旋转角速度（度/秒）")] public float spinSpeed = 900f;
            [Min(0.05f), Tooltip("每隔多久结算一次伤害")] public float hitInterval = 0.4f;
            [Min(0f)] public float damage = 10f;
            [Min(0f), Tooltip("转完之后的收招，是玩家的反击窗口")] public float recovery = 1f;
        }

        [Serializable]
        public sealed class LungeSettings
        {
            [Range(0f, 1f), Tooltip("后撤之后接突进的概率")] public float chance = 0.45f;
            [Range(0f, 1f)] public float phaseTwoChance = 0.7f;
            [Min(0f)] public float windup = 0.32f;
            [Min(0.01f)] public float speed = 24f;
            [Min(0f)] public float maxDistance = 9f;
            [Min(0f), Tooltip("冲到离玩家多远时停下")] public float stopDistance = 2.2f;
            [Min(0f)] public float damage = 20f;
            [Min(0f)] public float recovery = 0.7f;
        }

        [Serializable]
        public sealed class LongswordSettings
        {
            [Tooltip("设为 false 可以完全关闭长剑形态")] public bool enabled = true;
            [Min(0f), Tooltip("玩家至少离这么远，Boss 才会合剑")] public float minDistance = 6f;
            [Min(0.01f)] public float transformTime = 0.9f;
            [Min(0.01f), Tooltip("长剑形态持续多久")] public float duration = 16f;
            [Min(0f), Tooltip("形态结束后多久才能再次合剑")] public float cooldown = 10f;
            [Min(0f), Tooltip("长剑形态的近战距离")] public float attackDistance = 4.6f;
            [Min(1f), Tooltip("剑身和判定加长的倍率")] public float reachScale = 1.45f;
            [Min(1f), Tooltip("技能范围放大的倍率（瞬身斩半径、剑气宽度）")] public float skillScale = 1.35f;
        }

        public BlinkSlashSettings blinkSlash = new BlinkSlashSettings();
        public SwordWaveSettings swordWave = new SwordWaveSettings();
        public RetreatSettings retreat = new RetreatSettings();
        public PhaseSettings phase = new PhaseSettings();
        public WhirlwindSettings whirlwind = new WhirlwindSettings();
        public LungeSettings lunge = new LungeSettings();
        public LongswordSettings longsword = new LongswordSettings();
    }
}
