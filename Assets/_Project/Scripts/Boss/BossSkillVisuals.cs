using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    public sealed class BossSkillVisuals : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private const int RingSegments = 48;

        [SerializeField] private LineRenderer ring;
        [SerializeField] private TrailRenderer retreatTrail;
        [SerializeField] private Material waveMaterial;
        [SerializeField] private LayerMask environmentLayers;
        [SerializeField] private LayerMask hurtboxLayers;

        private readonly Vector3[] ringPoints = new Vector3[RingSegments];
        private MaterialPropertyBlock ringColor;

        private void Awake()
        {
            ringColor = new MaterialPropertyBlock();
            ring.positionCount = RingSegments;
            Clear();
        }

        internal void ShowRing(Vector3 center, float radius, Color color)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * (Mathf.PI * 2f / RingSegments);
                ringPoints[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0.045f, Mathf.Sin(angle) * radius);
            }

            ring.SetPositions(ringPoints);
            ringColor.SetColor(EmissionColor, color * 4f);
            ring.SetPropertyBlock(ringColor);
            ring.enabled = true;
        }

        internal void HideRing() => ring.enabled = false;

        internal void StartTrail()
        {
            retreatTrail.Clear();
            retreatTrail.emitting = true;
        }

        internal void EndTrail() => retreatTrail.emitting = false;

        internal void Clear()
        {
            HideRing();
            EndTrail();
            retreatTrail.Clear();
        }

        internal bool Obstructed(Vector3 from, Vector3 to)
        {
            return Physics.Linecast(from, to, environmentLayers, QueryTriggerInteraction.Ignore);
        }

        internal void LaunchWave(BossController boss, BossSkillSettings.SwordWaveSettings settings)
        {
            var waveObject = new GameObject("SwordWave");
            waveObject.transform.SetPositionAndRotation(boss.transform.position + Vector3.up * 0.4f, boss.transform.rotation);

            var blade = waveObject.AddComponent<LineRenderer>();
            blade.sharedMaterial = waveMaterial;
            blade.useWorldSpace = false;
            blade.widthMultiplier = 0.2f;
            blade.positionCount = 3;
            blade.SetPosition(0, new Vector3(-settings.width * 0.5f, 0f, -0.3f));
            blade.SetPosition(1, new Vector3(0f, 0f, 0.12f));
            blade.SetPosition(2, new Vector3(settings.width * 0.5f, 0f, -0.3f));
            waveObject.AddComponent<BossSwordWave>().Initialize(boss, settings, environmentLayers, hurtboxLayers);
        }

        internal void DamageTarget(BossController boss, float damage, Vector3 point, Vector3 direction)
        {
            boss.Target.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Attacker = boss.gameObject,
                HitPoint = point,
                Direction = direction
            });
        }
    }
}
