using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>每帧检测整段扫过的体积，同一发剑气只结算一次玩家命中。</summary>
    public sealed class BossSwordWave : MonoBehaviour
    {
        private readonly Collider[] contacts = new Collider[8];
        private BossController owner;
        private BossSkillSettings.SwordWaveSettings settings;
        private LayerMask environment;
        private LayerMask hurtboxes;
        private Vector3 start;
        private float travelled;
        private bool hit;

        internal void Initialize(BossController boss, BossSkillSettings.SwordWaveSettings skill, LayerMask walls, LayerMask targets)
        {
            owner = boss;
            settings = skill;
            environment = walls;
            hurtboxes = targets;
            start = transform.position;
        }

        private void Update()
        {
            if (owner == null || !owner.isActiveAndEnabled || owner.Receiver.Health.IsDead
                || owner.Status is BossStatus.PhaseTransition or BossStatus.FightOver
                || owner.Target == null || owner.Target.Health.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            float step = Mathf.Min(settings.speed * Time.deltaTime, settings.range - travelled);
            if (step <= 0f)
            {
                return;
            }

            Vector3 forward = transform.forward;
            bool blocked = Physics.Raycast(transform.position, forward, out RaycastHit wall, step, environment, QueryTriggerInteraction.Ignore);
            if (blocked)
            {
                step = wall.distance;
            }

            if (!hit)
            {
                CheckTarget(forward, step);
            }
            travelled += step;
            transform.position = start + forward * travelled;
            if (blocked || travelled >= settings.range)
            {
                Destroy(gameObject);
            }
        }

        private void CheckTarget(Vector3 forward, float step)
        {
            Vector3 midpoint = transform.position + forward * (step * 0.5f);
            var extents = new Vector3(settings.width * 0.5f, 0.42f, step * 0.5f + 0.12f);
            int count = Physics.OverlapBoxNonAlloc(midpoint, extents, contacts, transform.rotation, hurtboxes, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!contacts[i].TryGetComponent(out Hurtbox hurtbox) || hurtbox.Owner != owner.Target)
                {
                    continue;
                }

                Vector3 target = owner.Target.transform.position;
                target.y = midpoint.y;
                if (owner.SkillVisuals.Obstructed(transform.position, target))
                {
                    continue;
                }
                hit = true;
                float distance = Mathf.Clamp(Vector3.Dot(target - start, forward), 0f, settings.range);
                float damage = Mathf.Lerp(settings.nearDamage, settings.farDamage, distance / settings.range);
                owner.SkillVisuals.DamageTarget(owner, damage, target, forward);
                return;
            }
        }
    }
}
