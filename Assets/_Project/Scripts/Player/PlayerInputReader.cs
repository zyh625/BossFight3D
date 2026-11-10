using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight.Player
{
    /// <summary>
    /// 输入的唯一入口：把 Input System 的 Action 翻译成玩法层能直接读的值。
    /// 其他脚本不引用 InputSystem，以后改键位、加手柄都只动这里和 .inputactions 资源。
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string MapName = "Player";

        [SerializeField] private InputActionAsset actions;

        private InputActionMap map;
        private InputAction move;
        private InputAction look;
        private InputAction sprint;

        /// <summary>移动输入，长度 ≤ 1。</summary>
        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);

        /// <summary>视角输入（鼠标为本帧像素位移）。</summary>
        public Vector2 Look => look.ReadValue<Vector2>();

        public bool SprintHeld => sprint.IsPressed();

        private void Awake()
        {
            map = actions.FindActionMap(MapName, throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            look = map.FindAction("Look", throwIfNotFound: true);
            sprint = map.FindAction("Sprint", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            map.Enable();
        }

        private void OnDisable()
        {
            map.Disable();
        }
    }
}
