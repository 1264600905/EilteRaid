using EliteRaid;
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
        public static bool m_CanTranspilerGeneratePawns = true;
        public static bool m_CanTranspilerGenerateAnimals = true;
        public static bool m_CanTranspilerEntitySwarm = true;        //是否启用实体增强
        public static HashSet<Type> m_AllowPawnGroupKindWorkerTypes = new HashSet<Type>();
        // 在 GenerateAnything_Impl 方法中重构等级分配逻辑
        public static void GenerateAnything_Impl(List<Pawn> pawns, int baseNum, int maxPawnNum, bool raidFriendly)
        {
            Log.Message($"[EliteRaid] GenerateAnything_Impl: baseNum={baseNum}, maxPawnNum={maxPawnNum}, raidFriendly={raidFriendly}");
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
                    EliteLevel currentLevel;
                    if (levelIndex >= totalLevels)
                    {
                        // 如果等级不足，重新生成一次
                        var eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                        if (eliteLevel != null && eliteLevel.Level > 0)
                        {
                            currentLevel = eliteLevel;
                            if (EliteRaidMod.displayMessageValue)
                            {
                                Log.Message($"[EliteRaid] 重新生成等级成功: {pawn.LabelCap} -> Level {eliteLevel.Level}");
                            }
                        }
                        else
                        {
                            if (EliteRaidMod.displayMessageValue)
                            {
                                Log.Warning($"[EliteRaid] 精英等级不足，无法分配等级给 pawn {pawn.LabelCap}（索引：{i}）");
                            }
                            continue;
                        }
                    }
                    else
                    {
                        currentLevel = orderedLevelsList[levelIndex++];
                    }

                    if (!IsEliteLevelValid(currentLevel)) continue;

                    // 计算Hediff索引（每级9个，1级:1-9, 2级:10-19, 3级:20-29, 4级:30-39, 5级:40-49, 6级:50-59, 7级:60-69）
                    int minIndex = (currentLevel.Level - 1) * 10 + 1;
                    int maxIndex = minIndex + 8; // 每级9个，最后一个不用
                    int configIndex = Rand.Range(minIndex, maxIndex + 1);
                    string defName = $"CR_Powerup{configIndex}";
                    HediffDef powerupDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);

                    if (powerupDef == null && EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 尝试分配等级 {currentLevel.Level}，使用范围 [{minIndex}-{maxIndex}]，但找不到 {defName}");
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
                int finalNum = GetenhancePawnNumber(baseNum);
                float actualCompressionRatio = GetcompressionRatio(baseNum, finalNum);
                if (EliteRaidMod.showRaidMessages)
                {
                    Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum, finalNum,
                        actualCompressionRatio.ToString("F2"), finalNum), MessageTypeDefOf.NeutralEvent, true);
                }
            }
        }
        public static float GetcompressionRatio(int baseNum, int maxPawnNum)
        {
            if (maxPawnNum <= 0) return 1f;
            return (float)baseNum / maxPawnNum;
        }

