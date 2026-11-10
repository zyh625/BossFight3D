using UnityEngine;

namespace BossFight.Character
{
    /// <summary>Animator 参数名集中在这里，避免字符串散落在各处。</summary>
    public static class AnimParams
    {
        public const string SpeedName = "Speed";

        public static readonly int Speed = Animator.StringToHash(SpeedName);
    }
}
