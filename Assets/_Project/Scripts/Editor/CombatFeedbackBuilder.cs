using BossFight.Combat;
using BossFight.Feedback;
using Unity.Cinemachine;
using UnityEngine;

namespace BossFight.EditorTools
{
    internal static class CombatFeedbackBuilder
    {
        public static void AddImpact(GameObject actor)
        {
            if (!actor.TryGetComponent<CombatImpactFeedback>(out _))
            {
                actor.AddComponent<CombatImpactFeedback>();
                var impulse = actor.GetComponent<CinemachineImpulseSource>();
                impulse.ImpulseDefinition.ImpulseDuration = 0.12f;
            }
        }

        /// <summary>命中特效 + 音效。任何有 DamageReceiver 的角色都可以加。</summary>
        public static void AddHitEffects(GameObject actor, Hitbox[] hitboxes, float voicePitch = 1f)
        {
            var vfx = actor.AddComponent<ImpactVfx>();
            EditorWiring.SetRef(vfx, "material", ArenaMaterials.FxAdditive);

            var sfx = actor.AddComponent<CombatSfx>();
            EditorWiring.SetRef(sfx, "receiver", actor.GetComponent<DamageReceiver>());
            EditorWiring.Edit(sfx, "basePitch", p => p.floatValue = voicePitch);
            EditorWiring.Edit(sfx, "hitboxes", p =>
            {
                p.arraySize = hitboxes.Length;
                for (int i = 0; i < hitboxes.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = hitboxes[i];
                }
            });
        }

        /// <summary>刀光拖尾：跟随 basePoint—tipPoint 这条线段，在 hitbox 判定期间出现。</summary>
        public static void AddWeaponTrail(
            GameObject actor, Hitbox hitbox, Transform basePoint, Transform tipPoint, Color color)
        {
            var trail = actor.AddComponent<WeaponTrail>();
            EditorWiring.SetRef(trail, "hitbox", hitbox);
            EditorWiring.SetRef(trail, "basePoint", basePoint);
            EditorWiring.SetRef(trail, "tipPoint", tipPoint);
            EditorWiring.SetRef(trail, "material", ArenaMaterials.FxAdditive);
            EditorWiring.Edit(trail, "color", p => p.colorValue = color);
        }

        /// <summary>残影：高速移动或瞬移时自动出现。</summary>
        public static void AddAfterImages(GameObject actor, Transform[] sources, Color color, float speedThreshold)
        {
            var trail = actor.AddComponent<AfterImageTrail>();
            EditorWiring.SetRef(trail, "material", ArenaMaterials.FxAdditive);
            EditorWiring.Edit(trail, "color", p => p.colorValue = color);
            EditorWiring.Edit(trail, "speedThreshold", p => p.floatValue = speedThreshold);
            EditorWiring.Edit(trail, "sources", p =>
            {
                p.arraySize = sources.Length;
                for (int i = 0; i < sources.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
                }
            });
        }
    }
}
