using System;
using System.Collections;
using System.Collections.Generic;
using BossFight.Boss;
using BossFight.CameraRig;
using BossFight.Character;
using BossFight.Combat;
using BossFight.Game;
using BossFight.Player;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BossFight.Tests
{
    public sealed class FightFlowTests
    {
        [UnityTest]
        public IEnumerator HudTracksHealthStaminaAndBossGuard()
        {
            yield return LoadArenaNearGuard();
            var fight = Object.FindAnyObjectByType<FightDirector>();
            Assert.AreEqual(FightResult.Ongoing, fight.Result);
            Assert.IsNull(GameObject.Find("Fight Result"));

            fight.PlayerHealth.ApplyDamage(25f);
            fight.PlayerStamina.CostMultiplier = 1f; // 不受调试用的体力倍率影响
            Assert.IsTrue(fight.PlayerStamina.TrySpend(30f));
            Assert.AreEqual(HitOutcome.Blocked, HitBoss(fight, 50f, 25f).Outcome);
            yield return null;

            Assert.AreEqual(0.75f, Fill("Player Health"), 0.01f);
            Assert.AreEqual(0.70f, Fill("Player Stamina"), 0.01f);
            Assert.AreEqual(1190f / 1200f, Fill("Boss Health"), 0.01f);
            Assert.AreEqual(0.75f, Fill("Boss Guard"), 0.01f);
            Assert.AreEqual(0f, Fill("Skill Gauge"), 0.01f);
        }

        [UnityTest]
        public IEnumerator DefeatShowsResult_AndRestartReloadsTheFight()
        {
            yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            var fight = Object.FindAnyObjectByType<FightDirector>();
            fight.PlayerStamina.TrySpend(30f);
            fight.PlayerHealth.ApplyDamage(999f);
            yield return WaitFor(() => GameObject.Find("Fight Result") != null);

            Assert.AreEqual(FightResult.Defeat, fight.Result);
            Assert.AreEqual("战败", GameObject.Find("Result Title").GetComponent<Text>().text);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsFalse(fight.PlayerHealth.GetComponent<PlayerController>().enabled);

            int oldScene = SceneManager.GetActiveScene().handle;
            Button restart = GameObject.Find("Restart").GetComponent<Button>();
            Assert.AreSame(restart.gameObject, EventSystem.current.currentSelectedGameObject);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)restart.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, "重开按钮应位于 Canvas 可点击区域");
            Assert.AreSame(restart.gameObject, hits[0].gameObject);
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return WaitFor(() => SceneManager.GetActiveScene().handle != oldScene);
            yield return null;

            var next = Object.FindAnyObjectByType<FightDirector>();
            Assert.AreEqual(FightResult.Ongoing, next.Result);
            Assert.AreEqual(next.PlayerHealth.Max, next.PlayerHealth.Current);
            Assert.AreEqual(next.PlayerStamina.Max, next.PlayerStamina.Current);
            Assert.AreEqual(next.BossHealth.Max, next.BossHealth.Current);
            Assert.AreEqual(next.Boss.Guard.Max, next.Boss.Guard.Current);
            Assert.IsFalse(next.Boss.IsPhaseTwo);
            Assert.IsNull(GameObject.Find("Fight Result"));
        }

        [UnityTest]
        public IEnumerator LethalGuardBreakHeavy_PlaysCloseUpBeforeVictory()
        {
            yield return LoadArenaNearGuard();
            var fight = Object.FindAnyObjectByType<FightDirector>();
            Assert.AreEqual(HitOutcome.Blocked, HitBoss(fight, 10f, 100f).Outcome);
            Assert.AreEqual(BossStatus.Broken, fight.Boss.Status);

            var finisher = Object.FindAnyObjectByType<FinisherCamera>();
            var rig = finisher.GetComponent<CinemachineCamera>();
            float beforeFov = rig.Lens.FieldOfView;
            float beforeScale = Time.timeScale;
            DamageResult hit = HitBoss(fight, 900f, 0f, AttackImpact.ChargedHeavy);
            Assert.IsTrue(hit.Killed);
            Assert.IsTrue(hit.IsGuardBreakFinisher);
            Assert.IsTrue(finisher.IsPlaying);
            Assert.IsTrue(fight.Boss.GetComponent<LocalHitStop>().IsFrozen);
            Assert.IsTrue(fight.PlayerHealth.GetComponent<LocalHitStop>().IsFrozen);
            Assert.AreEqual(beforeScale, Time.timeScale);

            yield return new WaitForSecondsRealtime(0.13f);
            Assert.AreEqual(FightResult.Victory, fight.Result);
            Assert.IsNull(GameObject.Find("Fight Result"));
            Assert.Less(rig.Lens.FieldOfView, beforeFov - 5f);

            yield return WaitFor(() => GameObject.Find("Fight Result") != null);
            Assert.AreEqual(beforeFov, rig.Lens.FieldOfView, 0.01f);
            Assert.AreEqual("胜利", GameObject.Find("Result Title").GetComponent<Text>().text);
        }

        [UnityTest]
        public IEnumerator SimultaneousDeathsResolveAsDefeat()
        {
            yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            var fight = Object.FindAnyObjectByType<FightDirector>();
            fight.BossHealth.ApplyDamage(9999f);
            fight.PlayerHealth.ApplyDamage(9999f);
            yield return WaitFor(() => fight.ResultReady);
            Assert.AreEqual(FightResult.Defeat, fight.Result);
        }

        private static IEnumerator LoadArenaNearGuard()
        {
            void PlacePlayer(Scene scene, LoadSceneMode mode)
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                player.GetComponent<CharacterMotor>().Teleport(new Vector3(0f, 0f, -3.9f));
            }

            SceneManager.sceneLoaded += PlacePlayer;
            try
            {
                yield return SceneManager.LoadSceneAsync("Arena", LoadSceneMode.Single);
            }
            finally
            {
                SceneManager.sceneLoaded -= PlacePlayer;
            }
        }

        private static DamageResult HitBoss(FightDirector fight, float damage, float guard, AttackImpact impact = AttackImpact.Normal)
        {
            return fight.Boss.Receiver.TakeDamage(new DamageInfo
            {
                Amount = damage,
                GuardDamage = guard,
                Impact = impact,
                Attacker = fight.PlayerHealth.gameObject
            });
        }

        private static float Fill(string row)
        {
            return ((RectTransform)GameObject.Find(row).transform.Find("Track/Fill")).anchorMax.x;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            float deadline = Time.unscaledTime + 2.5f;
            yield return new WaitUntil(() => condition() || Time.unscaledTime >= deadline);
            Assert.IsTrue(condition(), "战斗界面未在预期时间内更新");
        }
    }
}
