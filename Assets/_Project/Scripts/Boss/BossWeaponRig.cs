using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>灰盒双剑姿态和共用扫掠轴。交叉斩的一次扫掠只结算一次。</summary>
    public sealed class BossWeaponRig : MonoBehaviour
    {
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform visual;
        [SerializeField] private Transform leftSword;
        [SerializeField] private Transform rightSword;
        [SerializeField] private Transform sweepPivot;
        [SerializeField] private Renderer signal;
        [SerializeField] private Hitbox hitbox;

        private MaterialPropertyBlock colorBlock;
        private bool phaseTwo;

        public Hitbox Hitbox => hitbox;

        private void Awake() => colorBlock = new MaterialPropertyBlock();

        public void Rest()
        {
            visual.localRotation = Quaternion.identity;
            leftSword.localPosition = new Vector3(-0.7f, 1.18f, 0.15f);
            rightSword.localPosition = new Vector3(0.7f, 1.18f, 0.15f);
            leftSword.localRotation = Quaternion.Euler(-26f, -30f, 0f);
            rightSword.localRotation = Quaternion.Euler(-26f, 30f, 0f);
            Signal(phaseTwo ? new Color(1f, 0.07f, 0.6f) * 4f : new Color(0.06f, 0.85f, 1f) * 2f);
        }

        public void SetPhaseTwo() => phaseTwo = true;

        public void WaveCharge(float fraction)
        {
            Pose(rightSword, 72f, 32f);
            Signal(new Color(0.12f, 0.9f, 1f) * Mathf.Lerp(2f, 7f, fraction));
        }

        public void Windup(int index, float fraction)
        {
            if (index != 1)
            {
                Pose(leftSword, -78f, Mathf.Lerp(-25f, -65f, fraction));
            }
            if (index != 0)
            {
                Pose(rightSword, 78f, Mathf.Lerp(-25f, -65f, fraction));
            }
            Signal(new Color(1f, 0.17f, 0.03f) * Mathf.Lerp(2f, index >= 2 ? 9f : 5f, fraction));
        }

        public void Swing(int index, float fraction)
        {
            float angle = Mathf.Lerp(-78f, 78f, fraction);
            if (index != 1)
            {
                Pose(leftSword, angle, index >= 2 ? Mathf.Lerp(-38f, 22f, fraction) : 0f);
            }
            if (index != 0)
            {
                Pose(rightSword, -angle, index >= 2 ? Mathf.Lerp(-38f, 22f, fraction) : 0f);
            }
            Pose(sweepPivot, index == 1 ? -angle : angle, 0f);
            Signal(new Color(1f, 0.8f, 0.2f) * 6f);
        }

        public void Stagger(bool broken)
        {
            Rest();
            visual.localRotation = Quaternion.Euler(broken ? 23f : 12f, 0f, 0f);
            Signal(new Color(0.15f, 0.8f, 1f) * (broken ? 5f : 2f));
        }

        public void Fall()
        {
            visual.localRotation = Quaternion.Euler(76f, 0f, 0f);
            Signal(Color.black);
        }

        private static void Pose(Transform part, float yaw, float pitch)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            part.localPosition = Vector3.up * 1.24f + rotation * (Vector3.forward * 0.65f);
            part.localRotation = rotation * Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Signal(Color emission)
        {
            colorBlock.SetColor(Emission, emission);
            signal.SetPropertyBlock(colorBlock);
        }
    }
}
