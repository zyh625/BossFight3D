using BossFight.Combat;
using BossFight.Player;
using BossFight.Player.States;
using UnityEngine;

namespace BossFight.DevTools
{
    /// <summary>
    /// 占位用的出拳表现：没有骨骼动画时，判定开启就让一只拳头向前伸出再收回，左右交替；
    /// 蓄力时右拳逐渐变大，对应"右拳凝聚光芒"。换上人形模型和拳击动画后删掉这个组件即可。
    /// </summary>
    public sealed class PlaceholderFistPunch : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private Transform leftFist;
        [SerializeField] private Transform rightFist;
        [Tooltip("出拳时拳头向前伸出的距离（米）")]
        [SerializeField, Min(0f)] private float reach = 0.7f;
        [Tooltip("满蓄力时右拳放大的倍率")]
        [SerializeField, Min(1f)] private float chargedScale = 2.2f;

        private Vector3 leftRest;
        private Vector3 rightRest;
        private Vector3 baseScale;
        private Transform punching;
        private Vector3 punchRest;
        private float duration;
        private float elapsed;
        private bool useLeft;

        private void Awake()
        {
            leftRest = leftFist.localPosition;
            rightRest = rightFist.localPosition;
            baseScale = rightFist.localScale;
        }

        private void OnEnable()
        {
            hitbox.Activated += OnActivated;
        }

        private void OnDisable()
        {
            hitbox.Activated -= OnActivated;
        }

        private void OnActivated(float activeDuration)
        {
            // 蓄力冲拳固定用右拳，普通连打左右交替
            bool heavy = player.ActiveAttack == PlayerAttackKind.ChargedHeavy;
            useLeft = !heavy && !useLeft;

            punching = useLeft ? leftFist : rightFist;
            punchRest = useLeft ? leftRest : rightRest;
            duration = Mathf.Max(activeDuration * 2f, 0.05f);
            elapsed = 0f;
        }

        private void Update()
        {
            float charge = player.VisibleChargeRatio;
            rightFist.localScale = baseScale * Mathf.Lerp(1f, chargedScale, charge);

            if (punching == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 0→1→0：前半程伸出，后半程收回
            float extend = Mathf.Sin(t * Mathf.PI);
            punching.localPosition = punchRest + Vector3.forward * (reach * extend);

            if (t >= 1f)
            {
                punching.localPosition = punchRest;
                punching = null;
            }
        }
    }
}
