using BossFight.Boss;
using BossFight.Character;
using BossFight.Combat;
using BossFight.Feedback;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    internal static class BossPrefabBuilder
    {
        private const float Height = 2.7f;
        private const float Radius = 0.53f;

        public static GameObject Build()
        {
            var root = new GameObject("ShadowSwordsman") { layer = LayerSetup.Get(LayerSetup.Enemy) };
            try
            {
                var controller = root.AddComponent<CharacterController>();
                controller.height = Height;
                controller.radius = Radius;
                controller.center = Vector3.up * (Height * 0.5f);
                root.AddComponent<CharacterMotor>();

                var receiver = root.AddComponent<DamageReceiver>();
                EditorWiring.Edit(root.GetComponent<Health>(), "max", p => p.floatValue = 1200f);
                AddHurtbox(root.transform, receiver);

                var targeting = root.AddComponent<LockOnTarget>();
                EditorWiring.Edit(targeting, "localAimPoint", p => p.vector3Value = Vector3.up * 1.8f);
                BossWeaponRig weapons = AddWeaponsAndVisual(root, receiver);
                CombatFeedbackBuilder.AddImpact(root);

                var boss = root.AddComponent<BossController>();
                EditorWiring.SetRef(boss, "config", CombatDataBuilder.GetOrCreateBossCombatConfig());
                EditorWiring.SetRef(boss, "weapons", weapons);
                WireSkills(root);

                ProjectPaths.EnsureFolderFor(ProjectPaths.BossPrefab);
                return PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.BossPrefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        internal static void WireSkills(GameObject root)
        {
            if (root.TryGetComponent<BossSkillVisuals>(out _))
            {
                return;
            }

            Transform visuals = Child(root.transform, "SkillVisuals");
            var ring = Child(visuals, "ImpactRing").gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.widthMultiplier = 0.095f;
            ring.sharedMaterial = ArenaMaterials.NeonAccent;
            ring.enabled = false;

            Transform trailPoint = Child(visuals, "RetreatTrail");
            trailPoint.localPosition = Vector3.up * 1.1f;
            var trail = trailPoint.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.23f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0f);
            trail.sharedMaterial = ArenaMaterials.NeonStrip;
            trail.emitting = false;

            var skills = root.AddComponent<BossSkillVisuals>();
            EditorWiring.SetRef(skills, "ring", ring);
            EditorWiring.SetRef(skills, "retreatTrail", trail);
            EditorWiring.SetRef(skills, "waveMaterial", ArenaMaterials.NeonStrip);
            EditorWiring.Edit(skills, "environmentLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Environment));
            EditorWiring.Edit(skills, "hurtboxLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Hurtbox));
            EditorWiring.SetRef(root.GetComponent<BossController>(), "skillVisuals", skills);
        }

        private static void AddHurtbox(Transform root, DamageReceiver receiver)
        {
            Transform hurtbox = Child(root, "Hurtbox");
            hurtbox.gameObject.layer = LayerSetup.Get(LayerSetup.Hurtbox);
            var collider = hurtbox.gameObject.AddComponent<CapsuleCollider>();
            collider.height = Height;
            collider.radius = Radius;
            collider.center = Vector3.up * (Height * 0.5f);
            collider.isTrigger = true;
            EditorWiring.SetRef(hurtbox.gameObject.AddComponent<Hurtbox>(), "owner", receiver);
        }

        private static BossWeaponRig AddWeaponsAndVisual(GameObject root, DamageReceiver receiver)
        {
            Transform visual = Child(root.transform, "Visual");
            var body = EditorWiring.CreateVisualPrimitive(PrimitiveType.Capsule, "Body", visual);
            body.transform.localPosition = Vector3.up * (Height * 0.5f);
            body.transform.localScale = new Vector3(Radius * 2f, Height * 0.5f, Radius * 2f);
            var bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = ArenaMaterials.PlaceholderBody;
            Renderer signal = Block(visual, "Signal", new Vector3(0f, 2.05f, 0.43f),
                new Vector3(0.75f, 0.12f, 0.16f), ArenaMaterials.NeonAccent);
            var flash = root.AddComponent<HitFlash>();
            EditorWiring.SetRef(flash, "receiver", receiver);
            EditorWiring.Edit(flash, "renderers", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            });

            Transform swords = Child(root.transform, "Sockets");
            Transform left = Sword(swords, "Sword_L");
            Transform right = Sword(swords, "Sword_R");
            Transform sweep = Child(root.transform, "SwordSweep");
            Transform start = Child(sweep, "Base");
            Transform end = Child(sweep, "Tip");
            start.localPosition = Vector3.forward * 0.15f;
            end.localPosition = Vector3.forward * 2.05f;
            var hitbox = sweep.gameObject.AddComponent<Hitbox>();
            EditorWiring.SetRef(hitbox, "basePoint", start);
            EditorWiring.SetRef(hitbox, "tipPoint", end);
            EditorWiring.Edit(hitbox, "radius", p => p.floatValue = 0.24f);
            EditorWiring.Edit(hitbox, "hurtboxLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Hurtbox));

            var rig = root.AddComponent<BossWeaponRig>();
            EditorWiring.SetRef(rig, "visual", visual);
            EditorWiring.SetRef(rig, "leftSword", left);
            EditorWiring.SetRef(rig, "rightSword", right);
            EditorWiring.SetRef(rig, "sweepPivot", sweep);
            EditorWiring.SetRef(rig, "signal", signal);
            EditorWiring.SetRef(rig, "hitbox", hitbox);
            return rig;
        }

        private static Transform Sword(Transform parent, string name)
        {
            Transform sword = Child(parent, name);
            Block(sword, "Grip", Vector3.forward * 0.1f, new Vector3(0.09f, 0.09f, 0.2f), ArenaMaterials.PlaceholderBody);
            Block(sword, "Blade", Vector3.forward * 1.1f, new Vector3(0.05f, 0.13f, 1.9f), ArenaMaterials.NeonAccent);
            return sword;
        }

        private static Renderer Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cube, name, parent);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            var renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
