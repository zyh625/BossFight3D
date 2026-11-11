using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BossFight.Boss
{
    /// <summary>
    /// Boss 的"选招"逻辑：只在回到追击状态、需要决定下一招时被询问一次。
    /// 每个招式有一条规则（距离范围、冷却、权重），符合条件的招式按权重随机；刚用过的招式权重减半，避免连续重复。
    /// 它不执行招式，也不知道招式怎么打——新增一个可主动选择的招式，只需要加进 Attacks 列表并配置规则。
    /// </summary>
    internal sealed class BossBrain
    {
        private static readonly BossMove[] Attacks =
        {
            BossMove.Combo, BossMove.BlinkSlash, BossMove.SwordWave, BossMove.Whirlwind
        };

        private readonly BossCombatConfig config;
        private readonly double[] readyAt = new double[Enum.GetValues(typeof(BossMove)).Length];
        private readonly float[] weights = new float[Attacks.Length];
        private BossMove lastAttack;
        private double lastHitAt = double.NegativeInfinity;
        private int consecutiveHits;
        private float cooldownScale = 1f;

        public BossBrain(BossCombatConfig config)
        {
            this.config = config;
        }

        /// <param name="attackDistance">当前形态的近战距离（长剑形态更远），决定连斩什么时候可以出手。</param>
        public BossMove ChooseAttack(float distance, double actionTime, bool canUseSkills, float attackDistance, int comboLength)
        {
            float total = 0f;
            for (int i = 0; i < Attacks.Length; i++)
            {
                BossMove move = Attacks[i];
                float weight;
                if (move == BossMove.Combo)
                {
                    BossMoveRule rule = config.GetRule(move);
                    rule.maxDistance = attackDistance;
                    weight = comboLength > 0 ? Weight(move, rule, distance, actionTime) : 0f;
                }
                else
                {
                    weight = canUseSkills ? Weight(move, config.GetRule(move), distance, actionTime) : 0f;
                }

                weights[i] = weight;
                total += weight;
            }

            if (total <= 0f)
            {
                return BossMove.None;
            }

            // 轮盘赌：在 [0, total) 里取一个数，落在哪个招式的权重区间就选哪个
            float roll = Random.value * total;
            int chosen = -1;
            for (int i = 0; i < Attacks.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                chosen = i;
                if (roll < weights[i])
                {
                    break;
                }
                roll -= weights[i];
            }

            lastAttack = Attacks[chosen];
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

        /// <summary>后撤收招时调用：是否接一记突进。二阶段概率更高。</summary>
        public bool ShouldLungeAfterRetreat(bool phaseTwo)
        {
            var settings = config.Skills.lunge;
            float chance = phaseTwo ? settings.phaseTwoChance : settings.chance;
            return chance > 0f && (chance >= 1f || Random.value < chance);
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

        private float Weight(BossMove move, BossMoveRule rule, float distance, double actionTime)
        {
            return actionTime >= readyAt[(int)move] && rule.InRange(distance)
                ? rule.weight * (lastAttack == move ? 0.5f : 1f)
                : 0f;
        }
    }
}
