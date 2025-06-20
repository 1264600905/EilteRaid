﻿using EliteRaid;
using RimwoldEliteRaidProject.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static EliteRaid.StaticVariables_ModCompatibility;

namespace EliteRaid
{
    internal static class General
    {
        internal enum MessageTypes : byte
        {
            Infomation = 0x00,
            Warning = 0x01,
            Error = 0x03,
            Debug = 0x04,
            DebugWarning = 0x05,
            DebugError = 0x06,
        }

        internal static void SendLog_Debug(MessageTypes type, string temp, params object[] args)
        {
            return;
#if DEBUG
            if (!Prefs.DevMode && (byte)type >= 0x04)
            {
                return;
            }
            if (args?.Any() ?? false)
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                }
            } else
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                }
            }
#endif
        }
        internal static void SendLog(MessageTypes type, string temp, params object[] args)
        {
            return;
            if (!Prefs.DevMode && (byte)type >= 0x04)
            {
                return;
            }
            if (args?.Any() ?? false)
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                }
            } else
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                }
            }
        }
        private static int hediffDefCounter = 0;
        public static bool m_CanTranspilerGeneratePawns = true;
        public static bool m_CanTranspilerGenerateAnimals = true;
        public static bool m_CanTranspilerEntitySwarm = true;        //是否启用实体增强
        public static HashSet<Type> m_AllowPawnGroupKindWorkerTypes = new HashSet<Type>();
        // 在 GenerateAnything_Impl 方法中重构等级分配逻辑
        public static void GenerateAnything_Impl(List<Pawn> pawns, int baseNum, int maxPawnNum, bool raidFriendly)
        {
            EliteLevelManager.GenerateLevelDistribution(baseNum);
            int enhancePawnNumber = EliteLevelManager.getCurrentLevelDistributionNum();
            int enhancedCount = 0;
            int order = PowerupUtility.GetNewOrder(); // 保留原有逻辑（若需要）

            // 记录日志（根据配置）
            if (EliteRaidMod.useCompressionRatio && EliteRaidMod.displayMessageValue)
            {
                // Log.Message($"压缩后人数（压缩率{EliteRaidMod.compressionRatio}）：{enhancePawnNumber}");
            } else
            {
                // Log.Message("最终输出人数" + enhancePawnNumber);
            }

            // -------------------- 新增：按等级分组逻辑 --------------------
            // 生成所有精英等级并分组（每个等级最多10个）
            var levelGroups = new Dictionary<int, List<EliteLevel>>();
            for (int i = 0; i < enhancePawnNumber; i++)
            {
                var eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                int cappedLevel = Math.Min(eliteLevel.Level, 7); // 限制最大等级为7
                if (!levelGroups.ContainsKey(cappedLevel))
                {
                    levelGroups[cappedLevel] = new List<EliteLevel>();
                }
                if (levelGroups[cappedLevel].Count < 10) // 每个等级最多10个
                {
                    levelGroups[cappedLevel].Add(eliteLevel);
                } else
                {
                    // 等级已满，随机替换一个现有等级（或使用默认处理）
                    var randomLevel = levelGroups.Values.SelectMany(g => g).RandomElement();
                    levelGroups[cappedLevel].Remove(randomLevel);
                    levelGroups[cappedLevel].Add(eliteLevel);
                }
            }

            // 展平分组数据为顺序列表（按等级升序排列，同等级内随机顺序）
            var orderedLevelsList = levelGroups
      .OrderBy(kvp => kvp.Key)
      .SelectMany(kvp => kvp.Value.OrderBy(l => Rand.Range(0, 100)))
      .ToList(); // 转换为列表
                 // 提前将 orderedLevels 转换为列表，避免多次枚举
            int totalLevels = orderedLevelsList.Count; // 获取总元素数
            // -------------------- 原有循环逻辑改造 --------------------
            int levelIndex = 0; // 用于遍历 orderedLevels
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                // 添加兼容性Hediff
                if (MOD_MSER_Active)
                {
                    pawn.health.AddHediff(CR_DummyForCompatibilityDefOf.CR_DummyForCompatibility);
                }

                // 处理精英升级（仅前 enhancePawnNumber 个pawn）
                if (EliteRaidMod.modEnabled && i < enhancePawnNumber && EliteRaidMod.AllowCompress(pawn))
                {
                    // **关键检查：确保 levelIndex 不超过列表长度**
                    if (levelIndex >= totalLevels)
                    {
                        Log.Warning($"[EliteRaid] 精英等级不足，无法分配等级给 pawn {pawn.LabelCap}（索引：{i}）");
                        break; // 等级不足时提前退出循环
                    }

                    var currentLevel = orderedLevelsList[levelIndex++];
                    if (!IsEliteLevelValid(currentLevel)) continue;

                    // 计算Hediff索引（1-70，每级10个）
                    int configIndex = (currentLevel.Level - 1) * 10 + Rand.Range(1, 11);
                    string defName = $"CR_Powerup{configIndex}";
                    HediffDef powerupDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                    if (powerupDef == null)
                    {
                        // Log.Message($"[EliteRaid] 找不到HediffDef: {defName}");
                        continue;
                    }

                    // 添加Hediff并应用属性
                    Hediff powerup = pawn.health.AddHediff(powerupDef);
                    if (powerup != null)
                    {
                        // 设置标签为真实等级（非截取后的等级）
                        powerup.Label.Named("EliteLevelLabel".Translate(currentLevel.Level));

                        // 应用属性修改器
                        if (PowerupUtility.TrySetStatModifierToHediff(powerup, currentLevel))
                        {
                            enhancedCount++;
                        } else
                        {
                            pawn.health.RemoveHediff(powerup);
                            // Log.Message($"[EliteRaid] 移除无效Hediff：{pawn.Name}");
                        }
                    }
                }

                // DummyForCompatibility 移除逻辑（保持不变）
                if (MOD_MSER_Active && i >= enhancePawnNumber)
                {
                    CR_DummyForCompatibility dummyHediff = pawn.health.hediffSet.hediffs
                        .Where(h => h is CR_DummyForCompatibility)
                        .Cast<CR_DummyForCompatibility>()
                        .FirstOrDefault();
                    if (dummyHediff != null)
                    {
                        pawn.health.RemoveHediff(dummyHediff);
                    }
                }
            }

            // 消息提示（保持不变）
            if (enhancedCount > 0)
            {
                int finalNum = GetenhancePawnNumber(baseNum, enhancePawnNumber);
                Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum, finalNum,
                    GetcompressionRatio(baseNum, maxPawnNum), finalNum), MessageTypeDefOf.NeutralEvent, true);
            }
        }
        public static float GetcompressionRatio(int baseNum, int maxPawnNum)
        {
            float compressionRatio = (float)(Math.Ceiling((double)(baseNum  / maxPawnNum)));
            if (EliteRaidMod.useCompressionRatio)
            {
                return EliteRaidMod.compressionRatio;
            } else
            {
                return compressionRatio;
            }
        }


        public static int GetenhancePawnNumber(int baseNum, int enhancePawnNumber)
        {
            int tempNum = (int)(baseNum / EliteRaidMod.compressionRatio);
            if (EliteRaidMod.useCompressionRatio && tempNum < enhancePawnNumber)
            {
                if (tempNum < 20)
                {
                    return 20;
                }
                return tempNum;
            } else
            {
                return EliteRaidMod.maxRaidEnemy;
            }
        }


        internal static void GeneratePawns_Impl(PawnGroupMakerParms parms, List<Pawn> pawns)
        {
            if ((pawns?.EnumerableNullOrEmpty() ?? true) || parms?.groupKind == null)
            {
                return;
            }
            foreach (Pawn pawn in pawns) { 
            PawnWeaponChager.CheckAndReplaceMainWeapon(pawn); // 检查并替换主武器
            }
            PawnWeaponChager.ResetCounter();
               // 新增：输出原始参数和 TryGetCompressWork_GeneratePawnsValues 的结果
               Type pawnGroupWorker = null;
            int baseNum = 0;
            int maxPawnNum = 0;
            bool raidFriendly = false;
            bool tryGetSuccess = PatchContinuityHelper.TryGetCompressWork_GeneratePawnsValues(
                parms.GetHashCode(),
                out pawnGroupWorker,
                out baseNum,
                out maxPawnNum,
                out raidFriendly
            );

           // Log.Message($"[EliteRaid] TryGetCompressWork_GeneratePawnsValues 结果: {tryGetSuccess}");
          //  Log.Message($"[EliteRaid] baseNum: {baseNum}, maxPawnNum: {maxPawnNum}, raidFriendly: {raidFriendly}");

           if (pawns.Count<EliteRaidMod.maxRaidEnemy)
            {
                return;
            }

            //平衡生成的动物和人类的数量
            int humanCount = pawns.Where(p => !p.RaceProps.Animal).ToList().Count ;
            int OthersCount = pawns.Count - humanCount;
            if (humanCount<=0||(pawns.Count>0&&OthersCount/humanCount>1))
            {
                int needReplaceCount =(int)Mathf.Ceil( OthersCount / 2);
                List<Pawn> animalPawns = pawns.Where(p => p.RaceProps.Animal).ToList();
                for (int i = 0; i < needReplaceCount; i++) {

                   

                    if (pawns[i] != null )
                    {
                        // 生成保底人类单位
                        Pawn atleastOnePerson = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                             parms.faction.RandomPawnKind(),
                             parms.faction,
                             PawnGenerationContext.NonPlayer,
                             -1,
                             forceGenerateNewPawn: true,
                             mustBeCapableOfViolence: true,
                             biocodeWeaponChance: 0.7f,
                             biocodeApparelChance: 0.7f
                         ));
                        PawnWeaponChager.CheckAndReplaceMainWeapon(atleastOnePerson);
                        pawns.Replace(animalPawns[i],atleastOnePerson);
                         Log.Message($"[EliteRaid] 添加保底人类单位: {atleastOnePerson.LabelCap}");
                    }
                }
                PawnWeaponChager.ResetCounter();
               // Log.Warning("[Elite Raid] 压缩率过高导致没有正常生成人类  Excessive compression rate caused failure to normally generate human units.");
            }
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly);
        }
        internal static void GenerateAnimals_Impl(PawnKindDef animalKind, List<Pawn> pawns)
        {
            if ((pawns?.EnumerableNullOrEmpty() ?? true) || animalKind == null)
            {
                return;
            }
            if (!PatchContinuityHelper.TryGetCompressWork_GenerateAnimalsValues(animalKind.GetHashCode(), out int baseNum, out int maxPawnNum, out bool raidFriendly))
            {
                return;
            }
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly);
        }
        // 新增：判断精英等级是否有效（等级≥1且有实际强化属性）
        private static bool IsEliteLevelValid(EliteLevel eliteLevel)
        {
            if (eliteLevel == null || eliteLevel.Level < 1)
                return false;

            // 检查是否有实际强化（例如承伤系数或移速不等于1）
            return eliteLevel.DamageFactor != 1f ||
                   eliteLevel.MoveSpeedFactor != 1f ||
                   eliteLevel.ScaleFactor != 1f; // 可根据需求添加更多检查
        }
        internal static void GenerateEntitys_Impl(List<Pawn> pawns, int baseNum, int maxPawnNum)
        {
            if (pawns == null || pawns.Count == 0 || baseNum <= 0 || maxPawnNum <= 0)
            {
                return;
            }
            if(EliteRaidMod.displayMessageValue)
            SendLog_Debug(MessageTypes.Debug, "进入增强逻辑，实体数量：{0}，baseNum：{1}，maxPawnNum：{2}",
                 pawns.Count, baseNum, maxPawnNum); // 添加此行
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly: false);
        }
    }
}
