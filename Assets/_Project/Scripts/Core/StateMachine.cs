using System;

namespace BossFight.Core
{
    /// <summary>
    /// 最小的有限状态机：同一时刻只有一个当前状态，切换时保证先 Exit 旧状态再 Enter 新状态。
    /// 它不是 MonoBehaviour，由持有者（PlayerController / BossController）在 Update 里驱动。
    /// </summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        /// <summary>状态切换后触发（旧状态, 新状态），用于调试显示和日志。</summary>
        public event Action<IState, IState> Changed;

        public void ChangeState(IState next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            IState previous = Current;
            previous?.Exit();
            Current = next;
            next.Enter();
            Changed?.Invoke(previous, next);
        }

        public void Tick(float dt)
        {
            Current?.Tick(dt);
        }
    }
}
