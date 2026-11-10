using BossFight.Character;
using BossFight.Combat;
using BossFight.DevTools;
using BossFight.Feedback;
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
    ///   │   └─ Socket_WeaponR / Weapon_Sword（剑的外观 + Hitbox）
    ///   └─ CameraTarget  相机跟随点
    /// 每次执行都是整体重建：数值请调 ScriptableObject；
    /// 唯一会保留的手动调整是 Socket 的本地位置/旋转（握剑姿势）。
    /// </summary>
    internal static class PlayerPrefabBuilder
    {
        private const float Height = 1.8f;
        private const float Radius = 0.35f;
        private const float CameraTargetHeight = 1.5f;

        private const string WeaponRName = "Socket_WeaponR";
        private const string WeaponLName = "Socket_WeaponL";

        private const float BladeLength = 1.0f;
        private const float GripLength = 0.18f;

        public static GameObject Build()
        {
            SocketPose savedWeaponR = SocketPose.Load(WeaponRName);
            SocketPose savedWeaponL = SocketPose.Load(WeaponLName);

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
                Transform weaponR = CreateChild(sockets, WeaponRName, Vector3.zero);
                Transform weaponL = CreateChild(sockets, WeaponLName, Vector3.zero);
                savedWeaponR.ApplyTo(weaponR);
                savedWeaponL.ApplyTo(weaponL);

                Transform cameraTarget = CreateChild(
                    root.transform, "CameraTarget", new Vector3(0f, CameraTargetHeight, 0f));

                Hitbox weaponHitbox = BuildSword(weaponR);

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

                var stamina = root.AddComponent<Stamina>();

                var input = root.AddComponent<PlayerInputReader>();
                EditorWiring.SetRef(input, "actions",
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectPaths.InputActions));

                var movement = root.AddComponent<PlayerMovement>();
                EditorWiring.SetRef(movement, "config", GetOrCreateMovementConfig());
                EditorWiring.SetRef(movement, "motor", motor);

                var player = root.AddComponent<PlayerController>();
                EditorWiring.SetRef(player, "input", input);
                EditorWiring.SetRef(player, "movement", movement);
                EditorWiring.SetRef(player, "motor", motor);
                EditorWiring.SetRef(player, "visual", visual);
                EditorWiring.SetRef(player, "stamina", stamina);
                EditorWiring.SetRef(player, "weaponHitbox", weaponHitbox);
                EditorWiring.SetRef(player, "combatConfig", CombatDataBuilder.GetOrCreatePlayerCombatConfig());
                EditorWiring.SetRef(player, "cameraTarget", cameraTarget);

                // 没有骨骼动画时用程序化挥剑代替；有 Humanoid 模型时它会在运行时自动停用
                var swing = root.AddComponent<PlaceholderWeaponSwing>();
                EditorWiring.SetRef(swing, "visual", visual);
                EditorWiring.SetRef(swing, "hitbox", weaponHitbox);
                EditorWiring.SetRef(swing, "socket", weaponR);

                SetLayerRecursively(root.transform, LayerSetup.Get(LayerSetup.Player));
                AddHurtbox(root);
                WireM3(root);

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

        internal static void WireM3(GameObject root)
        {
            if (!root.TryGetComponent<LockOnTargeting>(out _))
            {
                var targeting = root.AddComponent<LockOnTargeting>();
                EditorWiring.Edit(targeting, "occlusionLayers",
                    p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Environment));
            }

            if (!root.TryGetComponent<HitFlash>(out _))
            {
                var flash = root.AddComponent<HitFlash>();
                var renderers = root.GetComponent<CharacterVisual>().VisualRoot.GetComponentsInChildren<Renderer>(true);
                EditorWiring.SetRef(flash, "receiver", root.GetComponent<DamageReceiver>());
                EditorWiring.Edit(flash, "renderers", p =>
                {
                    p.arraySize = renderers.Length;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                    }
                });
            }

            CombatFeedbackBuilder.AddImpact(root);
        }

        private static void AddHurtbox(GameObject root)
        {
            var receiver = root.GetComponent<DamageReceiver>();
            if (receiver == null)
            {
                receiver = root.AddComponent<DamageReceiver>();
            }

            if (root.GetComponentInChildren<Hurtbox>(true) != null)
            {
                return;
            }

            var controller = root.GetComponent<CharacterController>();
            Transform hurtbox = CreateChild(root.transform, "Hurtbox", Vector3.zero);
            hurtbox.gameObject.layer = LayerSetup.Get(LayerSetup.Hurtbox);
            var trigger = hurtbox.gameObject.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.center = controller.center;
            trigger.height = controller.height;
            trigger.radius = controller.radius;
            EditorWiring.SetRef(hurtbox.gameObject.AddComponent<Hurtbox>(), "owner", receiver);
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

        /// <summary>
        /// 占位剑：握把在挂点原点，剑身沿挂点的 +Z 方向。Hitbox 的刀根/刀尖是两个空物体，
        /// 以后换成正式的剑模型时，只要保持"原点在握把、剑身朝 +Z"就不用改判定。
        /// </summary>
        private static Hitbox BuildSword(Transform socket)
        {
            Transform sword = CreateChild(socket, "Weapon_Sword", Vector3.zero);

            GameObject grip = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cube, "Grip", sword);
            grip.transform.localPosition = new Vector3(0f, 0f, GripLength * 0.5f);
            grip.transform.localScale = new Vector3(0.05f, 0.05f, GripLength);
            grip.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.PlaceholderBody;

            GameObject blade = EditorWiring.CreateVisualPrimitive(PrimitiveType.Cube, "Blade", sword);
            blade.transform.localPosition = new Vector3(0f, 0f, GripLength + BladeLength * 0.5f);
            blade.transform.localScale = new Vector3(0.025f, 0.09f, BladeLength);
            blade.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.NeonStrip;

            Transform basePoint = CreateChild(sword, "Hitbox_Base", new Vector3(0f, 0f, GripLength));
            Transform tipPoint = CreateChild(sword, "Hitbox_Tip", new Vector3(0f, 0f, GripLength + BladeLength));

            var hitbox = sword.gameObject.AddComponent<Hitbox>();
            EditorWiring.SetRef(hitbox, "basePoint", basePoint);
            EditorWiring.SetRef(hitbox, "tipPoint", tipPoint);
            EditorWiring.Edit(hitbox, "radius", p => p.floatValue = 0.18f);
            EditorWiring.Edit(hitbox, "hurtboxLayers", p => p.intValue = 1 << LayerSetup.Get(LayerSetup.Hurtbox));
            return hitbox;
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

        /// <summary>
        /// Socket 的本地位置/旋转 = 武器相对手骨的偏移，是需要你手动调的握剑姿势。
        /// 重建预制体前先从旧预制体里读出来，重建后写回去，这样调好的姿势不会丢。
        /// </summary>
        private readonly struct SocketPose
        {
            private readonly bool valid;
            private readonly Vector3 position;
            private readonly Quaternion rotation;

            private SocketPose(Vector3 position, Quaternion rotation)
            {
                valid = true;
                this.position = position;
                this.rotation = rotation;
            }

            public static SocketPose Load(string socketName)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PlayerPrefab);
                if (existing == null)
                {
                    return default;
                }

                foreach (Transform node in existing.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == socketName)
                    {
                        return new SocketPose(node.localPosition, node.localRotation);
                    }
                }

                return default;
            }

            public void ApplyTo(Transform socket)
            {
                if (valid)
                {
                    socket.localPosition = position;
                    socket.localRotation = rotation;
                }
            }
        }
    }
}
