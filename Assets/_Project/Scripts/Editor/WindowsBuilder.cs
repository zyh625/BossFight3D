using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 一键打出 Windows 可执行文件。菜单和命令行（-executeMethod）走的是同一个方法，
    /// 所以"在编辑器里能打出来的包"和"自动化打出来的包"是同一份配置。
    /// </summary>
    public static class WindowsBuilder
    {
        public const string OutputPath = "Builds/Windows/BossFight3D.exe";

        [MenuItem("Tools/BossFight/5 Build Windows（打包到 Builds 目录）", priority = 40)]
        public static void Build()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectPaths.ArenaScene },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            string message = $"[BossFight] Windows 构建 {summary.result}：{summary.outputPath}，" +
                             $"{summary.totalSize / (1024f * 1024f):0.0} MB，耗时 {summary.totalTime.TotalSeconds:0} 秒，" +
                             $"错误 {summary.totalErrors}，警告 {summary.totalWarnings}";

            if (summary.result != BuildResult.Succeeded)
            {
                // 抛异常让批处理模式以非零退出码结束，自动化流程才能发现构建失败
                throw new InvalidOperationException(message);
            }

            Debug.Log(message);
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(OutputPath);
            }
        }
    }
}
