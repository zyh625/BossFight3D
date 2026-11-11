using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal interface IBossWindup
    {
        float WindupRemaining { get; }
    }

    internal sealed class BossBlinkSlashState : IState, IBossWindup
    {
        private readonly BossController boss;
        private BossSkillSettings.BlinkSlashSettings settings;
        private float elapsed;
        private Vector3 landingPoint;
        private Vector3 approachDirection;
        private bool hovering;
        private bool landed;
        private bool hit;

        public float WindupRemaining => Mathf.Max(0f, settings.windup + settings.hover - elapsed);

        public BossBlinkSlashState(BossController boss) => this.boss = boss;

        // 长剑形态下落点范围更大
        private float Radius => settings.radius * boss.SkillScale;

        public void Enter()
        {
            settings = boss.Config.Skills.blinkSlash;
            elapsed = 0f;
            hovering = false;
            landed = false;
            hit = false;
            boss.Weapons.Windup(2, 0f);
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            if (elapsed < settings.windup)
            {
                boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed, dt);
                boss.Motor.Move(Vector3.zero, dt);
                boss.Weapons.Windup(2, elapsed / settings.windup);
                return;
            }

            if (!hovering)
            {
                hovering = true;
                approachDirection = boss.DirectionToTarget().normalized;
                landingPoint = boss.Target.transform.position;
                boss.Motor.Teleport(landingPoint + Vector3.up * settings.height);
                boss.SkillVisuals.ShowRing(landingPoint, Radius, new Color(1f, 0.23f, 0.08f));
            }

            float impactTime = settings.windup + settings.hover;
            if (elapsed < impactTime)
            {
                return;
            }

            float activeEnd = impactTime + settings.active;
            if (!landed)
            {
                Land();
                TryHit();
            }
            else if (elapsed < activeEnd)
            {
                TryHit();
            }

            if (elapsed >= activeEnd)
            {
                boss.SkillVisuals.HideRing();
                boss.Weapons.Rest();
            }
            if (elapsed >= activeEnd + settings.recovery)
            {
                boss.ToApproach();
            }
        }

        public void Exit()
        {
            if (hovering && !landed)
            {
                Land();
            }
            boss.SkillVisuals.HideRing();
        }

        private void Land()
        {
            landed = true;
            Vector3 safePoint = landingPoint;
            if (boss.Target != null)
            {
                Vector3 separation = boss.Target.transform.position - landingPoint;
                separation.y = 0f;
                const float clearance = 0.95f;
                if (separation.sqrMagnitude < clearance * clearance)
                {
                    Vector3 offset = separation.sqrMagnitude > 0.01f ? -separation.normalized : -approachDirection;
                    Vector3 candidate = landingPoint + offset * clearance;
                    if (!boss.SkillVisuals.Obstructed(landingPoint + Vector3.up, candidate + Vector3.up))
                    {
                        safePoint = candidate;
                    }
                }
            }
            boss.Motor.Teleport(safePoint);
            boss.Weapons.Swing(2, 0.5f);
        }

        private void TryHit()
        {
            if (hit || boss.Target == null || boss.Target.Health.IsDead)
            {
                return;
            }

            Vector3 delta = boss.Target.transform.position - landingPoint;
            delta.y = 0f;
            if (delta.sqrMagnitude <= Radius * Radius)
            {
                hit = true;
                boss.SkillVisuals.DamageTarget(boss, settings.damage, landingPoint, delta.normalized);
            }
        }
    }
}
