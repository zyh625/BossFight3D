using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 能量飞镖：沿直线飞行的道具。每帧用球形扫掠检测"这一帧要走的这一段"，所以飞得再快也不会穿过目标。
    /// 飞到射程末段逐渐缩小消散。命中带 IProjectileReflector 且正在反弹状态的角色时，
    /// 不造成伤害，而是掉头飞向原来的主人——主人和目标的身份互换，原主人可以翻滚躲开自己的飞镖。
    ///
    /// 飞镖由发射器预先创建并反复使用（对象池），飞行结束只是隐藏自己。
    /// </summary>
    public sealed class EnergyDart : MonoBehaviour
    {
        private const int MaxContacts = 8;
        private const float ChestHeight = 1.1f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer body;
        [SerializeField] private TrailRenderer trail;
        [SerializeField, Min(0.01f)] private float radius = 0.14f;
        [SerializeField] private LayerMask hurtboxLayers;
        [SerializeField] private LayerMask environmentLayers;
        [Tooltip("飞过射程的这个比例之后开始缩小消散")]
        [SerializeField, Range(0f, 1f)] private float fadeStart = 0.65f;
        [SerializeField] private Color reflectedColor = new Color(1f, 0.25f, 0.2f);

        private readonly RaycastHit[] contacts = new RaycastHit[MaxContacts];
        private MaterialPropertyBlock block;
        private GameObject owner;
        private DamageReceiver ownerReceiver;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float guardDamage;
        private float range;
        private float travelled;

        public bool IsFlying => gameObject.activeSelf;

        /// <summary>这支飞镖是否被弹回过。一支飞镖只会被反弹一次。</summary>
        public bool WasReflected { get; private set; }

        public void Launch(GameObject shooter, Vector3 position, Vector3 flightDirection,
            float flightSpeed, float damageAmount, float guardDamageAmount, float maxRange, Color color)
        {
            owner = shooter;
            ownerReceiver = shooter.GetComponent<DamageReceiver>();
            direction = flightDirection.normalized;
            speed = flightSpeed;
            damage = damageAmount;
            guardDamage = guardDamageAmount;
            range = maxRange;
            travelled = 0f;
            WasReflected = false;

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            trail.Clear();
            SetColor(color);
        }

        private void Update()
        {
            float step = Mathf.Min(speed * Time.deltaTime, range - travelled);
            if (step <= 0f)
            {
                Stop();
                return;
            }

            if (Sweep(step, out bool stopped) && stopped)
            {
                return;
            }

            transform.position += direction * step;
            travelled += step;

            // 射程末段逐渐缩小：能量耗尽而不是突然消失
            float fade = Mathf.InverseLerp(range, range * fadeStart, travelled);
            transform.localScale = Vector3.one * Mathf.Clamp01(fade);

            if (travelled >= range)
            {
                Stop();
            }
        }

        /// <returns>这一段路上是否碰到了东西；stopped 表示飞镖已经结束飞行。</returns>
        private bool Sweep(float step, out bool stopped)
        {
            stopped = false;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, contacts, step,
                hurtboxLayers | environmentLayers, QueryTriggerInteraction.Collide);

            // 取最近的一个有效碰撞
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                Collider other = contacts[i].collider;
                bool isHurtbox = other.TryGetComponent(out Hurtbox hurtbox);
                if (isHurtbox ? hurtbox.Owner == null || hurtbox.Owner == ownerReceiver : other.isTrigger)
                {
                    continue;
                }

                if (nearest < 0 || contacts[i].distance < contacts[nearest].distance)
                {
                    nearest = i;
                }
            }

            if (nearest < 0)
            {
                return false;
            }

            RaycastHit hit = contacts[nearest];
            if (!hit.collider.TryGetComponent(out Hurtbox target))
            {
                // 撞到场景
                Stop();
                stopped = true;
                return true;
            }

            DamageReceiver receiver = target.Owner;
            if (!WasReflected && ownerReceiver != null
                && receiver.TryGetComponent(out IProjectileReflector reflector) && reflector.ReflectsProjectiles)
            {
                Reflect(receiver);
                stopped = true; // 这一帧不再前进，下一帧朝新方向飞
                return true;
            }

            // SphereCast 起点已经和目标重叠时 point 是零向量，这时用飞镖当前位置
            Vector3 point = hit.distance > 0f ? hit.point : transform.position;
            receiver.TakeDamage(new DamageInfo
            {
                Amount = damage,
                GuardDamage = guardDamage,
                Attacker = owner,
                HitPoint = point,
                Direction = new Vector3(direction.x, 0f, direction.z).normalized
            });
            Stop();
            stopped = true;
            return true;
        }

        private void Reflect(DamageReceiver reflectedBy)
        {
            Vector3 home = ownerReceiver.transform.position + Vector3.up * ChestHeight;
            direction = (home - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);

            owner = reflectedBy.gameObject;
            ownerReceiver = reflectedBy;
            travelled = 0f;
            WasReflected = true;
            SetColor(reflectedColor);
        }

        private void Stop()
        {
            gameObject.SetActive(false);
        }

        private void SetColor(Color color)
        {
            block ??= new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            body.SetPropertyBlock(block);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }
}
