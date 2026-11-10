using BossFight.Character;
using BossFight.Combat;
using UnityEngine;

namespace BossFight.DevTools
{
    /// <summary>
    /// 占位用的程序化挥剑：角色还是胶囊体、没有骨骼动画时，在判定期间让武器挂点绕身体扫过正前方，
    /// 这样不导入任何动画也能看到挥砍、测试命中判定。
    /// 一旦换上带 Humanoid 骨骼的模型，武器跟随手骨，这个组件会自动停用。
    /// </summary>
    public sealed class PlaceholderWeaponSwing : MonoBehaviour
    {
        [SerializeField] private CharacterVisual visual;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private Transform socket;

        [Tooltip("挥砍扫过的角度范围：从身体一侧的 +角度 扫到另一侧的 -角度")]
        [SerializeField] private float sweepHalfAngle = 85f;
        [Tooltip("握把到身体中轴的距离（米）")]
        [SerializeField] private float reach = 0.45f;
        [Tooltip("握把离地高度（米）")]
        [SerializeField] private float height = 1.1f;
        [Tooltip("不挥砍时剑的上扬角度")]
        [SerializeField] private float restPitch = -50f;

        private float duration;
        private float elapsed;
        private bool swinging;
        private float fromAngle;

        private void Start()
        {
            if (visual.HasHumanoid)
            {
                enabled = false;
                return;
            }

            fromAngle = sweepHalfAngle;
            ApplyPose(fromAngle, restPitch);
        }

        private void OnEnable()
        {
            hitbox.Activated += OnActivated;
            hitbox.Deactivated += OnDeactivated;
        }

        private void OnDisable()
        {
            hitbox.Activated -= OnActivated;
            hitbox.Deactivated -= OnDeactivated;
        }

        private void OnActivated(float activeDuration)
        {
            duration = Mathf.Max(activeDuration, 0.01f);
            elapsed = 0f;
            swinging = true;
            ApplyPose(fromAngle, 0f);
        }

        private void OnDeactivated()
        {
            if (!swinging)
            {
                return;
            }

            // 下一刀从另一侧挥回来，连段看起来是左右交替的
            swinging = false;
            fromAngle = -fromAngle;
            ApplyPose(fromAngle, restPitch);
        }

        // 在 Update 里移动挂点，Hitbox 在 LateUpdate 里检测，保证检测到的是这一帧的位置
        private void Update()
        {
            if (!swinging)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyPose(Mathf.Lerp(fromAngle, -fromAngle, t), 0f);
        }

        private void ApplyPose(float yaw, float pitch)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            socket.localPosition = yawRotation * new Vector3(0f, 0f, reach) + Vector3.up * height;
            socket.localRotation = yawRotation * Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
