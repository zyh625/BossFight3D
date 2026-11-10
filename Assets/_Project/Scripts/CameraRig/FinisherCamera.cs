using BossFight.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace BossFight.CameraRig
{
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class FinisherCamera : MonoBehaviour
    {
        [SerializeField] private DamageReceiver boss;
        [SerializeField] private GameObject player;
        [SerializeField, Range(10f, 70f)] private float closeUpFov = 39f;
        [SerializeField, Min(0.1f)] private float duration = 0.5f;
        [SerializeField, Min(0.01f)] private float blendTime = 0.09f;

        private CinemachineCamera rig;
        private OrbitCameraInput orbit;
        private float normalFov;
        private float startedAt;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            rig = GetComponent<CinemachineCamera>();
            orbit = GetComponent<OrbitCameraInput>();
        }

        private void OnEnable() => boss.Damaged += OnBossDamaged;

        private void OnDisable()
        {
            boss.Damaged -= OnBossDamaged;
            Restore();
        }

        private void OnBossDamaged(DamageResult result)
        {
            if (!result.IsGuardBreakFinisher || result.Info.Attacker != player)
            {
                return;
            }

            if (!IsPlaying)
            {
                normalFov = rig.Lens.FieldOfView;
            }
            startedAt = Time.unscaledTime;
            IsPlaying = true;
            Vector3 focus = boss.TryGetComponent(out LockOnTarget target) ? target.AimPoint : boss.transform.position;
            orbit?.SetFinisherAim(focus);
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }
            float elapsed = Time.unscaledTime - startedAt;
            if (elapsed >= duration)
            {
                Restore();
                return;
            }

            float blend = Mathf.Min(blendTime, duration * 0.5f);
            float amount = Mathf.Min(elapsed, duration - elapsed) / blend;
            rig.Lens.FieldOfView = Mathf.Lerp(normalFov, closeUpFov, Mathf.SmoothStep(0f, 1f, amount));
        }

        private void Restore()
        {
            if (IsPlaying)
            {
                rig.Lens.FieldOfView = normalFov;
                IsPlaying = false;
                orbit?.SetFinisherAim(null);
            }
        }
    }
}