public static int GetenhancePawnNumber(int baseNum)
{
    int startCompressNum = EliteRaidMod.startCompressNum;
    float compressRate = EliteRaidMod.compressionRatio;
    int maxRaidEnemy = EliteRaidMod.maxRaidEnemy;
    int hardLimitNum = (int)(maxRaidEnemy*compressRate); // 如果没有可用 startCompressNum * compressRate 的结果

    // 新增逻辑：如果开始压缩人数为0，直接用压缩倍率计算
    if (startCompressNum == 0)
    {
        int compressed = (int)(baseNum / compressRate);
        return Math.Min(Math.Max(1, compressed), maxRaidEnemy); // 至少为1，且不超过maxRaidEnemy
    }

    if (baseNum <= startCompressNum)
    {
        return baseNum;
    }
    else if (baseNum <= (int)(startCompressNum * compressRate))
    {
        return startCompressNum;
    }
    else if (baseNum <= hardLimitNum)
    {
        int compressed = (int)(baseNum / compressRate);
        // 不能小于startCompressNum，也不能大于maxRaidEnemy
        return Math.Max(startCompressNum, Math.Min(compressed, maxRaidEnemy));
    }
    else
    {
        return maxRaidEnemy;
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

            //平衡生成的动物和人类的数量
            int humanCount = pawns.Where(p => !p.RaceProps.Animal).ToList().Count;
            int OthersCount = pawns.Count - humanCount;
            // 如果全部是动物，则只添加一个保底单位（人类），保底单位用Pirate
            if (humanCount <= 0 && pawns.Count > 0&&parms.faction!=Faction.OfInsects)
            {
                List<Pawn> animalPawns = pawns.Where(p => p.RaceProps.Animal).ToList();
                PawnKindDef humanKind = PawnKindDefOf.Pirate;
                Faction faction = animalPawns[0].Faction;
                Log.Message($"[EliteRaid][General.cs@L288] 全是动物，添加保底人类单位，kind={humanKind.defName}, faction={faction?.Name ?? "null"}");
                Pawn atleastOnePerson = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    humanKind,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true,
                    colonistRelationChanceFactor: 1f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowFood: false,
                    allowAddictions: false,
                    inhabitant: false,
                    certainlyBeenInCryptosleep: false,
                    forceRedressWorldPawnIfFormerColonist: false));
                Log.Message($"[EliteRaid][General.cs@L288] PawnGenerator.GeneratePawn结果: {(atleastOnePerson == null ? "null" : atleastOnePerson.LabelCap)}");
                PawnWeaponChager.CheckAndReplaceMainWeapon(atleastOnePerson);
                pawns.Replace(animalPawns[0], atleastOnePerson);
                Log.Message($"[EliteRaid] 添加保底人类单位: {atleastOnePerson.LabelCap}");
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
            Log.Message($"[Debug] animalKind={animalKind?.defName}, pawns.Count={pawns?.Count}");
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
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly: false);
        }

        // 测试方法：验证压缩计算逻辑
        public static void TestCompressionLogic()
        {
            if (!EliteRaidMod.displayMessageValue) return;
            
            Log.Message("=== 压缩逻辑测试开始 ===");
            
            // 测试不同难度级别的压缩计算
            var testCases = new[]
            {
                new { Difficulty = "修士/扈从", MaxRaidEnemy = 20, CompressionRatio = 3f },
                new { Difficulty = "骑士/法务官", MaxRaidEnemy = 30, CompressionRatio = 3f },
                new { Difficulty = "领主/总督", MaxRaidEnemy = 40, CompressionRatio = 4f },
                new { Difficulty = "都督/专制公", MaxRaidEnemy = 50, CompressionRatio = 5f },
                new { Difficulty = "大公/执政官/将军", MaxRaidEnemy = 60, CompressionRatio = 6f },
                new { Difficulty = "近卫总长/星系主宰", MaxRaidEnemy = 70, CompressionRatio = 8f },
                new { Difficulty = "至高星主", MaxRaidEnemy = 80, CompressionRatio = 9f },
                new { Difficulty = "星海皇帝", MaxRaidEnemy = 100, CompressionRatio = 10f }
            };
            
            foreach (var testCase in testCases)
            {
                Log.Message($"--- {testCase.Difficulty} 测试 ---");
                Log.Message($"设置: MaxRaidEnemy={testCase.MaxRaidEnemy}, CompressionRatio={testCase.CompressionRatio}");
                
                // 临时设置参数
                EliteRaidMod.maxRaidEnemy = testCase.MaxRaidEnemy;
                EliteRaidMod.compressionRatio = testCase.CompressionRatio;
                EliteRaidMod.useCompressionRatio = true;
                
                // 测试不同袭击人数
                int[] testNumbers = { 200, 299, 400, 599, 800, 1000 };
                foreach (int baseNum in testNumbers)
                {
                    int result = GetenhancePawnNumber(baseNum);
                    float actualRatio = (float)baseNum / result;
                    Log.Message($"  袭击{baseNum}人 → 压缩后{result}人 (实际压缩率: {actualRatio:F2})");
                }
                Log.Message("");
            }
            
            Log.Message("=== 压缩逻辑测试结束 ===");
        }

    
    }
}
