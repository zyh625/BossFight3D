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
