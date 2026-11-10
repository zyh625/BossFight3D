namespace BossFight.Core
{
    /// <summary>状态机里的一个状态。玩家和 Boss 的每个动作（移动、一次攻击、一个招式）都是一个状态。</summary>
    public interface IState
    {
        void Enter();

        /// <param name="dt">这一帧的时间。由状态机的持有者传入，所以顿帧时可以传 0。</param>
        void Tick(float dt);

        void Exit();
    }
}
