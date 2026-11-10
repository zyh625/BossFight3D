using UnityEngine;

namespace BossFight.Character
{
    /// <summary>Animator 参数名集中在这里，避免字符串散落在各处。</summary>
    public static class AnimParams
    {
        public const string SpeedName = "Speed";

        /// <summary>动作类状态（攻击等）的播放速度倍率，用来把动画时长对齐到数据里的时长。</summary>
        public const string ActionSpeedName = "ActionSpeed";

        public const string LocomotionState = "Locomotion";

        public static readonly int Speed = Animator.StringToHash(SpeedName);
        public static readonly int ActionSpeed = Animator.StringToHash(ActionSpeedName);
    }
}
