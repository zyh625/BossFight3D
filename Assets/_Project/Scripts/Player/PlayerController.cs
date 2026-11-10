using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 玩家的总入口：每帧把输入交给当前能力执行。
    /// M1 只有移动，所以直接调用 PlayerMovement；M2 起这里换成状态机，由状态决定调用哪些能力。
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;

        [Tooltip("相机跟随/注视的点（胸口高度的空物体）")]
        [SerializeField] private Transform cameraTarget;

        public Transform CameraTarget => cameraTarget;

        private void Update()
        {
            movement.Tick(input.Move, input.SprintHeld, Time.deltaTime);
        }
    }
}
