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

        public static Material Floor => GetOrCreate("Floor", new Color(0.06f, 0.07f, 0.1f), 0.3f, 0.8f);
        public static Material Wall => GetOrCreate("Wall", new Color(0.1f, 0.11f, 0.15f), 0.1f, 0.4f);
        public static Material NeonStrip => GetOrCreate("NeonCyan", Color.black, 0f, 0.5f, NeonCyan, 4f);
        public static Material NeonAccent => GetOrCreate("NeonMagenta", Color.black, 0f, 0.5f, NeonMagenta, 4f);
        public static Material PlaceholderBody => GetOrCreate("PlaceholderBody", new Color(0.3f, 0.32f, 0.4f), 0f, 0.5f);

        /// <summary>
        /// 所有特效（刀光、火花、光环、残影）共用的加色材质：无光照、叠加混合、双面、不写深度。
        /// 基础颜色是 HDR 白（亮度 3），实际颜色来自顶点色 / 粒子颜色，所以一份材质就够用，
        /// 并且任何用它的东西都会超过 Bloom 阈值而发光。
        /// </summary>
        public static Material FxAdditive
        {
            get
            {
                string path = $"{ProjectPaths.Materials}/FxAdditive.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null)
                {
                    return existing;
                }

                var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                ConfigureAdditive(material);
                ProjectPaths.EnsureFolderFor(path);
                AssetDatabase.CreateAsset(material, path);
                // URP 在创建材质资源时会按属性重新校验一遍关键字，创建后再设一次保证最终状态正确
                ConfigureAdditive(material);
                EditorUtility.SetDirty(material);
                return material;
            }
        }

        private static void ConfigureAdditive(Material material)
        {
            material.SetFloat("_Surface", 1f);   // Transparent
            material.SetFloat("_Blend", 2f);     // Additive
            material.SetFloat("_Cull", 0f);      // 双面
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetColor("_BaseColor", new Color(3f, 3f, 3f, 1f));
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
