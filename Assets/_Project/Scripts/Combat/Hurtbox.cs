using UnityEngine;

namespace BossFight.Combat
{
    /// <summary>
    /// 受击判定体：一个 Trigger 碰撞体 + 指向它所属角色的引用。
    /// 挂在逻辑物体（不是模型网格）上，所以换模型不影响判定范围。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private DamageReceiver owner;

        public DamageReceiver Owner => owner;
    }
}
