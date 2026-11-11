using BossFight.Character;
using BossFight.Combat;
using BossFight.Player;
using BossFight.Player.States;
using UnityEngine;

namespace BossFight.DevTools
{
    /// <summary>
    /// 胶囊体的背剑蓄力、横斩与下劈。Humanoid 模型使用手骨挂点。
    /// </summary>
    public sealed class PlaceholderWeaponSwing : MonoBehaviour
    {
        [SerializeField] private CharacterVisual visual;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private Transform socket;

        [Tooltip("挥砍扫过的角度范围：从身体一侧的 +角度 扫到另一侧的 -角度")]
        [SerializeField] private float sweepHalfAngle = 85f;
        [Tooltip("握把到身体中轴的距离（米）")]
        [SerializeField] private float reach = 0.45f;
        [Tooltip("握把离地高度（米）")]
        [SerializeField] private float height = 1.1f;
        [Tooltip("不挥砍时剑的上扬角度")]
        [SerializeField] private float restPitch = -50f;
        [SerializeField] private float overheadPitch = -104f;
        [SerializeField] private float chopEndPitch = 58f;
        [SerializeField] private float chopGripHeight = 1.32f;
        [Tooltip("轻攻击前摇时把剑向后多拉开的比例。前摇第一帧就开始动，按键才有即时反馈")]
        [SerializeField] private float cockBackFactor = 1.3f;
        [Tooltip("前摇的前多少比例用来完成拉剑动作（越小拉得越快）")]
        [SerializeField, Range(0.1f, 1f)] private float cockBackPortion = 0.6f;

        private float duration;
        private float elapsed;
        private bool swinging;
        private float fromAngle;
        private LocalHitStop hitStop;
        private PlayerController player;
        private bool chopping;

        private void Start()
        {
            hitStop = GetComponent<LocalHitStop>();
            player = GetComponent<PlayerController>();
            if (visual.HasHumanoid)
            {
                enabled = false;
                return;
            }

            fromAngle = sweepHalfAngle;
            ApplyPose(fromAngle, restPitch);
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
        }

        private void OnActivated(float activeDuration)
        {
            duration = Mathf.Max(activeDuration, 0.01f);
            elapsed = 0f;
            swinging = true;
            chopping = player != null && (player.ActiveAttack is PlayerAttackKind.Combination or PlayerAttackKind.ChargedHeavy);
            if (chopping)
            {
                ApplyChop(0f);
            }
            else
            {
                ApplyPose(fromAngle * cockBackFactor, 0f);
            }
        }

        private void OnDeactivated()
        {
            if (!swinging)
            {
                return;
            }

            // 下一刀从另一侧挥回来，连段看起来是左右交替的
            swinging = false;
            if (!chopping)
            {
                fromAngle = -fromAngle;
            }
            ApplyPose(fromAngle, restPitch);
        }

        // 在 Update 里移动挂点，Hitbox 在 LateUpdate 里检测，保证检测到的是这一帧的位置
        private void Update()
        {
            if (!swinging)
            {
                if (player != null && player.VisibleChargeRatio > 0f)
                {
                    socket.localPosition = new Vector3(reach * 0.78f, height, -reach);
                    socket.localRotation = Quaternion.Euler(-28f, 165f, 0f);
                }
                else if (player != null && player.IsAttackWindingUp
                    && (player.ActiveAttack is PlayerAttackKind.Combination or PlayerAttackKind.ChargedHeavy))
                {
                    ApplyChop(0f);
                }
                else if (player != null && player.IsAttackWindingUp
                    && (player.ActiveAttack is PlayerAttackKind.Light or PlayerAttackKind.DodgeCounter))
                {
                    // 轻攻击前摇：剑从待机姿势迅速放平并向后拉开，随后的判定阶段从这个位置挥出
                    float pull = Mathf.Clamp01(player.AttackWindupRatio / cockBackPortion);
                    pull = 1f - (1f - pull) * (1f - pull);
                    ApplyPose(Mathf.Lerp(fromAngle, fromAngle * cockBackFactor, pull), Mathf.Lerp(restPitch, 0f, pull));
                }
                else
                {
                    ApplyPose(fromAngle, restPitch);
                }
                return;
            }

            elapsed += hitStop != null ? hitStop.DeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (chopping)
            {
                ApplyChop(t);
            }
            else
            {
                ApplyPose(Mathf.Lerp(fromAngle * cockBackFactor, -fromAngle, t), 0f);
            }
        }

        private void ApplyChop(float fraction)
        {
            socket.localPosition = new Vector3(0f, chopGripHeight, reach);
            socket.localRotation = Quaternion.Euler(Mathf.Lerp(overheadPitch, chopEndPitch, fraction), 0f, 0f);
        }

        private void ApplyPose(float yaw, float pitch)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            socket.localPosition = yawRotation * new Vector3(0f, 0f, reach) + Vector3.up * height;
            socket.localRotation = yawRotation * Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
