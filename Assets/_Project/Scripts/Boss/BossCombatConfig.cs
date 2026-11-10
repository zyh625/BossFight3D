using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    [CreateAssetMenu(menuName = "BossFight/Boss Combat Config", fileName = "BossCombatConfig")]
    public sealed class BossCombatConfig : ScriptableObject
    {
        [SerializeField] private AttackData[] combo;
        [SerializeField, Min(0f)] private float attackDistance = 3.2f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float aimLockLead = 0.15f;
        [SerializeField, Min(0f)] private float comboCooldown = 1.5f;
        [SerializeField, Min(0f)] private float hitStun = 0.25f;
        [SerializeField, Min(0f)] private float brokenDuration = 3.5f;
        [SerializeField, Range(0f, 1f)] private float guardedDamageScale = 0.2f;
        [SerializeField, Min(1f)] private float brokenDamageScale = 1.5f;
        [SerializeField, Min(1f)] private float brokenHeavyDamageScale = 2.5f;

        public int ComboLength => combo != null ? combo.Length : 0;
        public AttackData GetStrike(int index) => combo[index];
        public float AttackDistance => attackDistance;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSpeed => turnSpeed;
        public float AimLockLead => aimLockLead;
        public float ComboCooldown => comboCooldown;
        public float HitStun => hitStun;
        public float BrokenDuration => brokenDuration;
        public float GuardedDamageScale => guardedDamageScale;
        public float BrokenDamageScale => brokenDamageScale;
        public float BrokenHeavyDamageScale => brokenHeavyDamageScale;
    }
}
