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

        private InputActionAsset runtimeActions;
        private InputActionMap map;
        private InputAction move;
        private InputAction look;
        private InputAction sprint;
        private InputAction roll;
        private InputAction jump;
        private InputAction attack;
        private InputAction charge;
        private InputAction lockOn;
        private InputAction slowMotion;
        private InputAction switchWeapon;

        /// <summary>移动输入，长度 ≤ 1。</summary>
        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);

        /// <summary>视角输入（鼠标为本帧像素位移）。</summary>
        public Vector2 Look => look.ReadValue<Vector2>();

        public bool SprintHeld => sprint.IsPressed();
        public bool RollPressedThisFrame => roll.WasPressedThisFrame();
        public bool JumpPressedThisFrame => jump.WasPressedThisFrame();

        /// <summary>左键：这一帧刚按下。</summary>
        public bool AttackPressedThisFrame => attack.WasPressedThisFrame();
        public bool AttackHeld => attack.IsPressed();

        /// <summary>右键：按住蓄力。</summary>
        public bool ChargeHeld => charge.IsPressed();
        public bool ChargePressedThisFrame => charge.WasPressedThisFrame();
        public bool LockOnPressedThisFrame => lockOn.WasPressedThisFrame();
        public bool SlowMotionPressedThisFrame => slowMotion.WasPressedThisFrame();
        public bool SwitchWeaponPressedThisFrame => switchWeapon.WasPressedThisFrame();

        private void Awake()
        {
            // 场景重载时，新玩家拥有独立的 Action 状态和设备绑定。
            runtimeActions = Instantiate(actions);
            map = runtimeActions.FindActionMap(MapName, throwIfNotFound: true);
            move = map.FindAction("Move", throwIfNotFound: true);
            look = map.FindAction("Look", throwIfNotFound: true);
            sprint = map.FindAction("Sprint", throwIfNotFound: true);
            roll = map.FindAction("Roll", throwIfNotFound: true);
            jump = map.FindAction("Jump", throwIfNotFound: true);
            attack = map.FindAction("Attack", throwIfNotFound: true);
            charge = map.FindAction("Charge", throwIfNotFound: true);
            lockOn = map.FindAction("LockOn", throwIfNotFound: true);
            slowMotion = map.FindAction("SlowMotion", throwIfNotFound: true);
            switchWeapon = map.FindAction("SwitchWeapon", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            map.Enable();
        }

        private void OnDisable()
        {
            map.Disable();
        }

        private void OnDestroy()
        {
            Destroy(runtimeActions);
        }
    }
}
