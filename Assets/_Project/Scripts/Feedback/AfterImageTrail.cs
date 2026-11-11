using System.Collections.Generic;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 残影：角色高速移动（翻滚、后撤）或瞬移时，在经过的位置留下逐渐消失的半透明轮廓。
    ///
    /// 它不需要任何人通知——只观察自己这一帧相对上一帧移动了多远：
    ///   一帧内位移超过瞬移阈值 → 沿路径补几个残影；速度超过阈值 → 每隔一小段时间留一个。
    /// 所以给任何角色挂上就能用，Boss 的招式代码里不需要出现"播放残影"。
    ///
    /// 残影复制的是模型当前的网格：普通网格直接复用，蒙皮网格（以后的人形模型）用 BakeMesh 把
    /// 当前姿势烘成静态网格。换模型之后不需要改这里。
    /// </summary>
    public sealed class AfterImageTrail : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private sealed class Ghost
        {
            public GameObject Root;
            public Transform[] Parts;
            public MeshFilter[] Filters;
            public MeshRenderer[] Renderers;
            public Mesh[] BakedMeshes;
            public float Age;
            public bool Active;
        }

        [Tooltip("要复制外观的物体（模型容器、武器等），会包含它们的所有子物体")]
        [SerializeField] private Transform[] sources;
        [SerializeField] private Material material;
        [SerializeField] private Color color = new Color(0.2f, 0.85f, 1f);
        [Tooltip("颜色的亮度倍率")]
        [SerializeField, Min(0f)] private float intensity = 1.2f;
        [Tooltip("残影刚出现时的不透明度")]
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.55f;

        [Header("触发条件")]
        [Tooltip("水平速度超过这个值时开始留残影（m/s）")]
        [SerializeField, Min(0f)] private float speedThreshold = 9f;
        [Tooltip("一帧内位移超过这个距离视为瞬移（米）")]
        [SerializeField, Min(0.1f)] private float teleportDistance = 2.5f;
        [SerializeField, Range(1, 6)] private int teleportGhosts = 4;

        [Header("外观")]
        [SerializeField, Min(0.01f)] private float interval = 0.05f;
        [SerializeField, Min(0.05f)] private float lifetime = 0.32f;
        [SerializeField, Range(2, 24)] private int poolSize = 12;

        private readonly List<Renderer> sourceRenderers = new List<Renderer>();
        private Ghost[] pool;
        private Transform poolRoot;
        private MaterialPropertyBlock block;
        private Vector3 previousPosition;
        private float cooldown;
        private int nextGhost;

        // Start 而不是 Awake：此时模型已经就位，Socket 也已经挂到骨骼上
        private void Start()
        {
            block = new MaterialPropertyBlock();
            foreach (Transform source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                foreach (Renderer candidate in source.GetComponentsInChildren<Renderer>(true))
                {
                    if (candidate is MeshRenderer || candidate is SkinnedMeshRenderer)
                    {
                        sourceRenderers.Add(candidate);
                    }
                }
            }

            poolRoot = new GameObject($"{name}_AfterImages").transform;
            pool = new Ghost[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                pool[i] = CreateGhost(i);
            }

            previousPosition = transform.position;
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
        }

        private void OnDisable()
        {
            if (pool == null)
            {
                return;
            }

            foreach (Ghost ghost in pool)
            {
                ghost.Active = false;
                if (ghost.Root != null)
                {
                    ghost.Root.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (pool == null)
            {
                return;
            }

            foreach (Ghost ghost in pool)
            {
                foreach (Mesh baked in ghost.BakedMeshes)
                {
                    Destroy(baked);
                }
            }

            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }

        // LateUpdate：这一帧的位移和动画姿势都已经确定
        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 position = transform.position;
            Vector3 delta = position - previousPosition;
            float distance = delta.magnitude;

            if (distance >= teleportDistance)
            {
                // 瞬移：从起点到终点之间均匀补残影（不含终点，那里是角色本体）
                for (int i = 0; i < teleportGhosts; i++)
                {
                    Spawn(previousPosition + delta * ((float)i / teleportGhosts));
                }
                cooldown = interval;
            }
            else if (dt > 0f)
            {
                cooldown -= dt;
                Vector3 planar = delta;
                planar.y = 0f;
                if (planar.magnitude / dt >= speedThreshold && cooldown <= 0f)
                {
                    Spawn(previousPosition);
                    cooldown = interval;
                }
            }

            previousPosition = position;
            Fade(dt);
        }

        private void Spawn(Vector3 rootPosition)
        {
            Ghost ghost = pool[nextGhost];
            nextGhost = (nextGhost + 1) % pool.Length;

            // 把每个部件相对角色根的偏移，原样搬到残影所在的位置
            Vector3 offset = rootPosition - transform.position;
            for (int i = 0; i < sourceRenderers.Count; i++)
            {
                Renderer source = sourceRenderers[i];
                bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy;
                ghost.Renderers[i].enabled = visible;
                if (!visible)
                {
                    continue;
                }

                Transform sourceTransform = source.transform;
                ghost.Parts[i].SetPositionAndRotation(sourceTransform.position + offset, sourceTransform.rotation);

                if (source is SkinnedMeshRenderer skinned)
                {
                    // BakeMesh(useScale: true) 的结果已经包含缩放
                    skinned.BakeMesh(ghost.BakedMeshes[i], true);
                    ghost.Parts[i].localScale = Vector3.one;
                }
                else
                {
                    ghost.Parts[i].localScale = sourceTransform.lossyScale;
                }
            }

            ghost.Age = 0f;
            ghost.Active = true;
            ghost.Root.SetActive(true);
            ApplyColor(ghost, 1f);
        }

        private void Fade(float dt)
        {
            foreach (Ghost ghost in pool)
            {
                if (!ghost.Active)
                {
                    continue;
                }

                ghost.Age += dt;
                float remaining = 1f - ghost.Age / lifetime;
                if (remaining <= 0f)
                {
                    ghost.Active = false;
                    ghost.Root.SetActive(false);
                    continue;
                }

                ApplyColor(ghost, remaining * remaining);
            }
        }

        private void ApplyColor(Ghost ghost, float strength)
        {
            // 颜色保持不变，用透明度淡出
            Color tint = color * intensity;
            tint.a = strength * maxAlpha;
            block.SetColor(BaseColorId, tint);
            foreach (MeshRenderer ghostRenderer in ghost.Renderers)
            {
                ghostRenderer.SetPropertyBlock(block);
            }
        }

        private Ghost CreateGhost(int index)
        {
            int count = sourceRenderers.Count;
            var ghost = new Ghost
            {
                Root = new GameObject($"Ghost_{index}"),
                Parts = new Transform[count],
                Filters = new MeshFilter[count],
                Renderers = new MeshRenderer[count],
                BakedMeshes = new Mesh[count]
            };
            ghost.Root.transform.SetParent(poolRoot, false);

            for (int i = 0; i < count; i++)
            {
                var part = new GameObject("Part");
                part.transform.SetParent(ghost.Root.transform, false);
                ghost.Parts[i] = part.transform;
                ghost.Filters[i] = part.AddComponent<MeshFilter>();
                ghost.Renderers[i] = part.AddComponent<MeshRenderer>();
                ghost.Renderers[i].sharedMaterial = material;
                ghost.Renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghost.Renderers[i].receiveShadows = false;

                Renderer source = sourceRenderers[i];
                if (source is SkinnedMeshRenderer)
                {
                    ghost.BakedMeshes[i] = new Mesh { name = "AfterImageBake" };
                    ghost.Filters[i].sharedMesh = ghost.BakedMeshes[i];
                }
                else
                {
                    ghost.Filters[i].sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                }
            }

            ghost.Root.SetActive(false);
            return ghost;
        }
    }
}
