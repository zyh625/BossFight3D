using BossFight.Combat;
using UnityEngine;

namespace BossFight.Boss
{
    /// <summary>
    /// 占位阶段的 Boss 武器姿势：没有骨骼动画时，用代码直接摆放两把剑和判定扫掠轴。
    /// 招式状态只调用"蓄势 / 挥砍 / 旋转 / 合剑"这些语义化的方法，不关心剑具体怎么摆；
    /// 换上正式模型和动画之后，这个类整体被动画取代，招式状态不用改。
    ///
    /// 长剑形态下左手剑隐藏，右手剑和判定扫掠轴按比例加长，所有姿势改为单剑版本。
    /// </summary>
    public sealed class BossWeaponRig : MonoBehaviour
    {
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        // 长剑连招的第 4 击（下标 3）是自上而下的慢刀
        private const int LongswordFinisherIndex = 3;

        [SerializeField] private Transform visual;
        [SerializeField] private Transform leftSword;
        [SerializeField] private Transform rightSword;
        [SerializeField] private Transform sweepPivot;
        [SerializeField] private Renderer signal;
        [SerializeField] private Hitbox hitbox;

        private MaterialPropertyBlock colorBlock;
        private bool phaseTwo;
        private bool longsword;

        public Hitbox Hitbox => hitbox;
        public bool IsLongsword => longsword;

        private void Awake() => colorBlock = new MaterialPropertyBlock();

        public void SetPhaseTwo() => phaseTwo = true;

        /// <summary>切换长剑形态。reachScale 是长剑相对普通剑的长度倍率，判定范围同步放大。</summary>
        public void SetLongsword(bool enabled, float reachScale)
        {
            longsword = enabled;
            leftSword.gameObject.SetActive(!enabled);
            rightSword.localScale = enabled ? new Vector3(1.7f, 1.7f, reachScale) : Vector3.one;
            sweepPivot.localScale = enabled ? new Vector3(1f, 1f, reachScale) : Vector3.one;
            Rest();
        }

        public void Rest()
        {
            visual.localRotation = Quaternion.identity;
            if (longsword)
            {
                rightSword.localPosition = new Vector3(0.3f, 1.25f, 0.35f);
                rightSword.localRotation = Quaternion.Euler(-40f, 8f, 0f);
            }
            else
            {
                leftSword.localPosition = new Vector3(-0.7f, 1.18f, 0.15f);
                rightSword.localPosition = new Vector3(0.7f, 1.18f, 0.15f);
                leftSword.localRotation = Quaternion.Euler(-26f, -30f, 0f);
                rightSword.localRotation = Quaternion.Euler(-26f, 30f, 0f);
            }

            Signal(phaseTwo ? new Color(1f, 0.07f, 0.6f) * 4f : new Color(0.06f, 0.85f, 1f) * 2f);
        }

        public void WaveCharge(float fraction)
        {
            Pose(rightSword, 72f, 32f);
            Signal(new Color(0.12f, 0.9f, 1f) * Mathf.Lerp(2f, 7f, fraction));
        }

        public void Windup(int index, float fraction)
        {
            if (longsword)
            {
                if (index >= LongswordFinisherIndex)
                {
                    Pose(rightSword, 0f, Mathf.Lerp(-40f, -115f, fraction));
                }
                else
                {
                    Pose(rightSword, 78f * SweepSign(index), Mathf.Lerp(-25f, -60f, fraction));
                }

                Signal(new Color(1f, 0.17f, 0.03f) * Mathf.Lerp(2f, index >= LongswordFinisherIndex ? 10f : 5f, fraction));
                return;
            }

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
            if (longsword)
            {
                if (index >= LongswordFinisherIndex)
                {
                    // 竖劈：剑从头顶劈到身前，判定在正前方一个较窄的扇形里扫过
                    Pose(rightSword, 0f, Mathf.Lerp(-115f, 35f, fraction));
                    Pose(sweepPivot, Mathf.Lerp(-28f, 28f, fraction), 0f);
                }
                else
                {
                    float yaw = Mathf.Lerp(78f, -78f, fraction) * SweepSign(index);
                    Pose(rightSword, yaw, 0f);
                    Pose(sweepPivot, yaw, 0f);
                }

                Signal(new Color(1f, 0.8f, 0.2f) * 6f);
                return;
            }

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

        /// <summary>旋风斩蓄势：剑向两侧平举，信号灯由暗变亮（绿色，区别于普通攻击的红色）。</summary>
        public void WhirlWindup(float fraction)
        {
            if (longsword)
            {
                Pose(rightSword, 90f, -8f);
            }
            else
            {
                Pose(leftSword, -90f, -8f);
                Pose(rightSword, 90f, -8f);
            }

            visual.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -40f, fraction), 0f);
            Signal(new Color(0.35f, 1f, 0.25f) * Mathf.Lerp(2f, 9f, fraction));
        }

        /// <summary>旋风斩旋转中：身体、剑和判定轴一起绕竖直轴转到指定角度。</summary>
        public void Whirl(float angle)
        {
            visual.localRotation = Quaternion.Euler(0f, angle, 0f);
            Pose(rightSword, angle, 0f);
            if (!longsword)
            {
                Pose(leftSword, angle + 180f, 0f);
            }

            Pose(sweepPivot, angle, 0f);
            Signal(new Color(0.35f, 1f, 0.25f) * 7f);
        }

        /// <summary>旋风斩收招：转晕了，身体歪斜、信号灯变暗。</summary>
        public void Dizzy()
        {
            Rest();
            visual.localRotation = Quaternion.Euler(10f, 0f, 9f);
            Signal(new Color(0.35f, 1f, 0.25f) * 1.2f);
        }

        /// <summary>合剑演出：两把剑向身前中线并拢，信号灯变白。</summary>
        public void Merge(float fraction)
        {
            float yaw = Mathf.Lerp(42f, 0f, fraction);
            float pitch = Mathf.Lerp(-30f, -75f, fraction);
            if (leftSword.gameObject.activeSelf)
            {
                Pose(leftSword, -yaw, pitch);
            }
            Pose(rightSword, yaw, pitch);
            Signal(Color.white * Mathf.Lerp(2f, 10f, fraction));
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

        // 长剑的快刀左右交替：第 0、2 刀从右向左，第 1 刀从左向右
        private static float SweepSign(int index) => index % 2 == 0 ? 1f : -1f;

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
