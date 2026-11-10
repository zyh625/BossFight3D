using BossFight.CameraRig;
using BossFight.Combat;
using BossFight.DevTools;
using BossFight.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 生成 Boss 战场景（灰盒版）：圆形封闭场地 + 灯光/后处理 + 相机 + 玩家。
    /// 每次执行都是整体重建并覆盖 Arena.unity，不要在这个场景里手动摆放需要保留的东西。
    /// </summary>
    internal static class ArenaSceneBuilder
    {
        private const float ArenaRadius = 24f;
        private const float WallHeight = 6f;
        private const float WallThickness = 1f;
        private const int WallSegments = 48;

        private const float CameraDistance = 4.5f;
        private const float CameraFov = 55f;

        public static void Build()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PlayerPrefab);
            if (playerPrefab == null)
            {
                playerPrefab = PlayerPrefabBuilder.Build();
            }


            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildArena();
            BuildLighting();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0f, -6f);

            BuildCameras(player.GetComponent<PlayerController>(), player.GetComponent<PlayerInputReader>());
            BuildTrainingDummy(player);

            ProjectPaths.EnsureFolderFor(ProjectPaths.ArenaScene);
            EditorSceneManager.SaveScene(scene, ProjectPaths.ArenaScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectPaths.ArenaScene, true) };
            Debug.Log($"[BossFight] 已生成 {ProjectPaths.ArenaScene}");
        }

        private static void BuildArena()
        {
            int environment = LayerSetup.Get(LayerSetup.Environment);
            Transform arena = new GameObject("Arena").transform;

            float floorSize = (ArenaRadius + WallThickness) * 2f + 2f;
            GameObject floor = CreateBlock("Floor", arena, environment, ArenaMaterials.Floor, true);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(floorSize, 1f, floorSize);

            // 用一圈长方体围成圆形围墙，每段墙的顶部和底部各有一条霓虹灯带
            float segmentAngle = 360f / WallSegments;
            float segmentWidth = 2f * (ArenaRadius + WallThickness) * Mathf.Tan(Mathf.PI / WallSegments) + 0.02f;
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(arena, false);

            for (int i = 0; i < WallSegments; i++)
            {
                Quaternion rotation = Quaternion.Euler(0f, segmentAngle * i, 0f);
                Vector3 outward = rotation * Vector3.forward;

                Transform segment = new GameObject($"Segment_{i:00}").transform;
                segment.SetParent(walls, false);
                segment.SetPositionAndRotation(outward * (ArenaRadius + WallThickness * 0.5f), rotation);

                GameObject wall = CreateBlock("Wall", segment, environment, ArenaMaterials.Wall, true);
                wall.transform.localPosition = new Vector3(0f, WallHeight * 0.5f, 0f);
                wall.transform.localScale = new Vector3(segmentWidth, WallHeight, WallThickness);

                // 每 4 段换一次强调色，给场地一点方向感
                Material neon = i % 4 == 0 ? ArenaMaterials.NeonAccent : ArenaMaterials.NeonStrip;
                float stripZ = -WallThickness * 0.5f - 0.03f;
                CreateStrip("Strip_Top", segment, environment, neon,
                    new Vector3(0f, WallHeight - 0.5f, stripZ), new Vector3(segmentWidth, 0.14f, 0.06f));
                CreateStrip("Strip_Bottom", segment, environment, neon,
                    new Vector3(0f, 0.08f, stripZ), new Vector3(segmentWidth, 0.1f, 0.06f));

                // 每隔一段墙加一根竖向灯柱，打破大面积的纯黑墙面
                if (i % 2 == 0)
                {
                    CreateStrip("Strip_Pillar", segment, environment, neon,
                        new Vector3(0f, WallHeight * 0.5f, stripZ), new Vector3(0.08f, WallHeight - 1.2f, 0.06f));
                }
            }

            BuildFloorRings(arena, environment);
        }

        /// <summary>地面上的同心发光环：给玩家一个判断距离的参照（内圈约等于 Boss 近战范围）。</summary>
        private static void BuildFloorRings(Transform arena, int layer)
        {
            Transform rings = new GameObject("FloorRings").transform;
            rings.SetParent(arena, false);

            const int segments = 72;
            float[] radii = { 3.2f, 9f, 16f };
            foreach (float radius in radii)
            {
                float length = 2f * radius * Mathf.Tan(Mathf.PI / segments) + 0.01f;
                for (int i = 0; i < segments; i++)
                {
                    Quaternion rotation = Quaternion.Euler(0f, 360f / segments * i, 0f);
                    GameObject piece = CreateBlock($"Ring_{radius:0}_{i:00}", rings, layer, ArenaMaterials.NeonStrip, false);
                    piece.transform.SetPositionAndRotation(rotation * Vector3.forward * radius + Vector3.up * 0.005f, rotation);
                    piece.transform.localScale = new Vector3(length, 0.01f, 0.04f);
                    piece.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        private static void BuildLighting()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.65f, 0.75f, 1f);
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;

            // 深色基调：不用天空盒，环境光压到很暗，亮度主要来自自发光 + Bloom
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.035f);
            RenderSettings.fogDensity = 0.015f;

            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetOrCreateVolumeProfile();
        }

        private static VolumeProfile GetOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProjectPaths.ArenaVolumeProfile);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            ProjectPaths.EnsureFolderFor(ProjectPaths.ArenaVolumeProfile);
            AssetDatabase.CreateAsset(profile, ProjectPaths.ArenaVolumeProfile);

            var bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.7f);

            var tonemapping = AddOverride<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.4f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            // 后处理组件要作为子资源存进 Profile 文件，否则重开工程后会丢
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void BuildCameras(PlayerController player, PlayerInputReader input)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CinemachineBrain>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            // Cinemachine 3：一个 CinemachineCamera + 位置组件（OrbitalFollow）+ 朝向组件（RotationComposer）
            var rig = new GameObject("CM FreeLook");
            var cmCamera = rig.AddComponent<CinemachineCamera>();
            cmCamera.Follow = player.CameraTarget;
            cmCamera.Lens.FieldOfView = CameraFov;

            var orbital = rig.AddComponent<CinemachineOrbitalFollow>();
            orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbital.Radius = CameraDistance;
            orbital.TrackerSettings.PositionDamping = new Vector3(0.15f, 0.15f, 0.15f);
            orbital.VerticalAxis.Range = new Vector2(-20f, 65f);
            orbital.VerticalAxis.Center = 15f;
            orbital.VerticalAxis.Value = 15f;

            var composer = rig.AddComponent<CinemachineRotationComposer>();
            composer.Damping = Vector2.zero; // 朝向不做阻尼，鼠标转视角才跟手

            // 相机和角色之间有墙时把相机往前推，避免穿墙
            var deoccluder = rig.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = 1 << LayerSetup.Get(LayerSetup.Environment);

            var orbitInput = rig.AddComponent<OrbitCameraInput>();
            EditorWiring.SetRef(orbitInput, "input", input);
            EditorWiring.SetRef(orbitInput, "orbital", orbital);
            WireCombatCamera(orbitInput, player.GetComponent<LockOnTargeting>());

            rig.AddComponent<CursorLocker>();
        }

        internal static void WireCombatCamera(OrbitCameraInput orbitInput, LockOnTargeting targeting)
        {
            var cmCamera = orbitInput.GetComponent<CinemachineCamera>();
            Transform aim = cmCamera.LookAt;
            if (aim == null || aim == cmCamera.Follow)
            {
                aim = new GameObject("CombatAim").transform;
                aim.position = cmCamera.Follow.position;
            }

            cmCamera.LookAt = aim;
            EditorWiring.SetRef(orbitInput, "targeting", targeting);
            EditorWiring.SetRef(orbitInput, "lockAim", aim);

            if (!orbitInput.TryGetComponent<CinemachineImpulseListener>(out _))
            {
                orbitInput.gameObject.AddComponent<CinemachineImpulseListener>();
            }
        }

        /// <summary>场地中央放一个训练木桩，并接好调试显示。M4 起这个位置换成 Boss。</summary>
        private static void BuildTrainingDummy(GameObject player)
        {
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.DummyPrefab);
            if (dummyPrefab == null)
            {
                dummyPrefab = DummyPrefabBuilder.Build();
            }

            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummy.transform.position = Vector3.zero;
            EditorWiring.SetRef(dummy.GetComponent<TrainingDummy>(), "sparringTarget", player.GetComponent<DamageReceiver>());

            var overlay = new GameObject("DevTools").AddComponent<CombatDebugOverlay>();
            EditorWiring.SetRef(overlay, "player", player.GetComponent<PlayerController>());
            EditorWiring.SetRef(overlay, "playerStamina", player.GetComponent<Stamina>());
            EditorWiring.SetRef(overlay, "target", dummy.GetComponent<DamageReceiver>());
        }

        private static GameObject CreateBlock(
            string name, Transform parent, int layer, Material material, bool keepCollider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.layer = layer;
            block.isStatic = true;
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Object.DestroyImmediate(block.GetComponent<Collider>());
            }

            return block;
        }

        private static void CreateStrip(
            string name, Transform parent, int layer, Material material, Vector3 localPosition, Vector3 localScale)
        {
            GameObject strip = CreateBlock(name, parent, layer, material, false);
            strip.transform.localPosition = localPosition;
            strip.transform.localScale = localScale;
            strip.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
