using BossFight.Combat;
using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 飞镖发射器：决定往哪打（瞄准），并从对象池里取一支飞镖发出去。
    ///
    /// 瞄准规则：锁定了目标就打目标；否则从屏幕中心发出一条射线，打到什么就瞄什么，什么都没打到就瞄远处一点。
    /// 注意飞镖是从角色手边飞出的，而瞄准点来自相机——先求出"玩家在看哪个点"，再让飞镖从手边飞向那个点，
    /// 这样准星指哪飞镖就落在哪，不会因为相机和角色不在同一位置而打偏。
    /// </summary>
    public sealed class DartLauncher : MonoBehaviour
    {
        private const float AimDistance = 60f;

        [SerializeField] private EnergyDart dartPrefab;
        [Tooltip("飞镖飞出的位置")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private LockOnTargeting targeting;
        [SerializeField] private PlayerCombatConfig config;
        [Tooltip("屏幕中心的瞄准射线可以打到哪些层")]
        [SerializeField] private LayerMask aimLayers;
        [SerializeField, Range(1, 24)] private int poolSize = 10;

        [Header("颜色")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.95f, 1f);
        [SerializeField] private Color chargedColor = new Color(1f, 0.35f, 0.9f);

        private EnergyDart[] pool;
        private Transform poolRoot;
        private Transform cameraTransform;
        private int next;

        private void Start()
        {
            Camera main = Camera.main;
            cameraTransform = main != null ? main.transform : null;

            // 预先创建好全部飞镖，战斗中只是取用和归还
            poolRoot = new GameObject($"{name}_Darts").transform;
            pool = new EnergyDart[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                pool[i] = Instantiate(dartPrefab, poolRoot);
                pool[i].gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }

        public void RecallAll()
        {
            if (pool == null)
            {
                return;
            }

            foreach (EnergyDart dart in pool)
            {
                dart.gameObject.SetActive(false);
            }
        }

        /// <summary>这一刻飞镖会飞向的水平方向，投掷状态用它让角色先转身。</summary>
        public Vector3 GetAimDirection()
        {
            Vector3 direction = GetAimPoint() - muzzle.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        /// <param name="chargeRatio">蓄力进度 0~1。0 是普通飞镖。</param>
        public EnergyDart Fire(float chargeRatio)
        {
            bool charged = chargeRatio > 0f;
            Vector3 origin = muzzle.position;
            Vector3 direction = (GetAimPoint() - origin).normalized;

            float damage = charged
                ? Mathf.Lerp(config.DartChargedMinDamage, config.DartChargedMaxDamage, chargeRatio)
                : config.DartDamage;

            EnergyDart dart = pool[next];
            next = (next + 1) % pool.Length;
            dart.Launch(gameObject, origin, direction,
                charged ? config.DartChargedSpeed : config.DartSpeed,
                damage,
                charged ? config.DartChargedGuardDamage : config.DartGuardDamage,
                config.DartRange,
                charged ? chargedColor : normalColor);
            return dart;
        }

        private Vector3 GetAimPoint()
        {
            if (targeting != null && targeting.Target != null)
            {
                return targeting.Target.AimPoint;
            }

            if (cameraTransform == null)
            {
                return muzzle.position + transform.forward * AimDistance;
            }

            // 射线从"越过角色"的位置开始：相机在角色身后，从相机位置出发会先打到角色自己
            Vector3 forward = cameraTransform.forward;
            float skip = Mathf.Max(0f, Vector3.Dot(muzzle.position - cameraTransform.position, forward)) + 0.6f;
            Vector3 start = cameraTransform.position + forward * skip;

            return Physics.Raycast(start, forward, out RaycastHit hit, AimDistance, aimLayers, QueryTriggerInteraction.Collide)
                ? hit.point
                : start + forward * AimDistance;
        }
    }
}
