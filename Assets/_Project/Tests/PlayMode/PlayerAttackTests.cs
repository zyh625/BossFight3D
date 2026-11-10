using System.Collections;
using BossFight.Boss;
using BossFight.Character;
using BossFight.Combat;
using BossFight.DevTools;
using BossFight.Player;
using BossFight.Player.States;
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
        private Keyboard keyboard;
        private PlayerController player;
        private Stamina stamina;
        private Health dummyHealth;
        private DamageReceiver dummyReceiver;
        private DamageReceiver playerReceiver;
        private BossController boss;
        private DamageResult lastDummyHit;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 批处理模式不限帧率时能跑到几千帧/秒，模拟的按键偶尔不会被识别为"本帧按下"。
            // 限定到接近真实游玩的帧率，让用例的时序稳定。
            Application.targetFrameRate = 120;

            input.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();

            yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            yield return null;

            player = Object.FindAnyObjectByType<PlayerController>();
            stamina = player.GetComponent<Stamina>();
            stamina.CostMultiplier = 1f; // 用例按设计数值断言，不受调试用的体力倍率影响
            playerReceiver = player.GetComponent<DamageReceiver>();
            boss = Object.FindAnyObjectByType<BossController>();
            if (boss != null)
            {
                boss.gameObject.SetActive(false);
            }
            var dummy = Object.FindAnyObjectByType<TrainingDummy>(FindObjectsInactive.Include);
            dummy.enabled = false; // 用例精确控制来袭时刻，独立于场景的陪练开关。
            dummy.gameObject.SetActive(true);
            dummyReceiver = dummy.GetComponent<DamageReceiver>();
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
            Application.targetFrameRate = -1;
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
            Assert.Greater(player.VisibleChargeRatio, 0f);
            Transform grip = player.GetComponentInChildren<Hitbox>().transform.parent;
            Assert.Less(grip.localPosition.z, 0f, "蓄力时剑应在身体后方");

            float beforeRelease = stamina.Current;
            float groundY = player.transform.position.y;
            input.Release(mouse.rightButton);
            yield return null;
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);
            Assert.AreEqual(PlayerAttackKind.ChargedHeavy, player.ActiveAttack);
            Assert.Greater(stamina.Current, beforeRelease - 10f, "松手时只结算最后一帧蓄力消耗");
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(player.transform.position.y, groundY + 0.5f, "释放蓄力重击应先跃起");
            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(dummyHealth.Max - 45f * 1.55f, dummyHealth.Current, 0.01f, "满蓄力重击伤害应为 45 × 1.55");
            Assert.AreEqual(AttackImpact.ChargedHeavy, lastDummyHit.Info.Impact);
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
            input.Release(mouse.rightButton);

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(32f * 1.3f, lastDummyHit.FinalDamage, 0.01f, "识破应跳过剩余前摇");
            Assert.AreEqual(50f, lastDummyHit.Info.GuardDamage, 0.01f);
            Assert.AreEqual(AttackImpact.Counter, lastDummyHit.Info.Impact);
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
        public IEnumerator HoldingChargeTakesDamageAndCancelsTheHeavy()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome, "蓄力过程没有格挡或识破");
            Assert.AreEqual("PlayerHitState", player.CurrentStateName);
            input.Release(mouse.rightButton);
            yield return new WaitForSeconds(0.65f);
            Assert.AreEqual(dummyHealth.Max, dummyHealth.Current, "受击打断的蓄力不能在松键时出招");
        }

        [UnityTest]
        public IEnumerator ReleasedHeavyCanSeeThrough()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.25f);
            input.Release(mouse.rightButton);
            yield return null;

            Assert.AreEqual(HitOutcome.Parried, HitPlayer().Outcome);
            Assert.AreEqual(stamina.Max, stamina.Current, 0.01f);
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(lastDummyHit.FinalDamage, 45f * 1.3f);
            Assert.AreEqual(60f, lastDummyHit.Info.GuardDamage);
            Assert.AreEqual(AttackImpact.Counter | AttackImpact.ChargedHeavy, lastDummyHit.Info.Impact);
        }

        [UnityTest]
        public IEnumerator SimultaneousButtons_UseVariedAttack_AndLateHitsAreNotParried()
        {
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return null;
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);
            Assert.AreEqual(PlayerAttackKind.Combination, player.ActiveAttack);
            Assert.AreEqual(0f, player.VisibleChargeRatio);
            Assert.AreEqual(stamina.Max - 20f, stamina.Current, 0.01f);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return new WaitForSeconds(0.21f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome);
            Assert.IsFalse(stamina.HasFreeSpending);
            Assert.AreEqual("PlayerHitState", player.CurrentStateName);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(dummyHealth.Max, dummyHealth.Current, "前摇中受击应打断切手技");
        }

        [UnityTest]
        public IEnumerator QuickRightThenLeft_PerformsAnOverheadChopWithoutCharge()
        {
            Hitbox weapon = player.GetComponentInChildren<Hitbox>();
            float startingBladeY = 0f;
            weapon.Activated += _ => startingBladeY = weapon.transform.forward.y;
            input.Press(mouse.rightButton);
            float chargeDeadline = Time.time + 0.15f;
            yield return new WaitUntil(() => player.ChargeRatio >= 0.025f || Time.time >= chargeDeadline);
            Assert.GreaterOrEqual(player.ChargeRatio, 0.025f);
            Assert.AreEqual(0f, player.VisibleChargeRatio);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));
            yield return null;
            Assert.AreEqual(PlayerAttackKind.Combination, player.ActiveAttack, player.CurrentStateName);
            Assert.AreEqual(0f, player.VisibleChargeRatio);
            Assert.AreEqual(stamina.Max - 20f, stamina.Current, 0.01f);
            InputSystem.QueueStateEvent(mouse, new MouseState());

            float deadline = Time.time + 0.65f;
            yield return new WaitUntil(() => (weapon.IsActive && weapon.transform.forward.y < -0.2f) || Time.time >= deadline);
            Assert.Greater(startingBladeY, 0.8f, "组合技从举剑起手");
            Assert.IsTrue(weapon.IsActive);
            Assert.Less(weapon.transform.forward.y, -0.2f, "同一刀在判定期间向下劈");
            Assert.Less(player.transform.position.y, 0.15f, "组合技保持在地面");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(dummyHealth.Max - 32f, dummyHealth.Current, 0.01f);
        }

        [UnityTest]
        public IEnumerator QuickLeftThenRight_ConvertsTheWindupIntoOneCombination()
        {
            input.Press(mouse.leftButton);
            yield return null;
            Assert.AreEqual(PlayerAttackKind.Light, player.ActiveAttack);
            input.Press(mouse.rightButton);
            yield return null;
            Assert.AreEqual(PlayerAttackKind.Combination, player.ActiveAttack);
            Assert.AreEqual(stamina.Max - 20f, stamina.Current, 0.01f);
            Assert.AreEqual(0f, player.VisibleChargeRatio);
            input.Release(mouse.leftButton);
            input.Release(mouse.rightButton);
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(dummyHealth.Max - 32f, dummyHealth.Current, 0.01f);
        }

        [UnityTest]
        public IEnumerator CJumpClearsTheBossGroundWaveAndCanStillTakeAHit()
        {
            Assert.IsNotNull(boss);
            dummyReceiver.gameObject.SetActive(false);
            player.GetComponent<CharacterMotor>().Teleport(new Vector3(0f, 0f, -6f));
            boss.gameObject.SetActive(true);
            float deadline = Time.time + 2f;
            yield return new WaitUntil(() => Object.FindAnyObjectByType<BossSwordWave>() != null || Time.time >= deadline);
            Assert.IsNotNull(Object.FindAnyObjectByType<BossSwordWave>(), "Boss 应在六米距离释放剑气");

            input.Press(keyboard.cKey);
            yield return null;
            input.Release(keyboard.cKey);
            Assert.AreEqual("PlayerJumpState", player.CurrentStateName);
            yield return new WaitForSeconds(0.39f);
            Assert.Greater(player.transform.position.y, 0.85f);
            Assert.AreEqual(playerReceiver.Health.Max, playerReceiver.Health.Current, "剑气应从抬起的 Hurtbox 下方经过");
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome, "跳跃没有全身无敌");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual("PlayerLocomotionState", player.CurrentStateName);
        }

        [UnityTest]
        public IEnumerator SeeThroughProtectsAgainstOnlyOneHit()
        {
            input.Press(mouse.rightButton);
            yield return new WaitForSeconds(0.1f);
            yield return Click();

            Assert.AreEqual(HitOutcome.Parried, HitPlayer().Outcome);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome);
            Assert.AreEqual("PlayerHitState", player.CurrentStateName);
            input.Release(mouse.rightButton);
        }

        [UnityTest]
        public IEnumerator HitStopIsLocal_AndHitStunCanBeCancelledByRoll()
        {
            yield return Click();
            var playerClock = player.GetComponent<LocalHitStop>();
            var dummyClock = dummyReceiver.GetComponent<LocalHitStop>();
            float globalTimeScale = Time.timeScale;
            double hitTime = playerClock.ActionTime;

            Assert.AreEqual(HitOutcome.Hit, HitPlayer().Outcome);
            Assert.AreEqual("PlayerHitState", player.CurrentStateName);
            Assert.IsTrue(playerClock.IsFrozen);
            Assert.IsTrue(dummyClock.IsFrozen);
            Assert.AreEqual(globalTimeScale, Time.timeScale);
            yield return new WaitUntil(() => !playerClock.IsFrozen);
            Assert.AreEqual(hitTime, playerClock.ActionTime, 0.03d);

            yield return new WaitUntil(() => playerClock.ActionTime - hitTime >= 0.21);
            Assert.AreEqual("PlayerHitState", player.CurrentStateName);
            input.Press(keyboard.spaceKey);
            yield return null;
            input.Release(keyboard.spaceKey);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual("PlayerRollState", player.CurrentStateName);
            Assert.AreEqual(dummyHealth.Max, dummyHealth.Current, "被打断的轻击不能残留命中");
        }

        [UnityTest]
        public IEnumerator MiddleClickLocksCameraAndMovement_AndTargetDeathUnlocks()
        {
            var motor = player.GetComponent<CharacterMotor>();
            motor.Teleport(new Vector3(0f, 0f, -5f));
            var targeting = player.GetComponent<LockOnTargeting>();
            var target = dummyReceiver.GetComponent<LockOnTarget>();
            player.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            yield return new WaitForSeconds(0.3f);

            input.Press(mouse.middleButton);
            yield return null;
            input.Release(mouse.middleButton);
            yield return new WaitForSeconds(0.25f);
            Assert.AreSame(target, targeting.Target);
            AssertInFrame(player.CameraTarget.position);
            AssertInFrame(target.AimPoint);

            input.Press(keyboard.dKey);
            yield return new WaitForSeconds(0.22f);
            Assert.AreEqual(Vector2.right, player.GetComponent<PlayerInputReader>().Move);
            input.Release(keyboard.dKey);
            Assert.Greater(player.transform.position.x, 0.3f);
            Vector3 facing = target.transform.position - player.transform.position;
            facing.y = 0f;
            Assert.Greater(Vector3.Dot(player.transform.forward, facing.normalized), 0.98f);

            input.Press(mouse.middleButton);
            yield return null;
            input.Release(mouse.middleButton);
            Assert.IsNull(targeting.Target);
            yield return null;
            input.Press(mouse.middleButton);
            yield return null;
            input.Release(mouse.middleButton);
            Assert.AreSame(target, targeting.Target);
            dummyHealth.ApplyDamage(dummyHealth.Max);
            yield return null;
            Assert.IsNull(targeting.Target);
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            input.Press(key);
            yield return null;
            input.Release(key);
        }

        // ---- 响应性：下面几条用例固定"按了就有反应"的取消规则 ----

        [UnityTest]
        public IEnumerator Roll_CancelsLightAttackRecovery_BeforeTheChainPoint()
        {
            yield return Click();
            // 轻攻击 1：前摇 0.10 + 判定 0.10，衔接点在 0.35。0.25 时判定刚结束，还没到衔接点
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);

            yield return Tap(keyboard.spaceKey);
            yield return null;

            Assert.AreEqual("PlayerRollState", player.CurrentStateName, "判定一结束就应该能用翻滚取消后摇");
        }

        [UnityTest]
        public IEnumerator Roll_CancelsLightAttackWindup()
        {
            input.Press(mouse.leftButton);
            yield return null;
            input.Release(mouse.leftButton);
            Assert.AreEqual("PlayerAttackState", player.CurrentStateName);

            yield return Tap(keyboard.spaceKey);
            yield return null;

            Assert.AreEqual("PlayerRollState", player.CurrentStateName, "轻攻击前摇中应该能直接翻滚");
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(dummyHealth.Max, dummyHealth.Current, "被翻滚取消的那一刀不应造成伤害");
        }

        [UnityTest]
        public IEnumerator MoveInput_EndsRecoveryAtTheChainPoint()
        {
            yield return Click();
            input.Press(keyboard.sKey);
            // 轻攻击 1 全长 0.50，衔接点 0.35。0.42 时如果没有移动取消，仍然会停在攻击状态
            yield return new WaitForSeconds(0.42f);

            Assert.AreEqual("PlayerLocomotionState", player.CurrentStateName, "过了衔接点，按着方向键应提前结束后摇");
            input.Release(keyboard.sKey);
        }

        [UnityTest]
        public IEnumerator EarlySecondClick_IsStillBufferedAtTheChainPoint()
        {
            yield return Click();
            // 第二下按在第一刀刚出手 0.1 秒时，离衔接点（0.35）还有约 0.25 秒
            yield return new WaitForSeconds(0.08f);
            yield return Click();
            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(dummyHealth.Max - 36f, dummyHealth.Current, 0.01f, "提前按下的第二刀应被缓冲并打出");
        }

        [UnityTest]
        public IEnumerator RollTail_ChainsDirectlyIntoAttack()
        {
            yield return Tap(keyboard.spaceKey);
            yield return null;
            Assert.AreEqual("PlayerRollState", player.CurrentStateName);

            // 翻滚全长 0.60，0.42 之后可以接动作
            yield return new WaitForSeconds(0.46f);
            Assert.AreEqual("PlayerRollState", player.CurrentStateName);
            yield return Click();

            Assert.AreEqual("PlayerAttackState", player.CurrentStateName, "翻滚收尾阶段应能直接出刀");
        }

        private static void AssertInFrame(Vector3 position)
        {
            Vector3 point = Camera.main.WorldToViewportPoint(position);
            Assert.Greater(point.z, 0f);
            Assert.That(point.x, Is.InRange(0f, 1f));
            Assert.That(point.y, Is.InRange(0f, 1f));
        }

        [UnityTest]
        public IEnumerator Roll_InvulnerabilityRefundsOnlyOnce()
        {
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
