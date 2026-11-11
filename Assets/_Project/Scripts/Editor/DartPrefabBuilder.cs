using BossFight.Combat;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 生成能量飞镖预制体：一段细长的菱形镖身 + 一条拖尾，都用特效材质。
    /// 运行时由 DartLauncher 复制出若干支放进对象池。
    /// </summary>
    internal static class DartPrefabBuilder
    {
        public static EnergyDart Build()
        {
            var root = new GameObject("EnergyDart");
            try
            {
                // 镖身：把立方体绕飞行方向转 45°，截面就是菱形
                GameObject body = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cube, "Body", root.transform);
                body.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                body.transform.localScale = new Vector3(0.09f, 0.09f, 0.7f);
                var bodyRenderer = body.GetComponent<Renderer>();
                bodyRenderer.sharedMaterial = ArenaMaterials.FxGlow;
                bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.14f;
                trail.widthCurve = AnimationCurve.Linear(0f, 0.12f, 1f, 0f);
                trail.sharedMaterial = ArenaMaterials.FxGlow;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.minVertexDistance = 0.05f;

                var dart = root.AddComponent<EnergyDart>();
                EditorWiring.SetRef(dart, "body", bodyRenderer);
                EditorWiring.SetRef(dart, "trail", trail);
                EditorWiring.Edit(dart, "hurtboxLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Hurtbox));
                EditorWiring.Edit(dart, "environmentLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Environment));

                ProjectPaths.EnsureFolderFor(ProjectPaths.DartPrefab);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.DartPrefab);
                return prefab.GetComponent<EnergyDart>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
