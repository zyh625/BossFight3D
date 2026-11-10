namespace BossFight.Player
{
    /// <summary>可以被缓冲的玩家指令。</summary>
    public enum PlayerCommand
    {
        None,
        /// <summary>左键（没有按住右键时）。</summary>
        LightAttack,
        /// <summary>按住右键的同时按左键。</summary>
        VariedAttack,
        Roll,
        Jump
    }

    /// <summary>
    /// 输入缓冲：记住最近一次指令一小段时间。
    /// 玩家在上一个动作还没结束时提前按键，等动作允许衔接的那一刻指令仍然有效，
    /// 连招就不需要"卡着帧"按。只保留最新的一条指令。
    /// </summary>
    public sealed class InputBuffer
    {
        private readonly float window;
        private PlayerCommand command;
        private float remaining;

        public InputBuffer(float windowSeconds)
        {
            window = windowSeconds;
        }

        public void Push(PlayerCommand newCommand)
        {
            command = newCommand;
            remaining = window;
        }

        public void Clear()
        {
            command = PlayerCommand.None;
        }

        public void Tick(float dt)
        {
            if (command == PlayerCommand.None)
            {
                return;
            }

            remaining -= dt;
            if (remaining <= 0f)
            {
                command = PlayerCommand.None;
            }
        }

        /// <summary>缓冲里是这条指令就取走它并返回 true。</summary>
        public bool TryConsume(PlayerCommand expected)
        {
            if (command != expected)
            {
                return false;
            }

            command = PlayerCommand.None;
            return true;
        }
    }
}
