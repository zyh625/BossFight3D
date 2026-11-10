using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 一圈向外扩散并淡出的光环（识破、完美闪避、破防重击时出现）。
    /// 环形网格由代码生成，所有光环共用同一份；每个光环只改自己的缩放和颜色。
    /// </summary>
    public sealed class PulseRing : MonoBehaviour
    {
        private const int Segments = 48;
        private const float InnerRatio = 0.86f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Mesh sharedMesh;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock block;
        private Color color;
        private float startRadius;
        private float endRadius;
        private float duration;
        private float elapsed;

        public static PulseRing Create(Material material, Transform parent)
        {
            var go = new GameObject("PulseRing");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetMesh();

            var ring = go.AddComponent<PulseRing>();
            ring.meshRenderer = go.AddComponent<MeshRenderer>();
            ring.meshRenderer.sharedMaterial = material;
            ring.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.block = new MaterialPropertyBlock();
            go.SetActive(false);
            return ring;
        }

        /// <param name="normal">光环所在平面的法线：贴地用 Vector3.up，朝向相机的冲击环用受击方向。</param>
        public void Play(Vector3 position, Vector3 normal, Color ringColor, float fromRadius, float toRadius, float seconds)
        {
            color = ringColor;
            startRadius = fromRadius;
            endRadius = toRadius;
            duration = Mathf.Max(seconds, 0.01f);
            elapsed = 0f;

            transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, normal));
            gameObject.SetActive(true);
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (t >= 1f)
            {
                gameObject.SetActive(false);
                return;
            }

            Apply(t);
        }

        private void Apply(float t)
        {
            // 先快后慢地扩散（ease-out），亮度线性衰减到 0
            float eased = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(startRadius, endRadius, eased);
            transform.localScale = new Vector3(radius, 1f, radius);

            Color faded = color * (1f - t);
            block.SetColor(BaseColorId, faded);
            meshRenderer.SetPropertyBlock(block);
        }

        /// <summary>半径为 1 的扁平圆环，位于 XZ 平面。</summary>
        private static Mesh GetMesh()
        {
            if (sharedMesh != null)
            {
                return sharedMesh;
            }

            var vertices = new Vector3[Segments * 2];
            var colors = new Color32[Segments * 2];
            var triangles = new int[Segments * 6];

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * InnerRatio;
                vertices[i * 2 + 1] = direction;
                // 内沿透明、外沿不透明，光环看起来有一条清晰的前沿
                colors[i * 2] = new Color32(255, 255, 255, 0);
                colors[i * 2 + 1] = new Color32(255, 255, 255, 255);

                int next = (i + 1) % Segments;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = next * 2 + 1;
                triangles[t + 5] = next * 2;
            }

            sharedMesh = new Mesh { name = "PulseRing", vertices = vertices, colors32 = colors, triangles = triangles };
            sharedMesh.RecalculateBounds();
            return sharedMesh;
        }
    }
}
