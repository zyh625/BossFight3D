using BossFight.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace BossFight.CameraRig
{
    /// <summary>
    /// 用鼠标位移驱动 Cinemachine 轨道相机（CinemachineOrbitalFollow）的水平/垂直轴。
    /// 鼠标位移本身就是"这一帧移动了多少"，所以不乘 deltaTime：
    /// 视角灵敏度与帧率无关，也不受慢动作（Time.timeScale）影响。
    /// </summary>
    [DefaultExecutionOrder(30)]
    public sealed class OrbitCameraInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CinemachineOrbitalFollow orbital;
        [SerializeField] private LockOnTargeting targeting;
        [SerializeField] private Transform lockAim;

        [Tooltip("每像素鼠标位移转动的角度（度）")]
        [SerializeField] private Vector2 sensitivity = new Vector2(0.12f, 0.08f);
        [SerializeField] private bool invertY;
        [SerializeField, Min(1f)] private float lockYawSpeed = 540f;
        [SerializeField, Range(0f, 1f)] private float lockFramingWeight = 0.42f;
        [SerializeField, Min(1f)] private float framingSpeed = 5f;
        private float framingWeight;
        private Vector3 lastAimPoint;

        private void Update()
        {
            var target = targeting != null ? targeting.Target : null;
            Transform followTarget = orbital.FollowTarget;
            if (lockAim != null && followTarget != null)
            {
                float desiredWeight = target != null ? lockFramingWeight : 0f;
                framingWeight = Mathf.MoveTowards(framingWeight, desiredWeight, framingSpeed * Time.unscaledDeltaTime);
                Vector3 playerPoint = followTarget.position;
                if (target != null)
                {
                    lastAimPoint = target.AimPoint;
                    Vector3 direction = lastAimPoint - playerPoint;
                    float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    float nextYaw = Mathf.MoveTowardsAngle(orbital.HorizontalAxis.Value, yaw, lockYawSpeed * Time.unscaledDeltaTime);
                    orbital.HorizontalAxis.Value = orbital.HorizontalAxis.ClampValue(nextYaw);
                }

                lockAim.position = Vector3.Lerp(playerPoint, lastAimPoint, framingWeight);
            }

            if (target != null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 look = input.Look;
            float verticalSign = invertY ? 1f : -1f;

            orbital.HorizontalAxis.Value =
                orbital.HorizontalAxis.ClampValue(orbital.HorizontalAxis.Value + look.x * sensitivity.x);
            orbital.VerticalAxis.Value =
                orbital.VerticalAxis.ClampValue(orbital.VerticalAxis.Value + look.y * sensitivity.y * verticalSign);
        }
    }
}
