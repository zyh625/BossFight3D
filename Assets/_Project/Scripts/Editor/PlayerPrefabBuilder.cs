using BossFight.Character;
using BossFight.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 生成玩家预制体。结构：
    ///   Player（逻辑根：CharacterController + 所有脚本）
    ///   ├─ Visual        模型容器；有 Mixamo 人偶就用人偶，否则用胶囊体占位
    ///   ├─ Sockets       武器/特效挂点，运行时由 SocketBinder 挂到骨骼上
    ///   └─ CameraTarget  相机跟随点
    /// 每次执行都是整体重建，数值请调 ScriptableObject，不要直接改预制体上的值。
    /// </summary>
    internal static class PlayerPrefabBuilder
    {
        private const float Height = 1.8f;
        private const float Radius = 0.35f;
        private const float CameraTargetHeight = 1.5f;

        public static GameObject Build()
        {
            var root = new GameObject("Player");
            try
            {
                var controller = root.AddComponent<CharacterController>();
                controller.height = Height;
                controller.radius = Radius;
                controller.center = new Vector3(0f, Height * 0.5f, 0f);

                Transform visualRoot = CreateChild(root.transform, "Visual", Vector3.zero);
                bool usesModel = BuildVisual(visualRoot);

                Transform sockets = CreateChild(root.transform, "Sockets", Vector3.zero);
                Transform weaponR = CreateChild(sockets, "Socket_WeaponR", Vector3.zero);
                Transform weaponL = CreateChild(sockets, "Socket_WeaponL", Vector3.zero);
                Transform cameraTarget = CreateChild(
                    root.transform, "CameraTarget", new Vector3(0f, CameraTargetHeight, 0f));

                var motor = root.AddComponent<CharacterMotor>();

                var visual = root.AddComponent<CharacterVisual>();
                EditorWiring.SetRef(visual, "visualRoot", visualRoot);

                var binder = root.AddComponent<SocketBinder>();
                EditorWiring.SetRef(binder, "visual", visual);
                EditorWiring.Edit(binder, "sockets", sockets_ =>
                {
                    sockets_.arraySize = 2;
                    FillSocket(sockets_.GetArrayElementAtIndex(0), weaponR,
                        HumanBodyBones.RightHand, new Vector3(0.45f, 1.0f, 0.2f));
                    FillSocket(sockets_.GetArrayElementAtIndex(1), weaponL,
                        HumanBodyBones.LeftHand, new Vector3(-0.45f, 1.0f, 0.2f));
                });

                var animDriver = root.AddComponent<LocomotionAnimDriver>();
                EditorWiring.SetRef(animDriver, "motor", motor);
                EditorWiring.SetRef(animDriver, "visual", visual);

                var input = root.AddComponent<PlayerInputReader>();
                EditorWiring.SetRef(input, "actions",
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActions));

                var movement = root.AddComponent<PlayerMovement>();
                EditorWiring.SetRef(movement, "config", GetOrCreateMovementConfig());
                EditorWiring.SetRef(movement, "motor", motor);

                var player = root.AddComponent<PlayerController>();
                EditorWiring.SetRef(player, "input", input);
                EditorWiring.SetRef(player, "movement", movement);
                EditorWiring.SetRef(player, "cameraTarget", cameraTarget);

                SetLayerRecursively(root.transform, LayerSetup.Get(LayerSetup.Player));

                ProjectPaths.EnsureFolderFor(ProjectPaths.PlayerPrefab);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.PlayerPrefab);
                Debug.Log($"[BossFight] 已生成 {ProjectPaths.PlayerPrefab}（Visual：{(usesModel ? "Mixamo 人偶" : "占位胶囊体")}）");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <returns>是否使用了 Mixamo 人偶。</returns>
        private static bool BuildVisual(Transform visualRoot)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.MixamoCharacter);
            if (model == null)
            {
                BuildPlaceholder(visualRoot);
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visualRoot);
            instance.name = "Model";

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            // 位移全部由代码（CharacterMotor）控制，动画只负责外观
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ProjectPaths.PlayerAnimatorController);
            return true;
        }

        private static void BuildPlaceholder(Transform visualRoot)
        {
            GameObject body = EditorWiring.CreateVisualPrimitive(PrimitiveType.Capsule, "Body", visualRoot);
            body.transform.localPosition = new Vector3(0f, Height * 0.5f, 0f);
            body.transform.localScale = new Vector3(Radius * 2f, Height * 0.5f, Radius * 2f);
            body.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.PlaceholderBody;

            // 发光的"面罩"，用来看清角色朝向
            GameObject visor = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cube, "Visor", visualRoot);
            visor.transform.localPosition = new Vector3(0f, 1.5f, Radius * 0.85f);
            visor.transform.localScale = new Vector3(0.4f, 0.08f, 0.15f);
            visor.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.NeonStrip;
        }

        private static PlayerMovementConfig GetOrCreateMovementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(ProjectPaths.PlayerMovementConfig);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            ProjectPaths.EnsureFolderFor(ProjectPaths.PlayerMovementConfig);
            AssetDatabase.CreateAsset(config, ProjectPaths.PlayerMovementConfig);
            return config;
        }

        private static void FillSocket(
            SerializedProperty element, Transform socket, HumanBodyBones bone, Vector3 fallback)
        {
            element.FindPropertyRelative("socket").objectReferenceValue = socket;
            element.FindPropertyRelative("bone").intValue = (int)bone;
            element.FindPropertyRelative("fallbackLocalPosition").vector3Value = fallback;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static void SetLayerRecursively(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            foreach (Transform child in node)
            {
                SetLayerRecursively(child, layer);
            }
        }
    }
}
