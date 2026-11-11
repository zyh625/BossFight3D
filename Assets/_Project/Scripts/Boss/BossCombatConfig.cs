using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    [CreateAssetMenu(menuName = "BossFight/Boss Combat Config", fileName = "BossCombatConfig")]
    public sealed class BossCombatConfig : ScriptableObject
    {
        [SerializeField] private AttackData[] combo;
        [Tooltip("长剑形态的四连击：三快一慢")]
        [SerializeField] private AttackData[] longswordCombo;
        [SerializeField, Min(0f)] private float attackDistance = 3.2f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float aimLockLead = 0.15f;
        [SerializeField, Min(0f)] private float comboCooldown = 1.5f;
        [SerializeField, Min(0f)] private float comboWeight = 1f;
        [SerializeField, Min(0f)] private float hitStun = 0.25f;
        [SerializeField, Min(0f)] private float brokenDuration = 3.5f;
        [SerializeField, Range(0f, 1f)] private float guardedDamageScale = 0.2f;
        [SerializeField, Min(1f)] private float brokenDamageScale = 1.5f;
        [SerializeField, Min(1f)] private float brokenHeavyDamageScale = 2.5f;
        [SerializeField] private BossSkillSettings skills = new BossSkillSettings();

        public int ComboLength => combo != null ? combo.Length : 0;
        public AttackData GetStrike(int index) => combo[index];
        public bool HasLongswordCombo => longswordCombo != null && longswordCombo.Length > 0;
        public int GetComboLength(bool longsword) => longsword && HasLongswordCombo ? longswordCombo.Length : ComboLength;
        public AttackData GetStrike(int index, bool longsword) => longsword && HasLongswordCombo ? longswordCombo[index] : combo[index];
        public float AttackDistance => attackDistance;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSpeed => turnSpeed;
        public float AimLockLead => aimLockLead;
        public float HitStun => hitStun;
        public float BrokenDuration => brokenDuration;
        public float GuardedDamageScale => guardedDamageScale;
        public float BrokenDamageScale => brokenDamageScale;
        public float BrokenHeavyDamageScale => brokenHeavyDamageScale;
        public BossSkillSettings Skills => skills;

        public BossMoveRule GetRule(BossMove move)
        {
            return move switch
            {
                BossMove.Combo => new BossMoveRule(0f, attackDistance, comboCooldown, comboWeight),
                BossMove.BlinkSlash => skills.blinkSlash.rule,
                BossMove.SwordWave => skills.swordWave.rule,
                BossMove.Retreat => skills.retreat.rule,
                BossMove.Whirlwind => skills.whirlwind.rule,
                _ => default
            };
        }
    }
}
