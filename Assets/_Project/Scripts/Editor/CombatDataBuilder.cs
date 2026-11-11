using BossFight.Boss;
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
                SetDefaultDodgeCounter(config);
                SetDefaultFistAttacks(config);
                return config;
            }

            //                              动画状态   前摇    判定   后摇    伤害  削格挡  体力  踏步
            AttackData light1 = Attack("Player_Light1", "Light1", 0.10f, 0.10f, 0.30f, 18f, 12f, 10f, 0.6f);
            AttackData light2 = Attack("Player_Light2", "Light2", 0.10f, 0.10f, 0.32f, 18f, 12f, 10f, 0.6f);
            AttackData light3 = Attack("Player_Light3", "Light3", 0.16f, 0.12f, 0.50f, 26f, 18f, 12f, 0.9f);
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
            SetDefaultDodgeCounter(config);
            SetDefaultFistAttacks(config);
            return config;
        }

        public static BossCombatConfig GetOrCreateBossCombatConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BossCombatConfig>(ProjectPaths.BossCombatConfig);
            if (existing != null)
            {
                SetDefaultLongswordCombo(existing);
                return existing;
            }

            AttackData[] strikes =
            {
                Attack("Boss_SlashLeft", "", 0.45f, 0.12f, 0f, 12f, 0f, 0f, 0.35f),
                Attack("Boss_SlashRight", "", 0.35f, 0.12f, 0f, 12f, 0f, 0f, 0.25f),
                Attack("Boss_CrossSlash", "", 0.75f, 0.15f, 1f, 24f, 0f, 0f, 0.45f)
            };
            var config = ScriptableObject.CreateInstance<BossCombatConfig>();
            ProjectPaths.EnsureFolderFor(ProjectPaths.BossCombatConfig);
            AssetDatabase.CreateAsset(config, ProjectPaths.BossCombatConfig);
            EditorWiring.Edit(config, "combo", property =>
            {
                property.arraySize = strikes.Length;
                for (int i = 0; i < strikes.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = strikes[i];
                }
            });
            SetDefaultLongswordCombo(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void SetDefaultLongswordCombo(BossCombatConfig config)
        {
            if (config.HasLongswordCombo)
            {
                return;
            }

            // 长剑四连击：三快一慢。前三刀前摇很短，第四刀明显停顿后竖劈，收招很长
            AttackData[] strikes =
            {
                Attack("Boss_Long1", "", 0.30f, 0.12f, 0f, 14f, 0f, 0f, 0.6f),
                Attack("Boss_Long2", "", 0.24f, 0.12f, 0f, 14f, 0f, 0f, 0.5f),
                Attack("Boss_Long3", "", 0.24f, 0.12f, 0f, 14f, 0f, 0f, 0.5f),
                Attack("Boss_Long4", "", 0.85f, 0.16f, 1.1f, 30f, 0f, 0f, 0.8f)
            };
            EditorWiring.Edit(config, "longswordCombo", property =>
            {
                property.arraySize = strikes.Length;
                for (int i = 0; i < strikes.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = strikes[i];
                }
            });
            AssetDatabase.SaveAssets();
        }

        private static void SetDefaultFistAttacks(PlayerCombatConfig config)
        {
            if (config.FistComboLength == 0)
            {
                // 拳套：出手极快、伤害低、踏步短，靠连打和削格挡条
                AttackData left = Attack("Player_FistLeft", "Punch1", 0.07f, 0.08f, 0.16f, 9f, 9f, 5f, 0.35f);
                AttackData right = Attack("Player_FistRight", "Punch2", 0.07f, 0.08f, 0.18f, 10f, 9f, 5f, 0.35f);
                EditorWiring.Edit(config, "fistCombo", combo =>
                {
                    combo.arraySize = 2;
                    combo.GetArrayElementAtIndex(0).objectReferenceValue = left;
                    combo.GetArrayElementAtIndex(1).objectReferenceValue = right;
                });
            }

            if (config.FistHeavy == null)
            {
                // 蓄力冲拳：0.2 秒内向前冲 5 米（速度 25 m/s，看起来就是瞬移）再出拳
                AttackData dash = Attack("Player_FistHeavy", "PunchHeavy", 0.10f, 0.10f, 0.5f, 38f, 40f, 0f, 5f);
                EditorWiring.SetRef(config, "fistHeavy", dash);
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetDefaultDodgeCounter(PlayerCombatConfig config)
        {
            if (config.DodgeCounter == null)
            {
                // 前摇很短、突进很远、不耗体力：完美闪避的奖励
                AttackData counter = Attack("Player_DodgeCounter", "Light3", 0.08f, 0.12f, 0.35f, 30f, 20f, 0f, 3f);
                EditorWiring.SetRef(config, "dodgeCounter", counter);
                AssetDatabase.SaveAssets();
            }
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
