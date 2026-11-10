using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BossFight.Boss
{
    /// <summary>主动招式按距离和冷却筛选，再加权抽取。后撤独立响应连续受击。</summary>
    internal sealed class BossBrain
    {
        private readonly BossCombatConfig config;
        private readonly double[] readyAt = new double[(int)BossMove.Retreat + 1];
        private BossMove lastAttack;
        private double lastHitAt = double.NegativeInfinity;
        private int consecutiveHits;
        private float cooldownScale = 1f;

        public BossBrain(BossCombatConfig config)
        {
            this.config = config;
        }

        public BossMove ChooseAttack(float distance, double actionTime, bool canUseSkills)
        {
            float combo = config.ComboLength > 0 && distance <= config.AttackDistance
                ? Weight(BossMove.Combo, distance, actionTime) : 0f;
            float blink = canUseSkills ? Weight(BossMove.BlinkSlash, distance, actionTime) : 0f;
            float wave = canUseSkills ? Weight(BossMove.SwordWave, distance, actionTime) : 0f;
            float total = combo + blink + wave;
            if (total <= 0f)
            {
                return BossMove.None;
            }

            float roll = Random.value * total;
            if (combo > 0f && (roll < combo || blink + wave <= 0f))
            {
                lastAttack = BossMove.Combo;
            }
            else
            {
                lastAttack = blink > 0f && (roll < combo + blink || wave <= 0f)
                    ? BossMove.BlinkSlash : BossMove.SwordWave;
            }
            return lastAttack;
        }

        public bool RecordHitAndTryRetreat(bool canRetreat, double actionTime)
        {
            var settings = config.Skills.retreat;
            consecutiveHits = actionTime - lastHitAt <= settings.chainWindow ? consecutiveHits + 1 : 1;
            lastHitAt = actionTime;
            if (!canRetreat || settings.rule.weight <= 0f || actionTime < readyAt[(int)BossMove.Retreat])
            {
                return false;
            }

            float chance = Mathf.Min(settings.maxChance, settings.baseChance + settings.chancePerHit * consecutiveHits);
            if (chance <= 0f || (chance < 1f && Random.value >= chance))
            {
                return false;
            }
            consecutiveHits = 0;
            return true;
        }

        public void FinishMove(BossMove move, double actionTime)
        {
            if (move != BossMove.None)
            {
                readyAt[(int)move] = actionTime + config.GetRule(move).cooldown * cooldownScale;
            }
        }

        public void EnterPhaseTwo(double actionTime)
        {
            cooldownScale = config.Skills.phase.cooldownScale;
            for (int i = 1; i < readyAt.Length; i++)
            {
                readyAt[i] = actionTime + Math.Max(0d, readyAt[i] - actionTime) * cooldownScale;
            }
        }

        private float Weight(BossMove move, float distance, double actionTime)
        {
            BossMoveRule rule = config.GetRule(move);
            return actionTime >= readyAt[(int)move] && rule.InRange(distance)
                ? rule.weight * (lastAttack == move ? 0.5f : 1f)
                : 0f;
        }
    }
}
