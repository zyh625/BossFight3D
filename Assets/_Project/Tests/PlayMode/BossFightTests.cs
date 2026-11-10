using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class BossFightTests
    {
        private BossController boss;
        private DamageReceiver player;
        private CharacterMotor playerMotor;
        private BossSkillSettings skills;
        private BossMoveRule blinkRule;
        private BossMoveRule waveRule;
        private BossMoveRule retreatRule;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.sceneLoaded += SelectComboOnly;
            try
            {
                yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            }
            finally
            {
                SceneManager.sceneLoaded -= SelectComboOnly;
            }
            yield return null;
            boss = Object.FindAnyObjectByType<BossController>();
            Assert.IsNotNull(boss, "Arena 应默认启用 Boss");
            var controller = Object.FindAnyObjectByType<PlayerController>();
            player = controller.GetComponent<DamageReceiver>();
            playerMotor = controller.GetComponent<CharacterMotor>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (skills != null)
            {
                skills.blinkSlash.rule = blinkRule;
                skills.swordWave.rule = waveRule;
                skills.retreat.rule = retreatRule;
            }
        }

        private void SelectComboOnly(Scene scene, LoadSceneMode mode)
        {
            skills = Object.FindAnyObjectByType<BossController>().Config.Skills;
            blinkRule = skills.blinkSlash.rule;
            waveRule = skills.swordWave.rule;
            retreatRule = skills.retreat.rule;
            skills.blinkSlash.rule.weight = 0f;
            skills.swordWave.rule.weight = 0f;
            skills.retreat.rule.weight = 0f;
        }

        [UnityTest]
        public IEnumerator ChaseAndCombo_HitOncePerStrike_AndThirdStrikeIsDelayed()
        {
            Vector3 start = boss.transform.position;
            var hits = new List<float>();
            var hitTimes = new List<double>();
            player.Damaged += hit =>
            {
                hits.Add(hit.FinalDamage);
                hitTimes.Add(Time.timeAsDouble);
            };

            yield return WaitFor(() => boss.Status == BossStatus.Combo, 2.5f);
            Assert.Greater(Vector3.Distance(start, boss.transform.position), 1f);
            DamageResult duringWindup = StrikeBoss(20f, 100f, AttackImpact.ChargedHeavy);
            Assert.AreEqual(20f, duringWindup.FinalDamage);
            Assert.AreEqual(boss.Guard.Max, boss.Guard.Current);
            Assert.AreEqual(BossStatus.Combo, boss.Status, "格挡条未破时，前摇有霸体");

            yield return WaitFor(() => hits.Count >= 3, 3.5f);
            CollectionAssert.AreEqual(new[] { 12f, 12f, 24f }, hits);
            Assert.Greater(hitTimes[2] - hitTimes[1], hitTimes[1] - hitTimes[0] + 0.2);
            Assert.AreEqual(52f, player.Health.Current);
            yield return new WaitForSeconds(0.55f);
            Assert.AreEqual(3, hits.Count, "第三刀的两把剑应共用一次判定");
            Assert.AreEqual(BossStatus.Combo, boss.Status, "第三刀命中后有后摇");
            Assert.IsFalse(boss.CanGuard);
        }

        [UnityTest]
        public IEnumerator GuardReducesDamage_ThenBrokenAndWeaknessRecover()
        {
            playerMotor.Teleport(new Vector3(0f, 0f, -22f));
            DamageResult block = StrikeBoss(50f, 25f);
            Assert.AreEqual(HitOutcome.Blocked, block.Outcome);
            Assert.AreEqual(10f, block.FinalDamage);
            Assert.AreEqual(75f, boss.Guard.Current);

            DamageResult breakingHit = StrikeBoss(50f, 75f);
            Assert.AreEqual(HitOutcome.Blocked, breakingHit.Outcome);
            Assert.AreEqual(BossStatus.Broken, boss.Status);
            Assert.AreEqual(0f, boss.Guard.Current);
            Assert.AreEqual(30f, StrikeBoss(20f, 30f).FinalDamage);
            Assert.AreEqual(50f, StrikeBoss(20f, 30f, AttackImpact.ChargedHeavy).FinalDamage);

            Time.timeScale = 4f;
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(BossStatus.Broken, boss.Status);
            Assert.AreEqual(0f, boss.Guard.Current);
            yield return WaitFor(() => boss.Guard.IsWeak, 3f);
            Assert.IsFalse(boss.CanGuard);
            Assert.AreEqual(20f, StrikeBoss(20f, 30f).FinalDamage);
            yield return WaitFor(() => !boss.Guard.IsWeak, 7.5f);
            Assert.AreEqual(boss.Guard.Max, boss.Guard.Current);
        }

        [UnityTest]
        public IEnumerator WeakBossCanBeInterruptedDuringWindup_AndDeathClosesHitbox()
        {
            playerMotor.Teleport(new Vector3(0f, 0f, -12f));
            StrikeBoss(10f, 100f);
            Time.timeScale = 3f;
            yield return WaitFor(() => boss.Guard.IsWeak, 5f);

            playerMotor.Teleport(boss.transform.position + boss.transform.forward * 2f);
            yield return WaitFor(() => boss.Status == BossStatus.Combo && boss.WindupRemaining > 0.1f, 1f);
            StrikeBoss(20f, 0f, AttackImpact.Counter);
            Assert.AreEqual(BossStatus.Staggered, boss.Status);
            Assert.AreEqual(player.Health.Max, player.Health.Current);

            yield return WaitFor(() => boss.IsStriking, 3.5f);
            Assert.IsTrue(StrikeBoss(9999f, 0f).Killed);
            Assert.AreEqual(BossStatus.Dead, boss.Status);
            Assert.IsFalse(boss.GetComponent<BossWeaponRig>().Hitbox.IsActive);
            Assert.IsFalse(boss.GetComponent<LockOnTarget>().IsAvailable);
            float playerHealth = player.Health.Current;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(playerHealth, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator SeeThroughCounterCanBreakGuardDuringTheBossCombo()
        {
            yield return WaitFor(() => boss.Status == BossStatus.Combo && boss.WindupRemaining > 0.1f, 2.5f);
            DamageResult counter = StrikeBoss(40f, 60f, AttackImpact.Counter | AttackImpact.ChargedHeavy);
            Assert.AreEqual(40f, counter.FinalDamage);
            Assert.AreEqual(40f, boss.Guard.Current);
            Assert.AreEqual(BossStatus.Combo, boss.Status);
            StrikeBoss(40f, 50f, AttackImpact.Counter);
            Assert.AreEqual(BossStatus.Broken, boss.Status);
            Assert.AreEqual(0f, boss.Guard.Current);
            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(player.Health.Max, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator GuardRecoveryWaitsUntilFourSecondsWithoutAHit()
        {
            playerMotor.Teleport(new Vector3(0f, 0f, -22f));
            StrikeBoss(10f, 30f);
            Time.timeScale = 4f;
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(70f, boss.Guard.Current);
            yield return WaitFor(() => boss.Guard.Current > 71f, 2f);
        }

        private DamageResult StrikeBoss(float damage, float guard, AttackImpact impact = AttackImpact.Normal)
        {
            return boss.Receiver.TakeDamage(new DamageInfo
            {
                Amount = damage,
                GuardDamage = guard,
                Impact = impact,
                Attacker = player.gameObject
            });
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            double deadline = Time.timeAsDouble + timeout;
            yield return new WaitUntil(() => condition() || Time.timeAsDouble >= deadline);
            Assert.IsTrue(condition(), $"预期状态未在 {timeout} 秒内出现");
        }
    }
}
