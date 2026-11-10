using UnityEngine;

namespace BossFight.Player
{
    /// <summary>玩家移动的调参数据。运行中修改会立刻生效，退出 Play 后保留。</summary>
    [CreateAssetMenu(menuName = "BossFight/Player Movement Config", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("速度 (m/s)")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6.5f;

        [Header("加减速 (m/s²)")]
        [Tooltip("起步和变向时速度变化的快慢")]
        [SerializeField] private float acceleration = 40f;
        [Tooltip("松开按键后停下的快慢")]
        [SerializeField] private float deceleration = 50f;

        [Header("转向")]
        [Tooltip("角色转向移动方向的角速度（度/秒）")]
        [SerializeField] private float turnSpeed = 900f;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float TurnSpeed => turnSpeed;
    }
}
