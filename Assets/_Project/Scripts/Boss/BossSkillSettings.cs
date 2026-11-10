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
        Retreat
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

        public BlinkSlashSettings blinkSlash = new BlinkSlashSettings();
        public SwordWaveSettings swordWave = new SwordWaveSettings();
        public RetreatSettings retreat = new RetreatSettings();
        public PhaseSettings phase = new PhaseSettings();
    }
}
