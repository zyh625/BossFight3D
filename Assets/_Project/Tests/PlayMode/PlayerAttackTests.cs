using System.Collections;
using BossFight.Character;
using BossFight.Combat;
using BossFight.DevTools;
using BossFight.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BossFight.Tests
{
    /// <summary>
    /// 冒烟测试：加载真实的 Arena 场景，用模拟的鼠标输入走完
    /// "按键 → 输入意图 → 状态机 → Hitbox → 伤害管线 → Health" 整条链路。
    /// </summary>
    public sealed class PlayerAttackTests
    {
        private readonly InputTestFixture input = new InputTestFixture();
        private Mouse mouse;
        private PlayerController player;
        private Stamina stamina;
        private Health dummyHealth;
        private DamageReceiver dummyReceiver;
        private DamageReceiver playerReceiver;
        private DamageResult lastDummyHit;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            input.Setup();
            mouse = InputSystem.AddDevice<Mouse>();

            yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            stamina = player.GetComponent<Stamina>();
            playerReceiver = player.GetComponent<DamageReceiver>();
            dummyReceiver = Object.FindFirstObjectByType<TrainingDummy>().GetComponent<DamageReceiver>();
            lastDummyHit = default;
            dummyReceiver.Damaged += result => lastDummyHit = result;
            dummyHealth = dummyReceiver.Health;

            // 木桩在原点，把玩家放到它正前方 1.3 米处（玩家默认面朝 +Z）
            player.GetComponent<CharacterMotor>().Teleport(new Vector3(0f, 0f, -1.3f));
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            input.TearDown();
        }

        private IEnumerator Click()
        {
            input.Press(mouse.leftButton);
            yield return null;
            input.Release(mouse.leftButton);
        }

        [UnityTest]
        public IEnumerator LeftClick_PerformsLightAttack_DamagesDummyAndSpendsStamina()
        {
            yield return Click();
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName, "左键按下的当帧就应进入攻击状态");

            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual(dummyHealth.Max - 18f, dummyHealth.Current, 0.01f, "轻攻击第一段应造成 18 点伤害，且只结算一次");
            Assert.Less(stamina.Current, stamina.Max, "轻攻击应消耗体力");
            Assert.AreEqual("PlayerLocomotionState", player.CurrentStateName, "攻击结束后应回到移动状态");
        }

        [UnityTest]
        public IEnumerator ThreeClicks_PerformFullCombo()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return Click();
                // 在上一段的后摇里提前按下，靠输入缓冲接上下一段
                yield return new WaitForSeconds(0.38f);
            }

            yield return new WaitForSeconds(1f);

            Assert.AreEqual(dummyHealth.Max - (18f + 18f + 26f), dummyHealth.Current, 0.01f, "三连应造成 18+18+26 点伤害");
        }

        [UnityTest]
        public IEnumerator HoldRight_DrainsPastFullCharge_ThenReleaseHitsWithoutAnExtraCost()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual("PlayerChargeState", player.CurrentStateName, "按住右键应进入蓄力");

            yield return new WaitForSeconds(1.05f);
            Assert.AreEqual(1f, player.ChargeRatio, 0.001f);
            Assert.AreEqual("PlayerChargeState", player.CurrentStateName, "满蓄力后右键按住时仍应继续蓄力");
            Assert.AreEqual(dummyHealth.Max, dummyHealth.Current, "松手前不能命中木桩");
            Assert.Less(stamina.Current, stamina.Max - 35f, "满蓄力后仍然持续耗体力");

            float beforeRelease = stamina.Current;
            input.Release(mouse.rightButton);
            yield return null;
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);
            Assert.Greater(stamina.Current, beforeRelease - 10f, "松手时只结算最后一帧蓄力消耗");
            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(dummyHealth.Max - 45f * 1.55f, dummyHealth.Current, 0.01f, "满蓄力重击伤害应为 45 × 1.55");
            Assert.AreEqual("PlayerLocomotionState", player.CurrentStateName);
        }

        [UnityTest]
        public IEnumerator ChargeExhaustion_ReleasesOnePartialHeavy_WhileRightStaysHeld()
        {
            Assert.IsTrue(stamina.TrySpend(86f));
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0f, stamina.Current);
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName, "体力耗尽应自动出招");
            yield return new WaitForSeconds(2.4f);

            Assert.AreEqual(dummyHealth.Max - 45f * (1f + 0.55f * 0.4f), dummyHealth.Current, 0.01f);
            Assert.AreEqual("PlayerLocomotionState", player.CurrentStateName, "继续按住右键不会再蓄力");
            input.Release(mouse.rightButton);
        }

        [UnityTest]
        public IEnumerator LightAttack_ThenRightAndLeft_ChainsIntoVariedAttack()
        {
            yield return Click();
            yield return new WaitForSeconds(0.2f);

            // 轻攻击后摇中：按住右键再按左键，切手技进入缓冲。
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.1f);
            yield return Click();
            yield return new WaitForSeconds(0.3f);
            input.Release(mouse.rightButton);
            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(dummyHealth.Max - (18f + 32f), dummyHealth.Current, 0.01f);
        }

        [UnityTest]
        public IEnumerator VariedAttack_SeeThroughCountersEarlyAndGrantsFreeStamina()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.15f);
            float staminaAfterCharging = stamina.Current;
            yield return Click();
            Assert.AreEqual(staminaAfterCharging - 20f, stamina.Current, 0.01f);

            Assert.AreEqual(HitOutcome.Parried, HitPlayer().Outcome);
            Assert.AreEqual(playerReceiver.Health.Max, playerReceiver.Health.Current);
            Assert.AreEqual(staminaAfterCharging, stamina.Current, 0.01f);
            Assert.AreEqual(3f, stamina.FreeSpendingRemaining, 0.01f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome, "每次出招只识破一次");
            input.Release(mouse.rightButton);

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(32f * 1.3f, lastDummyHit.FinalDamage, 0.01f, "识破应跳过剩余前摇");
            Assert.AreEqual(50f, lastDummyHit.Info.GuardDamage, 0.01f);
            float before = stamina.Current;
            Assert.IsTrue(stamina.TrySpend(stamina.Max));
            Assert.AreEqual(0f, stamina.Drain(35f));
            Assert.AreEqual(before, stamina.Current, 0.01f);

            yield return new WaitForSeconds(3f);
            Assert.IsFalse(stamina.HasFreeSpending);
            Assert.IsTrue(stamina.TrySpend(10f, out float spent));
            Assert.AreEqual(10f, spent);
        }

        [UnityTest]
        public IEnumerator HoldingChargeTakesDamage_ReleasedHeavyCanSeeThrough()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome, "蓄力过程没有格挡或识破");
            input.Release(mouse.rightButton);
            yield return null;

            Assert.AreEqual(HitOutcome.Parried, HitPlayer().Outcome);
            Assert.AreEqual(stamina.Max, stamina.Current, 0.01f);
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(lastDummyHit.FinalDamage, 45f * 1.3f);
            Assert.AreEqual(60f, lastDummyHit.Info.GuardDamage);
        }

        [UnityTest]
        public IEnumerator SimultaneousButtons_UseVariedAttack_AndLateHitsAreNotParried()
        {
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return null;
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);
            Assert.AreEqual(stamina.Max - 20f, stamina.Current, 0.01f);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return new WaitForSeconds(0.21f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome);
            Assert.IsFalse(stamina.HasFreeSpending);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(32f, lastDummyHit.FinalDamage, 0.01f);
        }

        [UnityTest]
        public IEnumerator Roll_InvulnerabilityRefundsOnlyOnce()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            input.Press(keyboard.spaceKey);
            yield return null;
            input.Release(keyboard.spaceKey);
            Assert.AreEqual("PlayerRollState", player.CurrentStateName);
            Assert.AreEqual(stamina.Max - 20f, stamina.Current, 0.01f);
            yield return new WaitForSeconds(0.08f);

            Assert.AreEqual(HitOutcome.Dodged, HitPlayer().Outcome);
            Assert.AreEqual(stamina.Max, stamina.Current, 0.01f);
            Assert.IsTrue(stamina.TrySpend(5f));
            Assert.AreEqual(HitOutcome.Dodged, HitPlayer().Outcome);
            Assert.AreEqual(stamina.Max - 5f, stamina.Current, 0.01f);
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome);
        }

        private DamageResult HitPlayer()
        {
            return playerReceiver.TakeDamage(new DamageInfo { Amount = 12f, Attacker = dummyReceiver.gameObject });
        }
    }
}
