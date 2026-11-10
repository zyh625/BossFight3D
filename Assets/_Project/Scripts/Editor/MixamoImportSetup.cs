using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 把 Mixamo 下载的 FBX 统一设置成 Humanoid：
    /// 角色 FBX 生成 Avatar；动画 FBX 复用这个 Avatar，并设置循环、锁定根运动。
    /// 这样就不用在 Inspector 里一个个手动点导入设置。
    /// </summary>
    internal static class MixamoImportSetup
    {
        /// <summary>需要循环播放的动画（文件名，不含扩展名）。新增循环动画时在这里登记。</summary>
        private static readonly HashSet<string> LoopingClips = new HashSet<string> { "Idle", "Walk", "Run", "HeavyCharge" };

        /// <returns>角色 FBX 是否存在并配置成功。</returns>
        public static bool Configure()
        {
            Avatar avatar = ConfigureCharacter(ProjectPaths.MixamoCharacter);
            if (avatar == null)
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(ProjectPaths.MixamoPlayerAnimations))
            {
                foreach (string path in FindFbx(ProjectPaths.MixamoPlayerAnimations))
                {
                    ConfigureAnimation(path, avatar);
                }
            }

            return true;
        }

        /// <summary>从动画 FBX 里取出真正的动画片段（跳过 Unity 的预览片段）。</summary>
        public static AnimationClip LoadClip(string fbxPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        private static Avatar ConfigureCharacter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return null;
            }

            if (importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError($"[BossFight] {path} 没能生成有效的 Humanoid Avatar，请检查它是否是 T-Pose、带骨骼的角色");
                return null;
            }

            return avatar;
        }

        private static void ConfigureAnimation(string path, Avatar avatar)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);

            // 第一步：先变成 Humanoid 并复用角色的 Avatar，重新导入后才能读到片段列表
            if (importer.animationType != ModelImporterAnimationType.Human || importer.sourceAvatar != avatar)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.SaveAndReimport();
            }

            // 第二步：片段设置
            string clipName = Path.GetFileNameWithoutExtension(path);
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                Debug.LogWarning($"[BossFight] {path} 里没有动画片段");
                return;
            }

            ModelImporterClipAnimation clip = clips[0];
            clip.name = clipName; // Mixamo 的片段都叫 "mixamo.com"，改成文件名
            clip.loopTime = LoopingClips.Contains(clipName);

            // 位移和朝向由代码控制：把根节点的旋转、高度、水平位移都烘焙进姿势，动画本身不移动角色
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = true;

            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
        }

        private static IEnumerable<string> FindFbx(string folder)
        {
            return AssetDatabase.FindAssets("t:Model", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
