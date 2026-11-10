using BossFight.Combat;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 命中特效：监听本角色的结算结果，按结果类型播放火花、光环和一次闪光。
    ///   命中      → 火花（数量随伤害增加）
    ///   被格挡    → 少量冷色火花 + 小光环
    ///   识破      → 金色大光环 + 火花 + 闪光
    ///   完美闪避  → 脚下的青色光环
    ///   破防重击  → 最大的一档
    /// 粒子系统和光环都在 Awake 里用代码创建并反复使用，运行中不产生新物体。
    /// </summary>
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class ImpactVfx : MonoBehaviour
    {
        private const int RingPoolSize = 3;
        private const float FallbackHeight = 1.2f;

        [SerializeField] private Material material;

        [Header("颜色")]
        [SerializeField] private Color hitColor = new Color(1f, 0.85f, 0.55f);
        [SerializeField] private Color blockColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color parryColor = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private Color dodgeColor = new Color(0.25f, 0.95f, 1f);

        [Header("火花")]
        [SerializeField, Min(0)] private int baseSparkCount = 10;
        [Tooltip("每点伤害额外增加的火花数")]
        [SerializeField, Min(0f)] private float sparksPerDamage = 0.5f;
        [SerializeField, Min(1)] private int maxSparkCount = 70;

        [Tooltip("光环颜色的亮度倍率，大于 1 才会产生辉光")]
        [SerializeField, Min(0f)] private float ringIntensity = 3f;

        [Header("闪光")]
        [SerializeField, Min(0f)] private float flashIntensity = 14f;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.14f;

        private DamageReceiver receiver;
        private Transform effectRoot;
        private ParticleSystem sparks;
        private PulseRing[] rings;
        private int nextRing;
        private Light flash;
        private float flashRemaining;

        private void Awake()
        {
            receiver = GetComponent<DamageReceiver>();

            // 特效放在场景根部：角色被隐藏或移动时，已经播出去的特效不受影响
            effectRoot = new GameObject($"{name}_ImpactVfx").transform;
            sparks = CreateSparks();

            rings = new PulseRing[RingPoolSize];
            for (int i = 0; i < RingPoolSize; i++)
            {
                rings[i] = PulseRing.Create(material, effectRoot);
            }

            var flashObject = new GameObject("Flash");
            flashObject.transform.SetParent(effectRoot, false);
            flash = flashObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 7f;
            flash.shadows = LightShadows.None;
            flash.enabled = false;
        }

        private void OnEnable()
        {
            receiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            receiver.Damaged -= OnDamaged;
        }

        private void OnDestroy()
        {
            if (effectRoot != null)
            {
                Destroy(effectRoot.gameObject);
            }
        }

        private void Update()
        {
            if (flashRemaining <= 0f)
            {
                return;
            }

            flashRemaining -= Time.deltaTime;
            flash.intensity = flashIntensity * Mathf.Clamp01(flashRemaining / flashDuration);
            flash.enabled = flashRemaining > 0f;
        }

        private void OnDamaged(DamageResult result)
        {
            Vector3 chest = transform.position + Vector3.up * FallbackHeight;
            Vector3 point = result.Info.HitPoint.sqrMagnitude > 0.0001f ? result.Info.HitPoint : chest;
            Vector3 incoming = result.Info.Direction.sqrMagnitude > 0.0001f ? result.Info.Direction : transform.forward;

            switch (result.Outcome)
            {
                case HitOutcome.Dodged:
                    PlayRing(transform.position + Vector3.up * 0.05f, Vector3.up, dodgeColor, 0.4f, 2.2f, 0.35f);
                    break;

                case HitOutcome.Parried:
                    EmitSparks(chest, -incoming, parryColor, 36);
                    PlayRing(chest, incoming, parryColor, 0.3f, 2.4f, 0.3f);
                    PlayRing(transform.position + Vector3.up * 0.05f, Vector3.up, parryColor, 0.5f, 3.2f, 0.45f);
                    Flash(chest, parryColor);
                    break;

                case HitOutcome.Blocked:
                    EmitSparks(point, -incoming, blockColor, baseSparkCount);
                    PlayRing(point, incoming, blockColor, 0.15f, 0.9f, 0.18f);
                    break;

                case HitOutcome.Hit when result.FinalDamage > 0f:
                    if (result.IsGuardBreakFinisher)
                    {
                        EmitSparks(point, -incoming, parryColor, maxSparkCount);
                        PlayRing(point, incoming, parryColor, 0.4f, 3.5f, 0.4f);
                        PlayRing(transform.position + Vector3.up * 0.05f, Vector3.up, parryColor, 0.6f, 5f, 0.6f);
                        Flash(point, parryColor);
                    }
                    else
                    {
                        int count = Mathf.Min(maxSparkCount,
                            baseSparkCount + Mathf.RoundToInt(result.FinalDamage * sparksPerDamage));
                        EmitSparks(point, -incoming, hitColor, count);
                    }
                    break;
            }
        }

        private void EmitSparks(Vector3 position, Vector3 direction, Color color, int count)
        {
            // 火花朝攻击者一侧、略微向上的锥形范围内飞溅
            Vector3 axis = (direction + Vector3.up * 0.35f).normalized;
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(axis));
            sparks.Emit(new ParticleSystem.EmitParams { startColor = color }, count);
        }

        private void PlayRing(Vector3 position, Vector3 normal, Color color, float from, float to, float seconds)
        {
            rings[nextRing].Play(position, normal, color * ringIntensity, from, to, seconds);
            nextRing = (nextRing + 1) % RingPoolSize;
        }

        private void Flash(Vector3 position, Color color)
        {
            flash.transform.position = position;
            flash.color = color;
            flash.intensity = flashIntensity;
            flash.enabled = true;
            flashRemaining = flashDuration;
        }

        private ParticleSystem CreateSparks()
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(effectRoot, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 13f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.gravityModifier = 1.4f;
            main.maxParticles = 512;

            // 不自动发射，只在命中时由代码 Emit
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 50f;
            shape.radius = 0.05f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            // 沿速度方向拉伸：没有贴图的方形粒子会变成一道道细长的光丝
            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.05f;
            particleRenderer.lengthScale = 1.5f;
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return system;
        }
    }
}
