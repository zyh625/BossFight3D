using UnityEngine;

namespace BossFight.Character
{
    /// <summary>
    /// 把 Motor 的实际水平速度（m/s）写进 Animator 的 Speed 参数，驱动 待机/走/跑 混合树。
    /// 用实际速度而不是输入：顶着墙跑时角色会回到待机，而不是原地踏步。
    /// </summary>
    public sealed class LocomotionAnimDriver : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private CharacterVisual visual;

        [Tooltip("Speed 参数的平滑时间（秒），越大过渡越柔和")]
        [SerializeField] private float dampTime = 0.1f;

        private void Update()
        {
            visual.SetFloat(AnimParams.Speed, motor.PlanarSpeed, dampTime, Time.deltaTime);
        }
    }
}
