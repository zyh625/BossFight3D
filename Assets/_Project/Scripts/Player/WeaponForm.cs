namespace BossFight.Player
{
    /// <summary>
    /// 玩家的武器形态，按 F 循环切换。三种形态共用同一套输入和状态机：
    ///   左键 = 这个形态的轻攻击，按住右键 = 蓄力，松开 = 这个形态的蓄力招式。
    /// 形态决定的只是"用哪份攻击数据、哪个判定体"，以及飞镖把近战判定换成了发射道具。
    /// </summary>
    public enum WeaponForm
    {
        /// <summary>剑：三连斩、跃起重击、切手技。</summary>
        Sword,
        /// <summary>能量飞镖：远程，无限使用；蓄力后更快更痛。没有识破。</summary>
        Dart,
        /// <summary>拳套：左右拳快速连打；蓄力后向前瞬移冲拳。识破窗口更宽，但成功时仍会受到少量伤害。</summary>
        Fist
    }
}
