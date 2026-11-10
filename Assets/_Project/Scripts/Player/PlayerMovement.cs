using BossFight.Character;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>按相机或锁定目标的方向计算位移，由当前动作状态驱动。</summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private CharacterMotor motor;

        private Transform cameraTransform;
        private LockOnTargeting targeting;
        private Vector3 velocity;

        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            targeting = GetComponent<LockOnTargeting>();
        }

        private void Start()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                cameraTransform = main.transform;
            }
        }

        public void ResetVelocity()
        {
            velocity = Vector3.zero;
            IsSprinting = false;
        }

        /// <param name="speedMultiplier">速度倍率，蓄力时小于 1。</param>
        public void Tick(Vector2 moveInput, bool wantsSprint, float dt, float speedMultiplier = 1f)
        {
            Vector3 direction = ToWorldDirection(moveInput);
            bool hasInput = direction.sqrMagnitude > 0.0001f;
            IsSprinting = hasInput && wantsSprint;

            float targetSpeed = (IsSprinting ? config.RunSpeed : config.WalkSpeed) * speedMultiplier;
            Vector3 targetVelocity = hasInput ? direction * targetSpeed : Vector3.zero;
            float rate = hasInput ? config.Acceleration : config.Deceleration;
            velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * dt);
            motor.Move(velocity, dt);

            if (TryGetLockDirection(out Vector3 facing))
            {
                motor.Face(facing, config.TurnSpeed, dt);
            }
            else if (hasInput)
            {
                motor.Face(direction, config.TurnSpeed, dt);
            }
        }

        public Vector3 ToWorldDirection(Vector2 input)
        {
            if (!TryGetLockDirection(out Vector3 forward))
            {
                forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                forward.y = 0f;
                forward.Normalize();
            }

            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            return forward * input.y + right * input.x;
        }

        public bool TryGetLockDirection(out Vector3 direction)
        {
            var target = targeting != null ? targeting.Target : null;
            direction = target != null ? target.transform.position - transform.position : Vector3.zero;
            direction.y = 0f;
            direction.Normalize();
            return direction.sqrMagnitude > 0f;
        }
    }
}
