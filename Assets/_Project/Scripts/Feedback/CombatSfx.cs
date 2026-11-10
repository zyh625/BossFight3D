using BossFight.Combat;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 战斗音效：武器判定开启时播挥砍声；本角色的结算结果出来时，按结果类型播命中 / 格挡 / 识破 / 闪避声。
    /// 和其他表现组件一样只监听事件，声音素材来自 SfxSynth 的程序合成。
    /// </summary>
    public sealed class CombatSfx : MonoBehaviour
    {
        [Tooltip("本角色的武器判定，每次开启播一次挥砍声")]
        [SerializeField] private Hitbox[] hitboxes;
        [SerializeField] private DamageReceiver receiver;

        [Header("音量")]
        [SerializeField, Range(0f, 1f)] private float swingVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defenseVolume = 0.7f;

        [Tooltip("每次播放随机偏移的音高范围，避免连续几刀听起来一模一样")]
        [SerializeField, Range(0f, 0.3f)] private float pitchJitter = 0.08f;
        [Tooltip("基础音高。体型大的角色调低一点，声音更沉")]
        [SerializeField, Range(0.5f, 1.5f)] private float basePitch = 1f;

        private AudioSource source;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D：第三人称近战，声音不需要随距离衰减

            // 预热：在加载阶段就把所有音效合成好，避免第一次挥刀/命中的那一帧才去合成而卡顿
            SfxSynth.Prewarm();
        }

        private void OnEnable()
        {
            foreach (Hitbox hitbox in hitboxes)
            {
                if (hitbox != null)
                {
                    hitbox.Activated += OnSwing;
                }
            }

            if (receiver != null)
            {
                receiver.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            foreach (Hitbox hitbox in hitboxes)
            {
                if (hitbox != null)
                {
                    hitbox.Activated -= OnSwing;
                }
            }

            if (receiver != null)
            {
                receiver.Damaged -= OnDamaged;
            }
        }

        private void OnSwing(float activeDuration)
        {
            Play(SfxSynth.Whoosh, swingVolume);
        }

        private void OnDamaged(DamageResult result)
        {
            switch (result.Outcome)
            {
                case HitOutcome.Parried:
                    Play(SfxSynth.Clang, defenseVolume);
                    break;
                case HitOutcome.Dodged:
                    Play(SfxSynth.Shimmer, defenseVolume);
                    break;
                case HitOutcome.Blocked:
                    Play(SfxSynth.Clang, defenseVolume * 0.6f, 1.25f);
                    break;
                case HitOutcome.Hit when result.FinalDamage > 0f:
                    Play(result.IsGuardBreakFinisher ? SfxSynth.HeavyImpact : SfxSynth.Impact, impactVolume);
                    break;
            }
        }

        private void Play(AudioClip clip, float volume, float pitchScale = 1f)
        {
            // PlayOneShot 允许同一个 AudioSource 上多个声音重叠播放
            source.pitch = basePitch * pitchScale * (1f + Random.Range(-pitchJitter, pitchJitter));
            source.PlayOneShot(clip, volume);
        }
    }
}
