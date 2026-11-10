namespace BossFight.Boss
{
    /// <summary>M4 只有三连斩，决策只筛选距离和冷却。</summary>
    internal sealed class BossBrain
    {
        private readonly BossCombatConfig config;
        private double comboReadyAt;

        public BossBrain(BossCombatConfig config)
        {
            this.config = config;
        }

        public bool CanStartCombo(float distance, double actionTime)
        {
            return config.ComboLength > 0 && distance <= config.AttackDistance && actionTime >= comboReadyAt;
        }

        public void FinishCombo(double actionTime)
        {
            comboReadyAt = actionTime + config.ComboCooldown;
        }
    }
}
