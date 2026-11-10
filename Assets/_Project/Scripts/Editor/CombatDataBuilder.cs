using BossFight.Combat;
using BossFight.Player;
using UnityEditor;
using UnityEngine;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 创建玩家战斗数据的默认值（docs/DESIGN.md 2.2 的动作表）。
    /// 资源已存在时直接复用，不会覆盖你调过的数值；想恢复默认值就删掉对应的 .asset 再执行。
    /// </summary>
    internal static class CombatDataBuilder
    {
        public static PlayerCombatConfig GetOrCreatePlayerCombatConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerCombatConfig>(ProjectPaths.PlayerCombatConfig);
            if (config != null)
            {
                SetDefaultVariedAttack(config);
                return config;
            }

            //                              动画状态   前摇    判定   后摇    伤害  削格挡  体力  踏步
            AttackData light1 = Attack("Player_Light1", "Light1", 0.18f, 0.10f, 0.30f, 18f, 12f, 10f, 0.6f);
            AttackData light2 = Attack("Player_Light2", "Light2", 0.16f, 0.10f, 0.32f, 18f, 12f, 10f, 0.6f);
            AttackData light3 = Attack("Player_Light3", "Light3", 0.25f, 0.12f, 0.50f, 26f, 18f, 12f, 0.9f);
            AttackData heavy = Attack("Player_Heavy", "Heavy", 0.45f, 0.15f, 0.60f, 45f, 35f, 0f, 1.2f);

            config = ScriptableObject.CreateInstance<PlayerCombatConfig>();
            ProjectPaths.EnsureFolderFor(ProjectPaths.PlayerCombatConfig);
            AssetDatabase.CreateAsset(config, ProjectPaths.PlayerCombatConfig);

            EditorWiring.Edit(config, "lightCombo", combo =>
            {
                combo.arraySize = 3;
                combo.GetArrayElementAtIndex(0).objectReferenceValue = light1;
                combo.GetArrayElementAtIndex(1).objectReferenceValue = light2;
                combo.GetArrayElementAtIndex(2).objectReferenceValue = light3;
            });
            EditorWiring.SetRef(config, "heavyAttack", heavy);

            SetDefaultVariedAttack(config);
            return config;
        }

        private static void SetDefaultVariedAttack(PlayerCombatConfig config)
        {
            if (config.VariedAttack == null)
            {
                AttackData varied = Attack("Player_Varied", "Heavy", 0.28f, 0.12f, 0.40f, 32f, 25f, 20f, 0.85f);
                EditorWiring.SetRef(config, "variedAttack", varied);
                AssetDatabase.SaveAssets();
            }
        }

        private static AttackData Attack(
            string assetName, string animation, float windup, float active, float recovery,
            float damage, float guardDamage, float stamina, float lunge)
        {
            string path = $"{ProjectPaths.AttackDataFolder}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<AttackData>();
            data.EditorInit(animation, windup, active, recovery, damage, guardDamage, stamina, lunge);
            ProjectPaths.EnsureFolderFor(path);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }
    }
}
