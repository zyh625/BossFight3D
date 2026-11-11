using BossFight.Boss;
using BossFight.CameraRig;
using BossFight.Combat;
using BossFight.Game;
using BossFight.Player;
using BossFight.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BossFight.EditorTools
{
    internal static class FightUiBuilder
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color PanelColor = new Color(0.024f, 0.046f, 0.07f, 0.94f);
        private static readonly Color TrackColor = new Color(0.12f, 0.16f, 0.21f, 1f);
        private static readonly Color PrimaryText = new Color(0.91f, 0.96f, 1f);
        private static readonly Color SecondaryText = new Color(0.63f, 0.74f, 0.83f);
        private static readonly Color Accent = new Color(0.35f, 0.91f, 0.85f);
        private static readonly Color HealthColor = new Color(0.91f, 0.28f, 0.38f);

        public static void Build(PlayerController player, BossController boss, OrbitCameraInput orbit)
        {
            var finisher = orbit.gameObject.AddComponent<FinisherCamera>();
            EditorWiring.SetRef(finisher, "boss", boss.GetComponent<DamageReceiver>());
            EditorWiring.SetRef(finisher, "player", player.gameObject);

            var director = new GameObject("Fight Director").AddComponent<FightDirector>();
            EditorWiring.SetRef(director, "player", player);
            EditorWiring.SetRef(director, "boss", boss);
            EditorWiring.SetRef(director, "cursorLocker", orbit.GetComponent<CursorLocker>());
            EditorWiring.SetRef(director, "finisher", finisher);

            var root = new GameObject("Fight UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(FightUiFont));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildHud(root, director);
            BuildResult(root, director);
        }

        private static void BuildHud(GameObject root, FightDirector director)
        {
            RectTransform content = Stretch("Combat HUD", root.transform);
            RectTransform boss = Panel("Boss Status", content, TopCenter, new Vector2(0f, -24f), new Vector2(570f, 130f));
            Label("Boss Name", boss, "暗影剑客", 24, 18f, -12f, 265f, 28f);
            Text phase = Label("Phase", boss, "", 16, 402f, -13f, 150f, 26f, TextAnchor.MiddleRight, Accent);
            BarView bossHealth = Bar("Boss Health", boss, "生命", 18f, -43f, 534f, HealthColor);
            BarView bossGuard = Bar("Boss Guard", boss, "格挡", 18f, -74f, 534f, new Color(0.79f, 0.86f, 0.91f));
            Text status = Label("Boss Action", boss, "", 15, 18f, -104f, 534f, 22f, TextAnchor.MiddleCenter, SecondaryText);

            RectTransform player = Panel("Player Status", content, Vector2.zero, new Vector2(24f, 24f), new Vector2(324f, 144f));
            Text weapon = Label("Player Weapon", player, "剑 · F 切换", 17, 16f, -10f, 118f, 26f);
            Text freeStamina = Label("Free Stamina", player, "", 15, 135f, -10f, 173f, 26f, TextAnchor.MiddleRight, Accent);
            BarView playerHealth = Bar("Player Health", player, "生命", 16f, -40f, 292f, HealthColor);
            BarView playerStamina = Bar("Player Stamina", player, "体力", 16f, -71f, 292f, Accent);
            BarView skill = Bar("Skill Gauge", player, "慢动作 [R]", 16f, -102f, 292f, new Color(0.43f, 0.58f, 0.98f));
            BarView charge = Bar("Heavy Charge", player, "重击蓄力", 16f, 39f, 292f, new Color(1f, 0.7f, 0.22f));
            charge.gameObject.SetActive(false);

            RectTransform marker = Place("Lock Marker", content, Vector2.zero, Vector2.zero, new Vector2(11f, 11f));
            marker.pivot = Center;
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Paint(marker, PrimaryText);
            marker.gameObject.SetActive(false);

            // 飞镖形态的准星：屏幕正中的一个小菱形
            RectTransform crosshair = Place("Crosshair", content, Center, Vector2.zero, new Vector2(8f, 8f));
            crosshair.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Paint(crosshair, Accent);
            crosshair.gameObject.SetActive(false);

            var hud = root.AddComponent<FightHud>();
            EditorWiring.SetRef(hud, "fight", director);
            EditorWiring.SetRef(hud, "content", content.gameObject);
            EditorWiring.SetRef(hud, "playerHealth", playerHealth);
            EditorWiring.SetRef(hud, "playerStamina", playerStamina);
            EditorWiring.SetRef(hud, "skillGauge", skill);
            EditorWiring.SetRef(hud, "bossHealth", bossHealth);
            EditorWiring.SetRef(hud, "bossGuard", bossGuard);
            EditorWiring.SetRef(hud, "charge", charge);
            EditorWiring.SetRef(hud, "phase", phase);
            EditorWiring.SetRef(hud, "status", status);
            EditorWiring.SetRef(hud, "freeStamina", freeStamina);
            EditorWiring.SetRef(hud, "lockMarker", marker);
            EditorWiring.SetRef(hud, "weapon", weapon);
            EditorWiring.SetRef(hud, "crosshair", crosshair.gameObject);
        }

        private static void BuildResult(GameObject root, FightDirector director)
        {
            RectTransform overlay = Stretch("Fight Result", root.transform);
            Paint(overlay, new Color(0.012f, 0.02f, 0.032f, 0.82f), true);
            RectTransform card = Panel("Result Card", overlay, Center, Vector2.zero, new Vector2(465f, 250f));
            Label("Header", card, "交战结束", 16, 24f, -25f, 417f, 24f, TextAnchor.MiddleCenter, SecondaryText);
            Text title = Label("Result Title", card, "", 54, 24f, -51f, 417f, 73f, TextAnchor.MiddleCenter);
            Text detail = Label("Result Detail", card, "", 20, 24f, -130f, 417f, 30f, TextAnchor.MiddleCenter, SecondaryText);

            RectTransform buttonRect = Place("Restart", card, TopCenter, new Vector2(0f, -176f), new Vector2(224f, 51f));
            Image background = Paint(buttonRect, Color.white, true);
            var restart = buttonRect.gameObject.AddComponent<Button>();
            restart.targetGraphic = background;
            ColorBlock colors = restart.colors;
            colors.normalColor = Accent;
            colors.highlightedColor = new Color(0.68f, 1f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.24f, 0.68f, 0.63f);
            restart.colors = colors;
            Label("Caption", buttonRect, "再战一局", 20, 0f, 0f, 224f, 51f, TextAnchor.MiddleCenter, PanelColor);

            var eventSystem = new GameObject("Result EventSystem").AddComponent<EventSystem>();
            eventSystem.gameObject.SetActive(false);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            overlay.gameObject.SetActive(false);

            var view = root.AddComponent<FightResultView>();
            EditorWiring.SetRef(view, "fight", director);
            EditorWiring.SetRef(view, "panel", overlay.gameObject);
            EditorWiring.SetRef(view, "title", title);
            EditorWiring.SetRef(view, "detail", detail);
            EditorWiring.SetRef(view, "restart", restart);
            EditorWiring.SetRef(view, "eventSystem", eventSystem);
        }

        private static BarView Bar(string name, Transform parent, string caption, float x, float y, float width, Color color)
        {
            RectTransform row = Place(name, parent, TopLeft, new Vector2(x, y), new Vector2(width, 29f));
            Text label = Label("Reading", row, caption, 14, 0f, 0f, width, 17f, TextAnchor.MiddleLeft, SecondaryText);
            RectTransform track = Place("Track", row, TopLeft, new Vector2(0f, -20f), new Vector2(width, 7f));
            Paint(track, TrackColor);
            RectTransform fill = Stretch("Fill", track);
            Paint(fill, color);
            var bar = row.gameObject.AddComponent<BarView>();
            EditorWiring.SetRef(bar, "label", label);
            EditorWiring.SetRef(bar, "fill", fill);
            EditorWiring.Edit(bar, "caption", p => p.stringValue = caption);
            return bar;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            RectTransform rect = Place(name, parent, anchor, offset, size);
            Paint(rect, PanelColor);
            Paint(Place("Accent", rect, TopLeft, Vector2.zero, new Vector2(size.x, 2f)), Accent);
            return rect;
        }

        private static Text Label(string name, Transform parent, string text, int size, float x, float y, float width, float height,
            TextAnchor align = TextAnchor.MiddleLeft, Color? color = null)
        {
            RectTransform rect = Place(name, parent, TopLeft, new Vector2(x, y), new Vector2(width, height));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.text = text;
            label.alignment = align;
            label.color = color ?? PrimaryText;
            label.raycastTarget = false;
            return label;
        }

        private static Image Paint(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static RectTransform Place(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Place(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one;
            return rect;
        }
    }
}
