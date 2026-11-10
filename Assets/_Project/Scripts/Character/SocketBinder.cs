using System;
using UnityEngine;

namespace BossFight.Character
{
    /// <summary>
    /// 启动时把 Socket（武器挂点、特效挂点）挂到对应的 Humanoid 骨骼下。
    /// Socket 在预制体里是逻辑根的子物体，不属于模型，所以换模型不会丢；
    /// 它在预制体里的本地坐标/旋转，挂上骨骼后就是相对骨骼的偏移（用来调握剑姿势）。
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class SocketBinder : MonoBehaviour
    {
        [Serializable]
        private struct SocketBinding
        {
            public Transform socket;
            public HumanBodyBones bone;

            [Tooltip("模型没有 Humanoid 骨骼（占位胶囊体）时，Socket 相对角色根的位置")]
            public Vector3 fallbackLocalPosition;
        }

        [SerializeField] private CharacterVisual visual;
        [SerializeField] private SocketBinding[] sockets = Array.Empty<SocketBinding>();

        private void Awake()
        {
            foreach (SocketBinding binding in sockets)
            {
                if (binding.socket == null)
                {
                    continue;
                }

                Transform bone = visual.GetBone(binding.bone);
                if (bone != null)
                {
                    binding.socket.SetParent(bone, false);
                }
                else
                {
                    binding.socket.SetParent(transform, false);
                    binding.socket.localPosition = binding.fallbackLocalPosition;
                }
            }
        }
    }
}
