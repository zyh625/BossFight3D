using BossFight.Combat;
using BossFight.Player;
using UnityEngine;

namespace BossFight.DevTools
{
    /// <summary>
    /// 临时的调试显示（正式 UI 在 M6）：玩家体力、当前状态、蓄力进度，目标的血量和最近一次受到的伤害。
    /// 用 IMGUI 是因为它不需要搭任何 UI 层级，适合随手加、随手删的调试信息；它有性能开销，不用于正式界面。
    /// </summary>
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Stamina playerStamina;
        [SerializeField] private DamageReceiver target;

        private float lastDamage;
        private float lastGuardDamage;
        private float comboDamage;
        private float lastHitTime = -10f;
        private int hitCount;
        private GUIStyle labelStyle;
        private DamageReceiver playerReceiver;
        private TrainingDummy dummy;
        private LockOnTargeting targeting;
        private Camera view;
        private string lastDefense;
        private float defenseTime = -10f;

        private const float ComboResetSeconds = 2f;

        private void OnEnable()
        {
            if (target != null)
            {
                target.Damaged += OnTargetDamaged;
                dummy = target.GetComponent<TrainingDummy>();
            }
            if (player != null && player.TryGetComponent(out playerReceiver))
            {
                playerReceiver.Damaged += OnPlayerDamaged;
                targeting = player.GetComponent<LockOnTargeting>();
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.Damaged -= OnTargetDamaged;
            }
            if (playerReceiver != null)
            {
                playerReceiver.Damaged -= OnPlayerDamaged;
            }
        }

        private void OnTargetDamaged(DamageResult result)
        {
            if (Time.time - lastHitTime > ComboResetSeconds)
            {
                comboDamage = 0f;
                hitCount = 0;
            }

            lastDamage = result.FinalDamage;
            lastGuardDamage = result.Info.GuardDamage;
            comboDamage += result.FinalDamage;
            hitCount++;
            lastHitTime = Time.time;
        }

        private void OnPlayerDamaged(DamageResult result)
        {
            lastDefense = result.Outcome switch
            {
                HitOutcome.Parried => "识破",
                HitOutcome.Dodged => "完美闪避",
                _ => $"受击 −{result.FinalDamage:0.#}"
            };
            defenseTime = Time.time;
        }

        private void Start()
        {
            view = Camera.main;
        }

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };

            const float width = 320f;
            float x = 20f;
            float y = Screen.height - 150f;

            if (playerReceiver != null)
            {
                Health health = playerReceiver.Health;
                DrawBar(new Rect(x, y - 42f, width, 14f), health.Current / health.Max,
                    new Color(1f, 0.25f, 0.4f), $"生命 {health.Current:0}/{health.Max:0}");
            }

            if (playerStamina != null)
            {
                float freeTime = playerStamina.FreeSpendingRemaining;
                string freeLabel = freeTime > 0f ? $"    免体力 {freeTime:0.0}s" : "";
                DrawBar(new Rect(x, y, width, 14f), playerStamina.Current / playerStamina.Max,
                    new Color(0.3f, 1f, 0.5f), $"体力 {playerStamina.Current:0}/{playerStamina.Max:0}{freeLabel}");
                y += 42f;
            }

            if (player != null)
            {
                string defense = Time.time - defenseTime < ComboResetSeconds ? $"    {lastDefense}" : "";
                GUI.Label(new Rect(x, y, width, 24f), $"状态：{player.CurrentStateName}{defense}", labelStyle);
                y += 24f;
                if (player.ChargeRatio > 0f)
                {
                    DrawBar(new Rect(x, y + 18f, width, 10f), player.ChargeRatio, new Color(1f, 0.8f, 0.2f), "蓄力");
                }
            }

            if (target != null)
            {
                Health health = target.Health;
                var rect = new Rect((Screen.width - 500f) * 0.5f, 40f, 500f, 18f);
                DrawBar(rect, health.Current / health.Max, new Color(1f, 0.25f, 0.4f),
                    $"木桩 {health.Current:0}/{health.Max:0}");

                if (Time.time - lastHitTime < ComboResetSeconds)
                {
                    GUI.Label(new Rect(rect.x, rect.yMax + 4f, 500f, 24f),
                        $"伤害 <b>{lastDamage:0.#}</b>    削格挡 {lastGuardDamage:0.#}    连击 {hitCount} 次，共 {comboDamage:0.#}", labelStyle);
                }

                if (dummy != null && dummy.IsWindingUp)
                {
                    GUI.Label(new Rect(rect.x, rect.yMax + 30f, 500f, 24f),
                        $"木桩出拳前摇 <b>{dummy.WindupRemaining:0.00}s</b>", labelStyle);
                }
            }

            if (targeting != null && targeting.Target != null && view != null)
            {
                Vector3 point = view.WorldToScreenPoint(targeting.Target.AimPoint + Vector3.up * 0.55f);
                if (point.z > 0f)
                {
                    GUI.Label(new Rect(point.x - 8f, Screen.height - point.y - 12f, 24f, 24f), "◆", labelStyle);
                }
            }
        }

        private void DrawBar(Rect rect, float ratio, Color color, string label)
        {
            GUI.Label(new Rect(rect.x, rect.y - 22f, rect.width, 22f), label, labelStyle);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height),
                Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
