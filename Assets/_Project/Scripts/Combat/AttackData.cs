using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 一次攻击的全部数据：时序（前摇/判定/后摇）、数值、动画。
    /// 时序由代码计时器驱动，动画只负责外观，所以换动画不会改变判定时间。
    /// </summary>
    [CreateAssetMenu(menuName = "BossFight/Attack Data", fileName = "AttackData")]
    public sealed class AttackData : ScriptableObject
    {
        [Header("时序（秒）")]
        [Tooltip("前摇：从出手到判定开始")]
        [SerializeField] private float windup = 0.2f;
        [Tooltip("判定：Hitbox 开启的时长")]
        [SerializeField] private float active = 0.1f;
        [Tooltip("后摇：判定结束到动作结束")]
        [SerializeField] private float recovery = 0.3f;
        [Tooltip("后摇进行到这个比例之后，才可以接下一个动作（0 = 判定一结束就能接，1 = 必须等后摇结束）")]
        [Range(0f, 1f)]
        [SerializeField] private float cancelPoint = 0.5f;

        [Header("数值")]
        [SerializeField] private float damage = 10f;
        [Tooltip("对格挡条的伤害")]
        [SerializeField] private float guardDamage = 10f;
        [SerializeField] private float staminaCost = 10f;

        [Header("位移")]
        [Tooltip("前摇 + 判定期间向前踏出的距离（米）")]
        [SerializeField] private float lungeDistance = 0.6f;

        [Header("动画")]
        [Tooltip("Animator 中的状态名。动画会被调整播放速度，使其总时长等于 前摇+判定+后摇")]
        [SerializeField] private string animationState;
        [SerializeField] private float crossFade = 0.05f;

        public float Windup => windup;
        public float Active => active;
        public float Recovery => recovery;
        public float Duration => windup + active + recovery;

        /// <summary>从动作开始算，到这个时间点之后可以接下一个动作。</summary>
        public float CancelTime => windup + active + recovery * cancelPoint;

        public float Damage => damage;
        public float GuardDamage => guardDamage;
        public float StaminaCost => staminaCost;
        public float LungeDistance => lungeDistance;
        public string AnimationState => animationState;
        public float CrossFade => crossFade;

#if UNITY_EDITOR
        /// <summary>仅供 Editor 搭建脚本创建默认数据时使用。</summary>
        public void EditorInit(
            string animation, float windupTime, float activeTime, float recoveryTime,
            float damageAmount, float guard, float stamina, float lunge)
        {
            animationState = animation;
            windup = windupTime;
            active = activeTime;
            recovery = recoveryTime;
            damage = damageAmount;
            guardDamage = guard;
            staminaCost = stamina;
            lungeDistance = lunge;
        }
#endif
    }
}
