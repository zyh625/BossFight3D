using System.Collections.Generic;
using BossFight.Combat;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 刀光拖尾：判定开启期间，每帧记录刀根和刀尖的位置，把这些采样连成一条带状网格。
    ///
    /// 为什么不用 TrailRenderer：它只跟踪一个点，生成的是朝向相机的带子；
    /// 刀光需要的是"刀根—刀尖"这条线段扫过的曲面，所以自己生成网格。
    ///
    /// 两个细节：
    /// 1. 采样之间用 Catmull-Rom 样条插值。0.1 秒的挥砍在 60 帧下只有 6 个采样，直接连线是折线。
    /// 2. 透明度随采样的"年龄"衰减，判定结束后拖尾自然淡出，而不是突然消失。
    /// </summary>
    public sealed class WeaponTrail : MonoBehaviour
    {
        private struct Sample
        {
            public Vector3 Base;
            public Vector3 Tip;
            public float Time;
        }

        [SerializeField] private Hitbox hitbox;
        [SerializeField] private Transform basePoint;
        [SerializeField] private Transform tipPoint;
        [SerializeField] private Material material;
        [SerializeField] private Color color = new Color(0.3f, 0.95f, 1f);
        [Tooltip("亮度")]
        [SerializeField, Range(0f, 1f)] private float brightness = 1f;

        [Tooltip("每个采样存活多久（秒），决定拖尾的长度")]
        [SerializeField, Min(0.02f)] private float lifetime = 0.16f;
        [Tooltip("相邻两个采样之间插值出几段，越大越平滑")]
        [SerializeField, Range(1, 6)] private int subdivisions = 4;
        [Tooltip("刀根一侧的透明度（刀尖为 1），让拖尾向内渐隐")]
        [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.15f;

        private readonly List<Sample> samples = new List<Sample>(64);
        private readonly List<Vector3> vertices = new List<Vector3>(512);
        private readonly List<Color32> colors = new List<Color32>(512);
        private readonly List<int> triangles = new List<int>(1536);

        private GameObject meshObject;
        private Mesh mesh;
        private bool emitting;

        private void Awake()
        {
            // 网格顶点直接用世界坐标，所以承载它的物体放在场景根部且不带任何变换
            meshObject = new GameObject($"{name}_WeaponTrail");
            mesh = new Mesh { name = "WeaponTrail" };
            mesh.MarkDynamic();
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void OnEnable()
        {
            hitbox.Activated += OnActivated;
            hitbox.Deactivated += OnDeactivated;
        }

        private void OnDisable()
        {
            hitbox.Activated -= OnActivated;
            hitbox.Deactivated -= OnDeactivated;
            emitting = false;
            samples.Clear();
            if (mesh != null)
            {
                mesh.Clear();
            }
        }

        private void OnDestroy()
        {
            Destroy(mesh);
            Destroy(meshObject);
        }

        private void OnActivated(float activeDuration)
        {
            emitting = true;
        }

        private void OnDeactivated()
        {
            emitting = false;
        }

        // LateUpdate：动画和占位挥剑都已经把武器移到了这一帧的最终位置
        private void LateUpdate()
        {
            float now = Time.time;

            if (emitting)
            {
                samples.Add(new Sample { Base = basePoint.position, Tip = tipPoint.position, Time = now });
            }

            int expired = 0;
            while (expired < samples.Count && now - samples[expired].Time > lifetime)
            {
                expired++;
            }
            if (expired > 0)
            {
                samples.RemoveRange(0, expired);
            }

            if (samples.Count < 2)
            {
                if (mesh.vertexCount > 0)
                {
                    mesh.Clear();
                }
                return;
            }

            Rebuild(now);
        }

        private void Rebuild(float now)
        {
            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            int last = samples.Count - 1;
            for (int i = 0; i < last; i++)
            {
                Sample p0 = samples[Mathf.Max(i - 1, 0)];
                Sample p1 = samples[i];
                Sample p2 = samples[i + 1];
                Sample p3 = samples[Mathf.Min(i + 2, last)];

                // 每一段只输出起点一侧的插值点，最后一段额外补上终点，避免重复顶点
                int steps = i == last - 1 ? subdivisions + 1 : subdivisions;
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / subdivisions;
                    float age = now - Mathf.Lerp(p1.Time, p2.Time, t);
                    AddColumn(
                        CatmullRom(p0.Base, p1.Base, p2.Base, p3.Base, t),
                        CatmullRom(p0.Tip, p1.Tip, p2.Tip, p3.Tip, t),
                        1f - Mathf.Clamp01(age / lifetime));
                }
            }

            int columns = vertices.Count / 2;
            for (int c = 0; c < columns - 1; c++)
            {
                int v = c * 2;
                triangles.Add(v);
                triangles.Add(v + 1);
                triangles.Add(v + 2);
                triangles.Add(v + 1);
                triangles.Add(v + 3);
                triangles.Add(v + 2);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            // 顶点是世界坐标且每帧都在变，给一个足够大的包围盒，省掉每帧重算并避免被错误剔除
            mesh.bounds = new Bounds(basePoint.position, Vector3.one * 20f);
        }

        private void AddColumn(Vector3 basePosition, Vector3 tipPosition, float fade)
        {
            // 平方衰减：新的部分亮，尾部快速变淡
            float alpha = fade * fade;
            vertices.Add(basePosition);
            vertices.Add(tipPosition);
            colors.Add(WithAlpha(alpha * baseAlpha));
            colors.Add(WithAlpha(alpha));
        }

        private Color32 WithAlpha(float alpha)
        {
            Color result = color * brightness;
            result.a = alpha;
            return result;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1
                           + (p2 - p0) * t
                           + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                           + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
