using System.IO;
using UnityEditor;

namespace BossFight.EditorTools
{
    /// <summary>工程内约定的资源路径。所有搭建脚本都从这里取路径。</summary>
    internal static class ProjectPaths
    {
        public const string Root = "Assets/_Project";

        public const string InputActions = Root + "/Input/GameInput.inputactions";
        public const string PlayerMovementConfig = Root + "/Data/PlayerMovementConfig.asset";
        public const string PlayerCombatConfig = Root + "/Data/PlayerCombatConfig.asset";
        public const string AttackDataFolder = Root + "/Data/Attacks";

        public const string Materials = Root + "/Art/Materials";
        public const string PlayerPrefab = Root + "/Prefabs/Player.prefab";
        public const string DummyPrefab = Root + "/Prefabs/TrainingDummy.prefab";
        public const string ArenaScene = Root + "/Scenes/Arena.unity";
        public const string ArenaVolumeProfile = Root + "/Settings/ArenaVolumeProfile.asset";

        // Mixamo 原始素材（不进 git）
        public const string MixamoCharacter = Root + "/Art/Mixamo/Characters/YBot.fbx";
        public const string MixamoPlayerAnimations = Root + "/Art/Mixamo/Animations/Player";

        // 由 Mixamo 素材生成的资源（不进 git）
        public const string PlayerAnimatorController = Root + "/Generated/Player.controller";

        /// <summary>确保资源所在的文件夹存在（逐级创建，让 Unity 生成 .meta）。</summary>
        public static void EnsureFolderFor(string assetPath)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
