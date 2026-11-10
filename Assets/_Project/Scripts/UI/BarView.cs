using UnityEngine;
using UnityEngine.UI;

namespace BossFight.UI
{
    public sealed class BarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Text label;
        [SerializeField] private string caption;

        private int shownValue = int.MinValue;
        private int shownMax = int.MinValue;

        public void SetValue(float value, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            fill.anchorMax = new Vector2(ratio, 1f);
            int rounded = Mathf.CeilToInt(value);
            int roundedMax = Mathf.CeilToInt(max);
            if (rounded == shownValue && roundedMax == shownMax)
            {
                return;
            }

            shownValue = rounded;
            shownMax = roundedMax;
            label.text = $"{caption}  {rounded} / {roundedMax}";
        }
    }
}
