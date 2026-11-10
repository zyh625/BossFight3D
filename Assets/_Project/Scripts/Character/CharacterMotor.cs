using UnityEngine;

namespace BossFight.Character
{
    /// <summary>
    /// 角色位移的唯一出口：封装 CharacterController，负责重力、贴地和转向。
    /// 上层（玩家状态、Boss 状态）只告诉它"这一帧想以什么水平速度移动"，不直接碰 CharacterController。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [Tooltip("重力加速度（m/s²，负值）")]
        [SerializeField] private float gravity = -25f;

        [Tooltip("站在地面时保持的向下速度，保证 isGrounded 稳定、下坡不离地")]
        [SerializeField] private float groundedStickVelocity = -2f;

        private CharacterController controller;
        private float verticalVelocity;
        private Vector3 pushVelocity;
        private float pushRemaining;

        /// <summary>上一次 Move 之后实际发生的水平速度（撞墙时会小于期望速度）。</summary>
        public Vector3 PlanarVelocity { get; private set; }

        public float PlanarSpeed => PlanarVelocity.magnitude;
        public bool IsGrounded => controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // 小帧步和缓慢蓄力移动也需要应用到碰撞体。
            controller.minMoveDistance = 0f;
        }

        /// <summary>
        /// 以给定的水平速度移动一帧。dt 由调用方传入，之后做顿帧（局部时间缩放）只需要改传入的 dt。
        /// </summary>
        public void Move(Vector3 planarVelocity, float dt)
        {
            planarVelocity.y = 0f;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            verticalVelocity += gravity * dt;

            float pushStep = Mathf.Min(dt, pushRemaining);
            pushRemaining -= pushStep;
            Vector3 motion = (planarVelocity + Vector3.up * verticalVelocity) * dt + pushVelocity * pushStep;
            controller.Move(motion);

            Vector3 actual = controller.velocity;
            actual.y = 0f;
            PlanarVelocity = actual;
        }

        public void Push(Vector3 direction, float distance, float duration)
        {
            direction.y = 0f;
            pushRemaining = duration;
            pushVelocity = duration > 0f ? direction.normalized * (distance / duration) : Vector3.zero;
        }

        /// <summary>以固定角速度把角色转向某个水平方向。</summary>
        public void Face(Vector3 direction, float degreesPerSecond, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * dt);
        }

        /// <summary>瞬间移动。CharacterController 启用时直接改 position 会被它覆盖，所以要先关再开。</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
            pushRemaining = 0f;
            PlanarVelocity = Vector3.zero;
        }
    }
}
