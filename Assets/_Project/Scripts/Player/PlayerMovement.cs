using BossFight.Character;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 玩家的"自由移动"能力：把移动输入换算成相对相机的方向，带加减速地驱动 Motor。
    /// 它不自己读输入、也不自己 Update，由上层（现在是 PlayerController，M2 起是移动状态）调用 Tick，
    /// 这样攻击、翻滚等状态可以决定此刻允不允许移动。
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private CharacterMotor motor;

        private Transform cameraTransform;
        private Vector3 velocity;

        /// <summary>这一帧是否真的在奔跑（有方向输入且按住奔跑）。M2 的体力消耗会读它。</summary>
        public bool IsSprinting { get; private set; }

        private void Start()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                cameraTransform = main.transform;
            }
        }

        public void Tick(Vector2 moveInput, bool wantsSprint, float dt)
        {
            Vector3 direction = ToWorldDirection(moveInput);
            bool hasInput = direction.sqrMagnitude > 0.0001f;
            IsSprinting = hasInput && wantsSprint;

            float targetSpeed = IsSprinting ? config.RunSpeed : config.WalkSpeed;
            Vector3 targetVelocity = hasInput ? direction * targetSpeed : Vector3.zero;
            float rate = hasInput ? config.Acceleration : config.Deceleration;

            velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * dt);
            motor.Move(velocity, dt);

            if (hasInput)
            {
                motor.Face(direction, config.TurnSpeed, dt);
            }
        }

        /// <summary>把 WASD 输入投影到相机的水平朝向上：按 W 永远是"朝屏幕里走"。</summary>
        private Vector3 ToWorldDirection(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            return forward * input.y + right * input.x;
        }
    }
}
