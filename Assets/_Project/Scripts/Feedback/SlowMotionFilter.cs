using UnityEngine;
using UnityEngine.Rendering;

namespace BossFight.Feedback
{
    /// <summary>
    /// 慢动作的画面滤镜：控制一个后处理 Volume 的权重（降饱和、偏冷色、加重暗角）。
    /// 它不认识慢动作系统，只观察全局时间流速——流速低于正常就淡入，恢复就淡出。
    /// 所以以后任何原因造成的慢动作（比如处决演出）都会自动带上滤镜。
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class SlowMotionFilter : MonoBehaviour
    {
        [Tooltip("滤镜淡入淡出的速度（权重/秒，按真实时间）")]
        [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;
        [Tooltip("时间流速低于这个值视为慢动作")]
        [SerializeField, Range(0f, 1f)] private float threshold = 0.95f;

        private Volume volume;

        private void Awake()
        {
            volume = GetComponent<Volume>();
            volume.weight = 0f;
        }

        private void Update()
        {
            // 暂停（timeScale = 0）不算慢动作
            float scale = Time.timeScale;
            float target = scale > 0f && scale < threshold ? 1f : 0f;

            // 用真实时间做过渡：滤镜的淡入不应该被它自己所表现的慢动作拖慢
            volume.weight = Mathf.MoveTowards(volume.weight, target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
