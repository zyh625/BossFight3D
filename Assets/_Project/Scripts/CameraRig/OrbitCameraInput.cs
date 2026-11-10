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
    public sealed class OrbitCameraInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CinemachineOrbitalFollow orbital;

        [Tooltip("每像素鼠标位移转动的角度（度）")]
        [SerializeField] private Vector2 sensitivity = new Vector2(0.12f, 0.08f);
        [SerializeField] private bool invertY;

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
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
