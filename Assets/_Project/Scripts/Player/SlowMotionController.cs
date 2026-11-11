using BossFight.Combat;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 慢动作：按 R 开启，再按一次、技能条耗尽、或玩家失去控制时结束。
    ///
    /// 实现方式是直接修改 Time.timeScale，所以**所有东西都变慢，包括玩家自己**。
    /// 它的作用不是让玩家变强，而是给玩家更多现实时间去看清 Boss 的出手、找准识破和闪避的时机：
    /// 0.2 秒的识破窗口在 0.4 倍速下相当于现实中的 0.5 秒。
    ///
    /// 时间的两套计量：
    ///   游戏内的一切（招式时序、冷却、移动、无敌帧）用缩放后的时间 → 自动变慢，不需要改任何玩法代码
    ///   技能条的消耗、视角转动、UI、顿帧的时长用真实时间            → 不受慢动作影响
    /// </summary>
    public sealed class SlowMotionController : MonoBehaviour
    {
        // Unity 默认的物理步长。慢动作时按比例缩小，物理和 FixedUpdate 的"现实频率"保持不变，画面才不会卡
        private const float DefaultFixedDeltaTime = 0.02f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerController player;
        [SerializeField] private SkillGauge gauge;
        [SerializeField] private Health health;

        [Tooltip("慢动作期间的时间流速（1 = 正常）")]
        [SerializeField, Range(0.05f, 1f)] private float slowTimeScale = 0.4f;
        [Tooltip("每秒（现实时间）消耗的技能条")]
        [SerializeField, Min(0f)] private float drainPerSecond = 25f;
        [Tooltip("技能条至少有这么多才能开启，避免开了一瞬间就结束")]
        [SerializeField, Min(0f)] private float minimumToStart = 10f;

        public bool IsActive { get; private set; }

        private void Update()
        {
            if (!IsActive)
            {
                if (input.SlowMotionPressedThisFrame && CanStart())
                {
                    Begin();
                }
                return;
            }

            // 用真实时间扣技能条：否则慢动作本身会让技能条也掉得更慢，持续时间就和流速设置纠缠在一起了
            bool hasFuel = gauge.Drain(drainPerSecond * Time.unscaledDeltaTime);
            if (input.SlowMotionPressedThisFrame || !hasFuel || !player.enabled || health.IsDead)
            {
                End();
            }
        }

        // 场景重载、玩家被禁用时一定要把时间流速还原——timeScale 是全局状态，不会随场景销毁而复位
        private void OnDisable()
        {
            End();
        }

        private bool CanStart()
        {
            return player.enabled && !health.IsDead && gauge.Current >= minimumToStart;
        }

        private void Begin()
        {
            IsActive = true;
            gauge.IsDraining = true;
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = DefaultFixedDeltaTime * slowTimeScale;
        }

        private void End()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            gauge.IsDraining = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
        }
    }
}
