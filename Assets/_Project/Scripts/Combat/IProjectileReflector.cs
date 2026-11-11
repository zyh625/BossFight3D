namespace BossFight.Combat
{
    /// <summary>
    /// 能把飞行道具弹回去的角色实现它（Boss 的旋风斩）。
    /// 飞镖只认这个接口，不认识 Boss——以后任何角色想反弹道具，实现它就行。
    /// </summary>
    public interface IProjectileReflector
    {
        bool ReflectsProjectiles { get; }
    }
}
