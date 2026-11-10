using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 一键搭建入口（菜单 Tools/BossFight）。
    /// 每一步都可以重复执行；Build All 也能在命令行批处理模式下通过 -executeMethod 调用。
    /// </summary>
    public static class BuildMenu
    {
        private const string Menu = "Tools/BossFight/";

        [MenuItem(Menu + "Build All（全部重建）", priority = 0)]
        public static void BuildAll()
        {
            SetupProject();
            ImportMixamo();
            BuildPrefabs();
            BuildArenaScene();
        }

        [MenuItem(Menu + "1 Setup Project（层、工程名）", priority = 20)]
        public static void SetupProject()
        {
            LayerSetup.EnsureLayers();
            PlayerSettings.companyName = "BossFight3D";
            PlayerSettings.productName = "BossFight3D";
            AssetDatabase.SaveAssets();
        }

        [MenuItem(Menu + "2 Import Mixamo（Humanoid 设置 + 动画控制器）", priority = 21)]
        public static void ImportMixamo()
        {
            if (!MixamoImportSetup.Configure())
            {
                Debug.Log($"[BossFight] 没找到 {ProjectPaths.MixamoCharacter}，玩家将使用占位胶囊体。" +
                          "素材下载清单见 docs/CREDITS.md");
                return;
            }

            PlayerAnimatorBuilder.Build();
        }

        [MenuItem(Menu + "3 Build Prefabs（玩家、木桩预制体）", priority = 22)]
        public static void BuildPrefabs()
        {
            PlayerPrefabBuilder.Build();
            DummyPrefabBuilder.Build();
        }

        [MenuItem(Menu + "4 Build Arena Scene（场地场景）", priority = 23)]
        public static void BuildArenaScene()
        {
            // 会新建并切换场景：先让你决定要不要保存当前场景的修改
            if (!Application.isBatchMode
                && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ArenaSceneBuilder.Build();
        }
    }
}
