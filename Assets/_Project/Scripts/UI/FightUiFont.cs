using UnityEngine;
using UnityEngine.UI;

namespace BossFight.UI
{
    public sealed class FightUiFont : MonoBehaviour
    {
        private static readonly string[] Fonts =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "PingFang SC", "Noto Sans CJK SC", "Arial"
        };

        private Font font;

        private void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(Fonts, 22);
            if (font == null)
            {
                return;
            }
            foreach (Text label in GetComponentsInChildren<Text>(true))
            {
                label.font = font;
            }
        }

        private void OnDestroy()
        {
            if (font != null)
            {
                Destroy(font);
            }
        }
    }
}
