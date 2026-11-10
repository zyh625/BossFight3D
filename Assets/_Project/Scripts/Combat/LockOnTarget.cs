using UnityEngine;

namespace BossFight.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class LockOnTarget : MonoBehaviour
    {
        [SerializeField] private Vector3 localAimPoint = new Vector3(0f, 1.4f, 0f);
        private Health health;

        public bool IsAvailable => isActiveAndEnabled && !health.IsDead;
        public Vector3 AimPoint => transform.TransformPoint(localAimPoint);

        private void Awake()
        {
            health = GetComponent<Health>();
        }
    }
}
