using System;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>往 Tags and Layers 里登记工程用到的层（已存在则跳过）。</summary>
    internal static class LayerSetup
    {
        public const string Player = "Player";
        public const string Environment = "Environment";

        private const int FirstUserLayer = 6;
        private const int LayerCount = 32;

        public static void EnsureLayers()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            EnsureLayer(layers, Player);
            EnsureLayer(layers, Environment);

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static int Get(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new InvalidOperationException($"层 '{layerName}' 不存在，请先执行 Tools/BossFight/M1/1 Setup Project");
            }

            return layer;
        }

        private static void EnsureLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < LayerCount; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return;
                }
            }

            for (int i = FirstUserLayer; i < LayerCount; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    return;
                }
            }

            throw new InvalidOperationException("没有空闲的层可用");
        }
    }
}
