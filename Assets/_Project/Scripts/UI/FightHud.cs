using BossFight.Boss;
using BossFight.Game;
using UnityEngine;
using UnityEngine.UI;

namespace BossFight.UI
{
    [DefaultExecutionOrder(110)]
    public sealed class FightHud : MonoBehaviour
    {
        [SerializeField] private FightDirector fight;
        [SerializeField] private GameObject content;
        [SerializeField] private BarView playerHealth;
        [SerializeField] private BarView playerStamina;
        [SerializeField] private BarView skillGauge;
        [SerializeField] private BarView bossHealth;
        [SerializeField] private BarView bossGuard;
        [SerializeField] private BarView charge;
        [SerializeField] private Text phase;
        [SerializeField] private Text status;
        [SerializeField] private Text freeStamina;
        [SerializeField] private RectTransform lockMarker;

        private RectTransform canvas;
        private Camera view;

        private void Awake()
        {
            canvas = (RectTransform)transform;
            skillGauge.SetValue(0f, 100f);
        }

        private void Start() => view = Camera.main;

        private void LateUpdate()
        {
            bool visible = fight.IsBossFight && !fight.ResultReady;
            if (content.activeSelf != visible)
            {
                content.SetActive(visible);
            }
            if (!visible)
            {
                return;
            }

            playerHealth.SetValue(fight.PlayerHealth.Current, fight.PlayerHealth.Max);
            var stamina = fight.PlayerStamina;
            playerStamina.SetValue(stamina.Current, stamina.Max);
            bossHealth.SetValue(fight.BossHealth.Current, fight.BossHealth.Max);
            BossController boss = fight.Boss;
            bossGuard.SetValue(boss.Guard.Current, boss.Guard.Max);
            phase.text = boss.IsPhaseTwo ? "阶段 II" : "阶段 I";
            status.text = StatusLabel(boss);
            freeStamina.text = stamina.HasFreeSpending ? $"免体力  {stamina.FreeSpendingRemaining:0.0}s" : "";

            float chargeRatio = fight.ChargeRatio;
            charge.gameObject.SetActive(chargeRatio > 0f);
            if (chargeRatio > 0f)
            {
                charge.SetValue(chargeRatio * 100f, 100f);
            }
            UpdateLockMarker();
        }

        private void UpdateLockMarker()
        {
            var target = fight.Targeting.Target;
            Vector3 point = target != null && view != null
                ? view.WorldToViewportPoint(target.AimPoint + Vector3.up * 0.55f)
                : Vector3.zero;
            bool visible = point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
            lockMarker.gameObject.SetActive(visible);
            if (visible)
            {
                lockMarker.anchoredPosition = new Vector2(point.x * canvas.rect.width, point.y * canvas.rect.height);
            }
        }

        private static string StatusLabel(BossController boss)
        {
            return boss.Status switch
            {
                BossStatus.Broken => "破防 · 重击可触发特写",
                BossStatus.Staggered => "出招被打断",
                BossStatus.Retreat => "后撤瞬身",
                BossStatus.PhaseTransition => $"转阶段保护  {boss.PhaseTransitionRemaining:0.00}s",
                BossStatus.Combo => MoveLabel($"三连斩 {boss.StrikeNumber}/{boss.ComboLength}", boss.WindupRemaining, boss.IsStriking),
                BossStatus.BlinkSlash => MoveLabel("瞬身斩", boss.WindupRemaining),
                BossStatus.SwordWave => MoveLabel("剑气", boss.WindupRemaining),
                BossStatus.Dead => "暗影剑客已击败",
                BossStatus.FightOver => "玩家倒下",
                _ => boss.CanGuard ? "追击 / 格挡" : "虚弱"
            };
        }

        private static string MoveLabel(string name, float windup, bool striking = false)
        {
            return windup > 0f ? $"{name} · 前摇 {windup:0.00}s" : $"{name} · {(striking ? "挥砍" : "收招")}";
        }
    }
}
