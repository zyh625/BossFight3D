using BossFight.Combat;
using BossFight.DevTools;
using BossFight.Feedback;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 生成训练木桩预制体。结构和之后的 Boss 一致：
    ///   TrainingDummy（逻辑根：实体碰撞 + Health + DamageReceiver）
    ///   ├─ Visual    外观
    ///   └─ Hurtbox   受击判定（Trigger，在 Hurtbox 层）
    /// </summary>
    internal static class DummyPrefabBuilder
    {
        private const float Height = 2f;
        private const float Radius = 0.45f;
        private const float MaxHealth = 300f;

        public static GameObject Build()
        {
            var root = new GameObject("TrainingDummy");
            try
            {
                root.layer = LayerSetup.Get(LayerSetup.Enemy);

                // 实体碰撞：挡住玩家，不参与伤害判定
                var body = root.AddComponent<CapsuleCollider>();
                body.height = Height;
                body.radius = Radius;
                body.center = new Vector3(0f, Height * 0.5f, 0f);

                var health = root.AddComponent<Health>();
                EditorWiring.Edit(health, "max", p => p.floatValue = MaxHealth);
                var receiver = root.AddComponent<DamageReceiver>();

                Transform visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);

                GameObject capsule = EditorWiring.CreateVisualPrimitive(PrimitiveType.Capsule, "Body", visual);
                capsule.transform.localPosition = new Vector3(0f, Height * 0.5f, 0f);
                capsule.transform.localScale = new Vector3(Radius * 2f, Height * 0.5f, Radius * 2f);
                var bodyRenderer = capsule.GetComponent<Renderer>();
                bodyRenderer.sharedMaterial = ArenaMaterials.PlaceholderBody;

                GameObject band = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cylinder, "Band", visual);
                band.transform.localPosition = new Vector3(0f, Height * 0.7f, 0f);
                band.transform.localScale = new Vector3(Radius * 2.1f, 0.04f, Radius * 2.1f);
                band.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.NeonAccent;

                var hurtboxObject = new GameObject("Hurtbox") { layer = LayerSetup.Get(LayerSetup.Hurtbox) };
                hurtboxObject.transform.SetParent(root.transform, false);
                var trigger = hurtboxObject.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true;
                trigger.height = Height;
                trigger.radius = Radius;
                trigger.center = new Vector3(0f, Height * 0.5f, 0f);
                var hurtbox = hurtboxObject.AddComponent<Hurtbox>();
                EditorWiring.SetRef(hurtbox, "owner", receiver);

                var flash = root.AddComponent<HitFlash>();
                EditorWiring.SetRef(flash, "receiver", receiver);
                EditorWiring.Edit(flash, "renderers", p =>
                {
                    p.arraySize = 1;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
                });

                var dummy = root.AddComponent<TrainingDummy>();
                EditorWiring.SetRef(dummy, "health", health);
                WireM3(root);

                ProjectPaths.EnsureFolderFor(ProjectPaths.DummyPrefab);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.DummyPrefab);
                Debug.Log($"[BossFight] 已生成 {ProjectPaths.DummyPrefab}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        internal static void WireM3(GameObject root)
        {
            if (!root.TryGetComponent<LockOnTarget>(out _))
            {
                root.AddComponent<LockOnTarget>();
            }

            CombatFeedbackBuilder.AddImpact(root);
        }
    }
}
