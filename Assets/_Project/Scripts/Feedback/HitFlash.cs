using BossFight.Combat;
using UnityEngine;

namespace BossFight.Feedback
{
    /// <summary>
    /// 受击闪白：掉血时把模型短暂刷成亮色。
    /// 用 MaterialPropertyBlock 覆盖颜色，不会生成材质副本，也不会改到共用同一材质的其他物体。
    /// 只监听 DamageReceiver 的事件——删掉这个组件不影响任何玩法。
    /// </summary>
    public sealed class HitFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DamageReceiver receiver;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float duration = 0.1f;

        private MaterialPropertyBlock block;
        private float remaining;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            receiver.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            receiver.Damaged -= OnDamaged;
            Clear();
        }

        private void OnDamaged(DamageResult result)
        {
            if (result.FinalDamage <= 0f)
            {
                return;
            }

            remaining = duration;
            block.SetColor(BaseColorId, flashColor);
            foreach (Renderer target in renderers)
            {
                target.SetPropertyBlock(block);
            }
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                return;
            }

            // 用不受时间缩放影响的时间：之后加入顿帧/慢动作时，闪白的时长不变
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                Clear();
            }
        }

        private void Clear()
        {
            remaining = 0f;
            foreach (Renderer target in renderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(null);
                }
            }
        }
    }
}
