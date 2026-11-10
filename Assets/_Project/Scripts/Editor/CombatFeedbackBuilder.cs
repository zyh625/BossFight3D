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
    }
}
