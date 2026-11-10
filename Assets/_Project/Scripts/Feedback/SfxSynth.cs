using System;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 程序化音效：用几行数学直接合成波形，不依赖任何音频素材。
    /// 每种音效只在第一次用到时合成一次，之后复用同一个 AudioClip。
    ///
    /// 用到的三种基本手法：
    ///   噪声 + 低通滤波（截止频率随时间变化）→ 挥砍的"呼"声
    ///   噪声爆发 + 低频正弦，指数衰减        → 打击的"砰"声
    ///   几个不成整数倍关系的正弦叠加          → 金属碰撞的"铛"声（非谐波泛音是金属音色的特征）
    /// </summary>
    public static class SfxSynth
    {
        private const int SampleRate = 44100;
        private const float Tau = Mathf.PI * 2f;

        private static AudioClip whoosh;
        private static AudioClip impact;
        private static AudioClip heavyImpact;
        private static AudioClip clang;
        private static AudioClip shimmer;

        public static AudioClip Whoosh => whoosh ??= Build("Sfx_Whoosh", 0.2f, 11, (t, n, random, state) =>
        {
            // 截止频率先升后降，听起来像刀从面前掠过
            float sweep = Mathf.Sin(Mathf.PI * n);
            float cutoff = Mathf.Lerp(0.02f, 0.35f, sweep);
            state[0] += cutoff * (Noise(random) - state[0]);
            return state[0] * sweep * 1.6f;
        });

        public static AudioClip Impact => impact ??= Build("Sfx_Impact", 0.16f, 23, (t, n, random, state) =>
        {
            float crack = Noise(random) * Mathf.Exp(-t * 55f);
            float thump = Mathf.Sin(Tau * 110f * t) * Mathf.Exp(-t * 28f);
            return crack * 0.55f + thump * 0.8f;
        });

        public static AudioClip HeavyImpact => heavyImpact ??= Build("Sfx_HeavyImpact", 0.45f, 37, (t, n, random, state) =>
        {
            // 音高下滑的低频 + 更长的噪声尾巴
            float pitch = Mathf.Lerp(95f, 45f, n);
            state[0] += Tau * pitch / SampleRate;
            float thump = Mathf.Sin(state[0]) * Mathf.Exp(-t * 9f);
            float crack = Noise(random) * Mathf.Exp(-t * 22f);
            return thump * 0.95f + crack * 0.5f;
        });

        public static AudioClip Clang => clang ??= Build("Sfx_Clang", 0.6f, 41, (t, n, random, state) =>
        {
            float ring = Mathf.Sin(Tau * 1320f * t) * Mathf.Exp(-t * 7f)
                         + Mathf.Sin(Tau * 1987f * t) * Mathf.Exp(-t * 9f) * 0.7f
                         + Mathf.Sin(Tau * 2731f * t) * Mathf.Exp(-t * 12f) * 0.5f
                         + Mathf.Sin(Tau * 3520f * t) * Mathf.Exp(-t * 16f) * 0.35f;
            float click = Noise(random) * Mathf.Exp(-t * 180f);
            return ring * 0.3f + click * 0.6f;
        });

        public static AudioClip Shimmer => shimmer ??= Build("Sfx_Shimmer", 0.28f, 53, (t, n, random, state) =>
        {
            // 快速上扬的滑音，叠一个高八度
            float pitch = Mathf.Lerp(520f, 1650f, n * n);
            state[0] += Tau * pitch / SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * n);
            return (Mathf.Sin(state[0]) + Mathf.Sin(state[0] * 2f) * 0.35f) * envelope * 0.45f;
        });

        /// <summary>提前合成全部音效。</summary>
        public static void Prewarm()
        {
            _ = Whoosh;
            _ = Impact;
            _ = HeavyImpact;
            _ = Clang;
            _ = Shimmer;
        }

        /// <param name="sample">(时间秒, 归一化进度 0~1, 随机源, 滤波器/相位状态) → 这一个采样的振幅。</param>
        private static AudioClip Build(
            string name, float seconds, int seed, Func<float, float, System.Random, float[], float> sample)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            var random = new System.Random(seed); // 固定种子：每次运行合成出的声音完全一样
            var state = new float[2];

            // 结尾 5 毫秒淡出，避免波形被突然截断产生"咔哒"声
            int fadeSamples = SampleRate / 200;
            for (int i = 0; i < count; i++)
            {
                float value = sample((float)i / SampleRate, (float)i / count, random, state);
                float fade = Mathf.Clamp01((float)(count - i) / fadeSamples);
                data[i] = Mathf.Clamp(value * fade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Noise(System.Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }
    }
}
