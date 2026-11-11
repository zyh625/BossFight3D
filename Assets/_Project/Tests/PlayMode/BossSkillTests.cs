using System;
using System.Collections;
using BossFight.Boss;
using BossFight.Character;
using BossFight.Combat;
using BossFight.Game;
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

        // ---- P1：旋风斩、后撤突进、长剑形态 ----

        [UnityTest]
        public IEnumerator Whirlwind_HitsRepeatedly_AndReflectsDarts()
        {
            BossMoveRule waveRule = default;
            try
            {
                yield return LoadArena(5f, skills =>
                {
                    waveRule = skills.swordWave.rule;
                    skills.swordWave.rule.weight = 0f;
                });
                int hits = 0;
                player.Damaged += result => hits += result.FinalDamage > 0f ? 1 : 0;
                yield return WaitFor(() => boss.Status == BossStatus.Whirlwind, 1f);
                Assert.IsFalse(boss.ReflectsProjectiles, "蓄势阶段还不会反弹");
                yield return WaitFor(() => boss.ReflectsProjectiles, 1.2f);

                var launcher = player.GetComponent<DartLauncher>();
                EnergyDart dart = launcher.Fire(0f);
                yield return WaitFor(() => dart.WasReflected, 1f);
                Assert.IsTrue(dart.WasReflected, "旋转中的 Boss 应把飞镖弹回");

                yield return WaitFor(() => boss.Status != BossStatus.Whirlwind, 4.5f);
                Assert.GreaterOrEqual(hits, 2, "旋风斩是多段伤害");
            }
            finally
            {
                boss.Config.Skills.swordWave.rule = waveRule;
            }
        }

        [UnityTest]
        public IEnumerator Victory_RecallsReflectedDartBeforeItCanDamagePlayer()
        {
            BossMoveRule waveRule = default;
            try
            {
                yield return LoadArena(5f, skills =>
                {
                    waveRule = skills.swordWave.rule;
                    skills.swordWave.rule.weight = 0f;
                });
                var fight = Object.FindAnyObjectByType<FightDirector>();
                yield return WaitFor(() => boss.ReflectsProjectiles, 1.2f);
                EnergyDart dart = player.GetComponent<DartLauncher>().Fire(0f);
                yield return WaitFor(() => dart.WasReflected, 1f);
                Assert.IsTrue(dart.IsFlying);

                HitBoss(boss.Receiver.Health.Max * 2f, 0f);
                yield return WaitFor(() => fight.Result == FightResult.Victory, 0.5f);
                float healthOnVictory = player.Health.Current;

                Assert.IsFalse(dart.IsFlying);
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(healthOnVictory, player.Health.Current);
            }
            finally
            {
                boss.Config.Skills.swordWave.rule = waveRule;
            }
        }

        [UnityTest]
        public IEnumerator RetreatCanChainIntoALunge()
        {
            yield return LoadArena(3.7f);
            var skills = boss.Config.Skills;
            float oldBase = skills.retreat.baseChance;
            float oldMax = skills.retreat.maxChance;
            float oldLunge = skills.lunge.chance;
            skills.retreat.baseChance = 1f;
            skills.retreat.maxChance = 1f;
            skills.lunge.chance = 1f;
            try
            {
                HitBoss(10f, 10f);
                Assert.AreEqual(BossStatus.Retreat, boss.Status);
                yield return WaitFor(() => boss.Status == BossStatus.Lunge, 1f);
                Assert.AreEqual(BossStatus.Lunge, boss.Status, "后撤收招后应接突进");

                float before = boss.DirectionToTargetDistance;
                yield return WaitFor(() => boss.Status == BossStatus.Approach, 2.5f);
                Assert.Less(boss.DirectionToTargetDistance, before - 2f, "突进应明显拉近距离");
            }
            finally
            {
                skills.retreat.baseChance = oldBase;
                skills.retreat.maxChance = oldMax;
                skills.lunge.chance = oldLunge;
            }
        }

        [UnityTest]
        public IEnumerator PhaseTwoAtRange_MergesIntoALongsword_WithAFourHitCombo()
        {
            yield return LoadArena(9f);
            var skills = boss.Config.Skills;
            BossMoveRule blinkRule = skills.blinkSlash.rule;
            BossMoveRule waveRule = skills.swordWave.rule;
            skills.blinkSlash.rule.weight = 0f;
            skills.swordWave.rule.weight = 0f;
            try
            {
                Assert.AreEqual(3, boss.ComboLength);
                Health health = boss.Receiver.Health;
                // 打掉 60% 的血触发转阶段。Boss 正在格挡时伤害只剩 20%，按当时的状态换算出需要的数值
                HitBoss(health.Max * 0.6f / (boss.CanGuard ? 0.2f : 1f), 0f);
                Assert.IsTrue(boss.IsPhaseTwo);

                playerMotor.Teleport(boss.transform.position + Vector3.back * 9f);
                yield return WaitFor(() => boss.IsLongsword, 4.5f);
                Assert.IsTrue(boss.IsLongsword, "二阶段且玩家在远处时应合剑");
                Assert.AreEqual(4, boss.ComboLength, "长剑形态是四连击");
                Assert.IsFalse(boss.CanGuard, "长剑形态不格挡");

                float guardBefore = boss.Guard.Current;
                DamageResult result = HitBoss(10f, 15f);
                Assert.AreEqual(HitOutcome.Hit, result.Outcome);
                Assert.AreEqual(10f, result.FinalDamage, 0.01f, "长剑形态下伤害不被格挡削减");
                Assert.AreEqual(guardBefore - 15f, boss.Guard.Current, 0.5f, "同时照常削减格挡条");
            }
            finally
            {
                skills.blinkSlash.rule = blinkRule;
                skills.swordWave.rule = waveRule;
            }
        }

        [UnityTest]
        public IEnumerator LongswordExpiresDuringContinuousApproach()
        {
            yield return LoadArena(9f);
            var skills = boss.Config.Skills;
            var settings = skills.longsword;
            var blinkRule = skills.blinkSlash.rule;
            var waveRule = skills.swordWave.rule;
            var whirlRule = skills.whirlwind.rule;
            skills.blinkSlash.rule.weight = 0f;
            skills.swordWave.rule.weight = 0f;
            skills.whirlwind.rule.weight = 0f;
            skills.longsword.duration = 0.25f;
            try
            {
                HitBoss(boss.Receiver.Health.Max * 0.6f / (boss.CanGuard ? 0.2f : 1f), 0f);
                playerMotor.Teleport(boss.transform.position + Vector3.back * 9f);
                yield return WaitFor(() => boss.IsLongsword, 4.5f);
                yield return new WaitForSeconds(0.5f);

                Assert.AreEqual(BossStatus.Approach, boss.Status);
                Assert.IsFalse(boss.IsLongsword);
                Assert.AreEqual(3, boss.ComboLength);
            }
            finally
            {
                skills.longsword = settings;
                skills.blinkSlash.rule = blinkRule;
                skills.swordWave.rule = waveRule;
                skills.whirlwind.rule = whirlRule;
            }
        }

        private IEnumerator LoadArena(float distance, Action<BossSkillSettings> configureSkills = null)
        {
            void PositionPlayer(Scene scene, LoadSceneMode mode)
            {
                boss = Object.FindAnyObjectByType<BossController>();
                configureSkills?.Invoke(boss.Config.Skills);
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
