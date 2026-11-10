using System;
using System.Collections;
using BossFight.Boss;
using BossFight.Character;
using BossFight.Combat;
using BossFight.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BossFight.Tests
{
    public sealed class BossSkillTests
    {
        private BossController boss;
        private DamageReceiver player;
        private CharacterMotor playerMotor;

        [UnityTest]
        public IEnumerator SwordWaveUsesDistanceFalloffAndHitsOnce()
        {
            const float distance = 6f;
            yield return LoadArena(distance);
            int hits = 0;
            float damage = 0f;
            player.Damaged += result =>
            {
                hits++;
                damage = result.FinalDamage;
            };

            yield return WaitFor(() => boss.Status == BossStatus.SwordWave, 0.7f);
            yield return WaitFor(() => hits > 0, 1.8f);
            Assert.AreEqual(Mathf.Lerp(28f, 10f, distance / 14f), damage, 0.15f);
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(1, hits);
        }

        [UnityTest]
        public IEnumerator BlinkSlashLocksTheCircle_AndMovingOutAvoidsDamage()
        {
            yield return LoadArena(15f);
            Vector3 locked = player.transform.position;
            yield return WaitFor(() => boss.Status == BossStatus.BlinkSlash && boss.transform.position.y > 3.8f, 2f);
            Assert.AreEqual(locked.z, boss.transform.position.z, 0.05f);
            LineRenderer ring = boss.GetComponentInChildren<LineRenderer>();
            Assert.IsTrue(ring.enabled);
            Vector3 oldRingPoint = ring.GetPosition(0);
            playerMotor.Teleport(locked + Vector3.right * 4f);
            yield return null;
            Assert.AreEqual(oldRingPoint, ring.GetPosition(0), "滞空后落点不能跟随玩家");

            yield return WaitFor(() => boss.transform.position.y < 1f, 0.75f);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(player.Health.Max, player.Health.Current);
            Assert.IsFalse(ring.enabled);
        }

        [UnityTest]
        public IEnumerator StayingInsideBlinkSlashTakesOneHit()
        {
            yield return LoadArena(15f);
            int hits = 0;
            player.Damaged += _ => hits++;
            yield return WaitFor(() => hits > 0, 2f);
            Assert.AreEqual(68f, player.Health.Current);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, hits);
        }

        [UnityTest]
        public IEnumerator RetreatUsesTheHitReaction_AndOnlyEvadesDuringTravel()
        {
            yield return LoadArena(3.7f);
            var settings = boss.Config.Skills.retreat;
            float oldChance = settings.baseChance;
            float oldMax = settings.maxChance;
            settings.baseChance = 1f;
            settings.maxChance = 1f;
            try
            {
                Vector3 start = boss.transform.position;
                Assert.AreEqual(HitOutcome.Blocked, HitBoss(10f, 10f).Outcome);
                Assert.AreEqual(BossStatus.Retreat, boss.Status);
                Assert.AreEqual(HitOutcome.Dodged, HitBoss(10f, 10f).Outcome);
                yield return new WaitForSeconds(0.38f);
                Assert.Greater(boss.transform.position.z - start.z, 4.85f);
                Assert.AreEqual(BossStatus.Retreat, boss.Status);
                Assert.AreEqual(HitOutcome.Hit, HitBoss(10f, 0f).Outcome);
            }
            finally
            {
                settings.baseChance = oldChance;
                settings.maxChance = oldMax;
            }
        }

        [UnityTest]
        public IEnumerator PhaseTwoClosesTheOldStrike_RestoresGuardAndSpeedsUpWindup()
        {
            yield return LoadArena(3.7f);
            var retreat = boss.Config.Skills.retreat;
            BossMoveRule previousRule = retreat.rule;
            retreat.rule.weight = 0f;
            try
            {
                Assert.AreEqual(HitOutcome.Blocked, HitBoss(50f, 60f).Outcome);
                Assert.AreEqual(40f, boss.Guard.Current);
                playerMotor.Teleport(boss.transform.position + Vector3.back * 2f);
                yield return WaitFor(() => boss.IsStriking, 1.3f);
                HitBoss(700f, 0f);

                Assert.AreEqual(BossStatus.PhaseTransition, boss.Status);
                Assert.IsTrue(boss.IsPhaseTwo);
                Assert.IsFalse(boss.GetComponent<BossWeaponRig>().Hitbox.IsActive);
                Assert.AreEqual(boss.Guard.Max, boss.Guard.Current);
                Assert.AreEqual(HitOutcome.Invulnerable, HitBoss(20f, 100f).Outcome);
                Vector3 beforePush = player.transform.position;
                yield return new WaitForSeconds(0.42f);
                Assert.Greater(Vector3.Distance(beforePush, player.transform.position), 1.8f);
                Assert.AreEqual(BossStatus.PhaseTransition, boss.Status);

                yield return WaitFor(() => boss.Status == BossStatus.Approach, 2.2f);
                playerMotor.Teleport(boss.transform.position + Vector3.back * 2f);
                yield return WaitFor(() => boss.Status == BossStatus.Combo && boss.StrikeNumber == 1, 0.9f);
                Assert.That(boss.WindupRemaining, Is.InRange(0.32f, 0.384f));
                HitBoss(20f, 0f);
                Assert.AreEqual(BossStatus.Combo, boss.Status, "转阶段只能触发一次");
            }
            finally
            {
                retreat.rule = previousRule;
            }
        }

        private IEnumerator LoadArena(float distance)
        {
            void PositionPlayer(Scene scene, LoadSceneMode mode)
            {
                boss = Object.FindAnyObjectByType<BossController>();
                var controller = Object.FindAnyObjectByType<PlayerController>();
                player = controller.GetComponent<DamageReceiver>();
                playerMotor = controller.GetComponent<CharacterMotor>();
                playerMotor.Teleport(new Vector3(0f, 0f, -distance));
            }

            SceneManager.sceneLoaded += PositionPlayer;
            try
            {
                yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            }
            finally
            {
                SceneManager.sceneLoaded -= PositionPlayer;
            }
            yield return null;
        }

        private DamageResult HitBoss(float amount, float guard)
        {
            return boss.Receiver.TakeDamage(new DamageInfo { Amount = amount, GuardDamage = guard, Attacker = player.gameObject });
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            double deadline = Time.timeAsDouble + timeout;
            yield return new WaitUntil(() => condition() || Time.timeAsDouble >= deadline);
            Assert.IsTrue(condition(), $"预期状态未在 {timeout} 秒内出现");
        }
    }
}
