using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>场地和占位角色用到的材质：深色基底 + 高饱和自发光。已存在则复用，不覆盖你的修改。</summary>
    internal static class ArenaMaterials
    {
        private const string LitShader = "Universal Render Pipeline/Lit";

        public static readonly Color NeonCyan = new Color(0.1f, 0.9f, 1f);
        public static readonly Color NeonMagenta = new Color(1f, 0.15f, 0.7f);

        public static Material Floor => GetOrCreate("Floor", new Color(0.62f, 0.66f, 0.72f), 0f, 0.45f);
        public static Material Wall => GetOrCreate("Wall", new Color(0.82f, 0.84f, 0.88f), 0f, 0.3f);
        public static Material NeonStrip => GetOrCreate("NeonCyan", Color.black, 0f, 0.5f, NeonCyan, 1.6f);
        public static Material NeonAccent => GetOrCreate("NeonMagenta", Color.black, 0f, 0.5f, NeonMagenta, 1.6f);
        public static Material PlaceholderBody => GetOrCreate("PlaceholderBody", new Color(0.16f, 0.18f, 0.26f), 0f, 0.5f);

        /// <summary>
        /// 所有特效（刀光、火花、光环、残影）共用的材质：无光照、半透明混合、双面、不写深度。
        /// 实际颜色和透明度来自顶点色 / 粒子颜色 / MaterialPropertyBlock，所以一份材质就够用。
        ///
        /// 用普通的透明混合而不是加色混合：场地是明亮配色，加色混合是"往背景上加亮度"，
        /// 背景本来就亮时几乎看不出来；透明混合是"用特效的颜色盖住背景"，在亮背景上也清晰。
        /// </summary>
        public static Material FxGlow
        {
            get
            {
                string path = $"{ProjectPaths.Materials}/FxGlow.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null)
                {
                    return existing;
                }

                var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                ConfigureTransparent(material);
                ProjectPaths.EnsureFolderFor(path);
                AssetDatabase.CreateAsset(material, path);
                // URP 在创建材质资源时会按属性重新校验一遍关键字，创建后再设一次保证最终状态正确
                ConfigureTransparent(material);
                EditorUtility.SetDirty(material);
                return material;
            }
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);   // Transparent
            material.SetFloat("_Blend", 0f);     // Alpha
            material.SetFloat("_Cull", 0f);      // 双面
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // 略高于 1 的亮度：颜色鲜艳并带一点辉光，但不会被色调映射洗成白色
            material.SetColor("_BaseColor", new Color(1.3f, 1.3f, 1.3f, 1f));
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static Material GetOrCreate(
            string name, Color baseColor, float metallic, float smoothness,
            Color? emission = null, float emissionIntensity = 0f)
        {
            string path = $"{ProjectPaths.Materials}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find(LitShader));
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            ProjectPaths.EnsureFolderFor(path);
            AssetDatabase.CreateAsset(material, path);

            if (emission.HasValue)
            {
                // 必须在 CreateAsset 之后设置：URP 在材质资源创建时会按默认值重置关键字，
                // 之前打开的 _EMISSION 会被清掉
                material.SetColor("_EmissionColor", emission.Value * emissionIntensity); // 强度 > 1 才会超过 Bloom 阈值
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(material);
            }

            return material;
        }
    }
}
