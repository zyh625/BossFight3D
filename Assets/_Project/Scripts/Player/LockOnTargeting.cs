using BossFight.Combat;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>选取屏幕内、视线无遮挡的目标，供移动和相机共享。</summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Health), typeof(PlayerInputReader))]
    public sealed class LockOnTargeting : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float acquireDistance = 14f;
        [SerializeField, Min(0f)] private float releaseDistance = 18f;
        [SerializeField] private LayerMask occlusionLayers;

        private PlayerInputReader input;
        private Health health;
        private Camera view;
        private LockOnTarget target;

        public LockOnTarget Target => target != null && target.IsAvailable ? target : null;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
        }

        private void Start()
        {
            view = Camera.main;
        }

        private void Update()
        {
            if (health.IsDead)
            {
                target = null;
                return;
            }

            if (target != null && (!target.IsAvailable || !IsWithin(target, releaseDistance)))
            {
                target = null;
            }

            if (input.LockOnPressedThisFrame)
            {
                target = target == null ? FindTarget() : null;
            }
        }

        private void OnDisable()
        {
            target = null;
        }

        private LockOnTarget FindTarget()
        {
            if (view == null)
            {
                return null;
            }

            LockOnTarget best = null;
            float bestScore = float.PositiveInfinity;
            foreach (LockOnTarget candidate in FindObjectsByType<LockOnTarget>())
            {
                if (!candidate.IsAvailable || candidate.gameObject == gameObject || !IsWithin(candidate, acquireDistance))
                {
                    continue;
                }

                Vector3 point = view.WorldToViewportPoint(candidate.AimPoint);
                if (point.z <= 0f || point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f)
                {
                    continue;
                }

                float score = ((Vector2)point - new Vector2(0.5f, 0.5f)).sqrMagnitude;
                if (score < bestScore && !Physics.Linecast(view.transform.position, candidate.AimPoint,
                        occlusionLayers, QueryTriggerInteraction.Ignore))
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private bool IsWithin(LockOnTarget candidate, float distance)
        {
            return (candidate.transform.position - transform.position).sqrMagnitude <= distance * distance;
        }
    }
}
