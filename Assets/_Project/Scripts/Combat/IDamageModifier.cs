namespace BossFight.Combat
{
    /// <summary>
    /// 伤害结算管线上的一环。玩家防御、Boss 格挡、破防增伤等机制各实现一个修饰器，挂在角色上即可生效，
    /// 彼此不需要知道对方存在。
    /// </summary>
    public interface IDamageModifier
    {
        /// <summary>执行顺序，小的先执行。约定：全局无敌 0 → 玩家防御 100 → Boss 格挡 200 → 增伤 300。</summary>
        int Order { get; }

        void Modify(DamageContext context);
    }
}
