using EliteRaid;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace EliteRaid
{
    internal static class PatchContinuityHelper
    {
        // 新增：袭击记录结构体
        public struct RaidRecord
        {
            public DateTime RecordTime;
            public int BaseNum;
            public Faction Faction;
            public Type PawnGroupWorker;
            public bool IsDropPodRaid;
            
            public RaidRecord(DateTime recordTime, int baseNum, Faction faction, Type pawnGroupWorker, bool isDropPodRaid = false)
            {
                RecordTime = recordTime;
                BaseNum = baseNum;
                Faction = faction;
                PawnGroupWorker = pawnGroupWorker;
                IsDropPodRaid = isDropPodRaid;
            }
        }

        // 新增：存储袭击记录的列表
        private static readonly List<RaidRecord> raidRecords = new List<RaidRecord>();
        private static readonly object raidRecordsLock = new object();

        struct CompressWork
        {
            public bool allowedCompress;
            public int hashKey;
            public Type pawnGroupWorker;
            public int baseNum;
            public int maxPawnNum;
            public bool raidFriendly;
            public CompressWork(int hashKey, int baseNum, int maxPawnNum, bool raidFriendly = false)
            {
                allowedCompress = true;
                this.hashKey = hashKey;
                this.baseNum = baseNum;
                this.maxPawnNum = maxPawnNum;
                this.raidFriendly = raidFriendly;
                pawnGroupWorker = null;
            }
            public CompressWork(int hashKey, Type pawnGroupWorker, int baseNum, int maxPawnNum, bool raidFriendly = false)
            {
                allowedCompress = true;
                this.hashKey = hashKey;
                this.baseNum = baseNum;
                this.maxPawnNum = maxPawnNum;
                this.raidFriendly = raidFriendly;
                this.pawnGroupWorker = pawnGroupWorker;
            }
        }
        private static CompressWork m_CompressWork_GeneratePawns = default(CompressWork);
        private static CompressWork m_CompressWork_GenerateAnimals = default(CompressWork);

        // 新增：记录袭击信息的方法
        internal static void RecordRaidInfo(int baseNum, Faction faction, Type pawnGroupWorker, bool isDropPodRaid = false)
        {
            lock (raidRecordsLock)
            {
                var record = new RaidRecord(DateTime.Now, baseNum, faction, pawnGroupWorker, isDropPodRaid);
                raidRecords.Add(record);
                
                // 清理超过5分钟的旧记录
                var cutoffTime = DateTime.Now.AddMinutes(-5);
                raidRecords.RemoveAll(r => r.RecordTime < cutoffTime);
                
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 记录袭击信息: 时间={record.RecordTime:HH:mm:ss.fff}, 数量={baseNum}, 派系={faction?.Name ?? "无"}, 类型={pawnGroupWorker?.Name ?? "无"}, 空投={isDropPodRaid}");
                }
            }
        }

        // 新增：查找最接近的袭击记录
        internal static bool TryFindClosestRaidRecord(Faction faction, out RaidRecord record, int maxTimeDiffSeconds = 30)
        {
            record = default(RaidRecord);
            
            lock (raidRecordsLock)
            {
                if (raidRecords.Count == 0)
                {
                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message("[EliteRaid] 没有找到任何袭击记录");
                    }
                    return false;
                }

                var now = DateTime.Now;
                var cutoffTime = now.AddSeconds(-maxTimeDiffSeconds);
                
                // 查找指定派系且时间在范围内的记录
                var validRecords = raidRecords
                    .Where(r => r.Faction == faction && r.RecordTime >= cutoffTime)
                    .OrderByDescending(r => r.RecordTime)
                    .ToList();

                if (validRecords.Count == 0)
                {
                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 没有找到派系 {faction?.Name ?? "无"} 在 {maxTimeDiffSeconds} 秒内的袭击记录");
                    }
                    return false;
                }

                // 返回时间最接近的记录
                record = validRecords.First();
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 找到最接近的袭击记录: 时间={record.RecordTime:HH:mm:ss.fff}, 数量={record.BaseNum}, 派系={record.Faction?.Name ?? "无"}, 空投={record.IsDropPodRaid}");
                }
                return true;
            }
        }

        // 新增：清理指定派系的袭击记录
        internal static void ClearRaidRecordsForFaction(Faction faction)
        {
            lock (raidRecordsLock)
            {
                int removedCount = raidRecords.RemoveAll(r => r.Faction == faction);
                if (removedCount > 0 && EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 清理了 {removedCount} 条派系 {faction?.Name ?? "无"} 的袭击记录");
                }
            }
        }

        // 新增：获取所有袭击记录的调试信息
        internal static string GetRaidRecordsDebugInfo()
        {
            lock (raidRecordsLock)
            {
                if (raidRecords.Count == 0)
                    return "没有袭击记录";

                var sb = new StringBuilder();
                sb.AppendLine($"当前有 {raidRecords.Count} 条袭击记录:");
                foreach (var record in raidRecords.OrderByDescending(r => r.RecordTime))
                {
                    sb.AppendLine($"  时间: {record.RecordTime:HH:mm:ss.fff}, 数量: {record.BaseNum}, 派系: {record.Faction?.Name ?? "无"}, 空投: {record.IsDropPodRaid}");
                }
                return sb.ToString();
            }
        }

        internal static IEnumerable<PawnGenOptionWithXenotype> SetCompressWork_GeneratePawns(IEnumerable<PawnGenOptionWithXenotype> options, PawnGroupMakerParms groupParms)
        {
            if (options.EnumerableNullOrEmpty() || groupParms == null)
            {
                return StateFalse(options);
            }
            if (!EliteRaidMod.modEnabled)
            {
                return StateFalse(options);
            }
            if (!General.m_AllowPawnGroupKindWorkerTypes.Contains(groupParms.groupKind.workerClass))
            {
                return StateFalse(options);
            }
            if (!EliteRaidMod.AllowCompress(groupParms, out bool raidFriendly))
            {
                return StateFalse(options);
            }
            int maxPawnNum = EliteRaidMod.maxRaidEnemy, pawnCount = 0;
            int baseNum = options.Count();

            // 新增：记录袭击信息（包括空投袭击）
            bool isDropPodRaid = IsDropPodRaid(groupParms);
            RecordRaidInfo(baseNum, groupParms.faction, groupParms.groupKind.workerClass, isDropPodRaid);

            // 新增压缩率计算（确保 baseNum >= compressionRatio 时才压缩）
            if (EliteRaidMod.useCompressionRatio && baseNum >= EliteRaidMod.compressionRatio)
            {
                int temp = (int)(baseNum / EliteRaidMod.compressionRatio);
                maxPawnNum = Math.Min(temp, EliteRaidMod.maxRaidEnemy);
              
            } else
            {
                maxPawnNum = EliteRaidMod.maxRaidEnemy; // 保持原有逻辑或调整为 baseNum
            }
            // 确保至少为1，且不超过原始数量
            maxPawnNum = Math.Max(1, Math.Min(maxPawnNum, baseNum));

            bool allowedCompress = true;
            if (!EliteRaidMod.allowMechanoidsValue && !options.Any(x => !(x.Option.kind?.RaceProps?.IsMechanoid ?? false)))
            {
                allowedCompress = false;
            }
            if (!EliteRaidMod.allowInsectoidsValue && !options.Any(x => !(x.Option.kind?.RaceProps?.FleshType == FleshTypeDefOf.Insectoid)))
            {
                allowedCompress = false;
            }

            List<PawnGenOptionWithXenotype> list = new List<PawnGenOptionWithXenotype>();
            foreach (PawnGenOptionWithXenotype option in options.OrderByDescending(x => x.Cost))
            {
                if (allowedCompress && pawnCount >= maxPawnNum)
                {
                    break;
                }
                pawnCount++;
                list.Add(option);
            }

            if (allowedCompress)
            {
                m_CompressWork_GeneratePawns = new CompressWork(groupParms.GetHashCode(), groupParms.groupKind.workerClass, baseNum, maxPawnNum, raidFriendly);
#if DEBUG
                if (groupParms.traderKind != null)
                {
                   // Log.Message(String.Format("@@@ Compressed Raid: WORKER_CLASS={0}, BASE_NUM={1}, COMPRESSED_NUM={2}, TRADER_KIND={3}", groupParms.groupKind.workerClass, baseNum, maxPawnNum, groupParms.traderKind));
                }
#endif
                return list;
            }
            return StateFalse(options);

            IEnumerable<PawnGenOptionWithXenotype> StateFalse(IEnumerable<PawnGenOptionWithXenotype> ret)
            {
                m_CompressWork_GeneratePawns.allowedCompress = false;
                return ret;
            }
        }

        // 新增：判断是否为空投袭击的辅助方法
        private static bool IsDropPodRaid(PawnGroupMakerParms groupParms)
        {
            // 检查是否为空投袭击
            // 可以通过检查袭击策略或其他参数来判断
            if (groupParms?.raidStrategy != null)
            {
                string strategyName = groupParms.raidStrategy.defName?.ToLower();
                return strategyName?.Contains("drop") == true || 
                       strategyName?.Contains("pod") == true ||
                       strategyName?.Contains("sky") == true;
            }
            return false;
        }

        internal static void SetCompressWork_GeneratePawns(PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOption> __result)
        {
            if ((__result?.EnumerableNullOrEmpty() ?? true) || groupParms == null)
            {
                m_CompressWork_GeneratePawns.allowedCompress = false;
                return;
            }

            // 1. 使用 PawnStateChecker 分离关键角色和普通角色
            var criticalOptions = new List<PawnGenOption>();
            var normalOptions = new List<PawnGenOption>();

            // 为了使用 PawnStateChecker，我们需要临时生成 Pawn 实例进行检查
            List<Pawn> tempPawns = new List<Pawn>();
            try
            {
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message("捕获到当前生成的敌人阵营22"+ groupParms.faction);
                }
                Messages.Message("捕获到当前生成的敌人阵营22"+ groupParms.faction, MessageTypeDefOf.NeutralEvent);
                foreach (var option in __result)
                {
                    // 临时生成 Pawn 用于检查
                       Pawn tempPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        option.kind,
                        groupParms.faction,
                        PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true,
                        canGeneratePawnRelations: false,
                        mustBeCapableOfViolence: false
                    ));
                       
                  
                    tempPawns.Add(tempPawn);

                    // 使用 PawnStateChecker 判断是否可压缩
                    if (!PawnStateChecker.CanCompressPawn(tempPawn))
                    {
                        criticalOptions.Add(option);
                    } else
                    {
                        normalOptions.Add(option);
                    }
                }

                // 2. 只对普通角色执行压缩
                if (normalOptions.Count == 0)
                {
                    m_CompressWork_GeneratePawns.allowedCompress = false;
                    __result = criticalOptions;
                    return;
                }

                // 3. 计算压缩后的数量
                int baseNum = normalOptions.Count;
                
                // 新增：记录袭击信息（包括空投袭击）
                bool isDropPodRaid = IsDropPodRaid(groupParms);
                RecordRaidInfo(baseNum, groupParms.faction, groupParms.groupKind.workerClass, isDropPodRaid);
                
                int maxPawnNum = General.GetenhancePawnNumber(baseNum);

                if (maxPawnNum >= baseNum)
                {
                    m_CompressWork_GeneratePawns.allowedCompress = false;
                    __result = criticalOptions.Concat(normalOptions);
                    return;
                }

                // 4. 实现真正的"压缩"：合并多个敌人为一个更强的敌人
                List<PawnGenOption> compressedOptions = CompressPawnOptions(normalOptions, maxPawnNum,groupParms.faction);

                // 5. 合并关键角色和压缩后的普通角色
                __result = criticalOptions.Concat(compressedOptions);

                // 6. 记录压缩信息
                m_CompressWork_GeneratePawns = new CompressWork(
                    groupParms.GetHashCode(),
                    groupParms.groupKind.workerClass,
                    baseNum,
                    maxPawnNum,
                    groupParms.faction.HostileTo(Faction.OfPlayer));
            } finally
            {
                // 清理临时生成的 Pawn
                foreach (var pawn in tempPawns)
                {
                    pawn.Discard();
                }
            }
        }

        // 优化 CompressPawnOptions 方法中的类型检查
        private static List<PawnGenOption> CompressPawnOptions(List<PawnGenOption> options, int targetCount,Faction faction)
        {
            // 按成本排序（从高到低）
            options = options.OrderByDescending(x => x.Cost).ToList();

            // 计算每个压缩后敌人需要"吸收"的原始敌人数量
            float compressionRatio = (float)options.Count / targetCount;

            List<PawnGenOption> result = new List<PawnGenOption>();

            // 模拟"合并"敌人的过程
            for (int i = 0; i < targetCount; i++)
            {
                int startIndex = (int)(i * compressionRatio);
                int endIndex = Math.Min(startIndex + (int)compressionRatio + 1, options.Count);

                // 获取要"合并"的敌人
                var enemiesToMerge = options.GetRange(startIndex, endIndex - startIndex);

                // 选择最强大的敌人作为基础
                PawnGenOption baseOption = enemiesToMerge[0];

                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message("捕获到当前生成的敌人阵营11"+ faction);
                }
                Messages.Message("捕获到当前生成的敌人阵营11"+ faction, MessageTypeDefOf.NeutralEvent);
                // 确保不压缩关键角色类型
                if (!PawnStateChecker.CanCompressPawn(
                 
                  PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        baseOption.kind,
                        Faction.OfMechanoids, // 使用默认派系进行检查
                        PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true,
                        canGeneratePawnRelations: false
                    ))))
                {
                    result.Add(baseOption);
                    continue;
                }
            }

            return result;
        }

        internal static int SetCompressWork_GenerateAnimals(PawnKindDef animalKind, int baseNum)
        {
           
            if (animalKind == null || baseNum <= 0)
            {
                return StateFalse(baseNum);
            }
            if (!EliteRaidMod.modEnabled || !EliteRaidMod.allowAnimalsValue)
            {
                return StateFalse(baseNum);
            }
            int maxPawnNum = General.GetenhancePawnNumber(baseNum);
            
            if (maxPawnNum >= baseNum)
            {
                return StateFalse(baseNum);
            }
            if (!EliteRaidMod.allowMechanoidsValue && (animalKind?.RaceProps?.IsMechanoid ?? false))
            {
                return StateFalse(baseNum);
            }
            if (!EliteRaidMod.allowInsectoidsValue && animalKind?.RaceProps?.FleshType == FleshTypeDefOf.Insectoid)
            {
                return StateFalse(baseNum);
            }

            int hashKey = animalKind.GetHashCode();
            m_CompressWork_GenerateAnimals = new CompressWork(hashKey, baseNum, maxPawnNum);
#if DEBUG
          //  Log.Message(String.Format("@@@ Compressed Raid: ANIMAL_KIND={0}, BASE_NUM={1}, COMPRESSED_NUM={2}", animalKind.LabelCap, baseNum, maxPawnNum));
#endif
            return maxPawnNum;

            int StateFalse(int ret)
            {
                m_CompressWork_GenerateAnimals.allowedCompress = false;
                return ret;
            }
        }

        internal static bool TryGetCompressWork_GeneratePawnsValues(int hashKey, out Type pawnGroupWorker, out int baseNum, out int maxPawnNum, out bool raidFriendly)
        {
            if (!m_CompressWork_GeneratePawns.allowedCompress || hashKey != m_CompressWork_GeneratePawns.hashKey)
            {
                baseNum = 0;
                maxPawnNum = 0;
                raidFriendly = false;
                pawnGroupWorker = null;
                return false;
            }
            baseNum = m_CompressWork_GeneratePawns.baseNum;
            maxPawnNum = m_CompressWork_GeneratePawns.maxPawnNum;
            raidFriendly = m_CompressWork_GeneratePawns.raidFriendly;
            pawnGroupWorker = m_CompressWork_GeneratePawns.pawnGroupWorker;
            return true;
        }
        internal static bool TryGetCompressWork_GenerateAnimalsValues(int hashKey, out int baseNum, out int maxPawnNum, out bool raidFriendly)
        {
            if (!m_CompressWork_GenerateAnimals.allowedCompress || hashKey != m_CompressWork_GenerateAnimals.hashKey)
            {
                baseNum = 0;
                maxPawnNum = 0;
                raidFriendly = false;
                return false;
            }
            baseNum = m_CompressWork_GenerateAnimals.baseNum;
            maxPawnNum = m_CompressWork_GenerateAnimals.maxPawnNum;
            raidFriendly = m_CompressWork_GenerateAnimals.raidFriendly;
            return true;
        }

        private static CompressWork m_CompressWork_EntitySwarm = default(CompressWork);

        internal static int SetCompressWork_EntitySwarm(IncidentParms parms, int baseNum)
        {
          
            if (parms == null || baseNum <= 0)
            {
                return StateFalse(baseNum);
            }
            if (!EliteRaidMod.modEnabled || !EliteRaidMod.allowEntitySwarmValue)
            {
                return StateFalse(baseNum);
            }

            int maxPawnNum = CalculateCompressedPawnCount(baseNum);
            
            if (maxPawnNum >= baseNum)
            {
                return StateFalse(baseNum);
            }

            // 检查实体类型限制
            if (!EliteRaidMod.allowMechanoidsValue &&
                parms.faction?.def?.techLevel >= TechLevel.Industrial)  // 假设工业级以上可能有机械族
            {
                return StateFalse(baseNum);
            }

            int hashKey = parms.GetHashCode();
            m_CompressWork_EntitySwarm = new CompressWork(hashKey, baseNum, maxPawnNum);

            // 记录实际压缩率到日志
            if (EliteRaidMod.displayMessageValue)
            {
                float actualCompressionRatio = CalculateActualCompressionRatio(baseNum, maxPawnNum);
                Log.Message($"[EliteRaid] 实体群压缩完成: {baseNum} → {maxPawnNum} (实际压缩率: {actualCompressionRatio:F2})");
            }

#if DEBUG
         //   Log.Message($"@@@ Compressed Raid: ENTITY_SWARM, BASE_NUM={baseNum}, COMPRESSED_NUM={maxPawnNum}");
#endif

            return maxPawnNum;

            int StateFalse(int ret)
            {
                m_CompressWork_EntitySwarm.allowedCompress = false;
                return ret;
            }
        }

        // 获取实体群压缩工作的值
        internal static bool TryGetCompressWork_EntitySwarmValues(int hashKey, out int baseNum, out int maxPawnNum)
        {
            if (!m_CompressWork_EntitySwarm.allowedCompress || hashKey != m_CompressWork_EntitySwarm.hashKey)
            {
                baseNum = 0;
                maxPawnNum = 0;
                return false;
            }

            baseNum = m_CompressWork_EntitySwarm.baseNum;
            maxPawnNum = m_CompressWork_EntitySwarm.maxPawnNum;
            return true;
        }

        private static int CalculateCompressedPawnCount(int baseNum)
        {
            return General.GetenhancePawnNumber(baseNum);
        }

        private static float CalculateActualCompressionRatio(int baseNum, int maxPawnNum)
        {
            if (maxPawnNum <= 0) return 1f;
            return (float)baseNum / maxPawnNum;
        }

        // 新增：测试袭击记录系统的方法
        internal static void TestRaidRecordSystem()
        {
            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message("=== 袭击记录系统测试开始 ===");
            }
            
            // 清理现有记录
            lock (raidRecordsLock)
            {
                raidRecords.Clear();
            }
            
            // 创建测试派系
            var testFaction = Faction.OfMechanoids; // 使用机械族作为测试派系
            
            // 记录一些测试袭击
            RecordRaidInfo(100, testFaction, typeof(PawnGroupKindWorker_Normal), false);
            System.Threading.Thread.Sleep(100); // 等待100ms
            RecordRaidInfo(150, testFaction, typeof(PawnGroupKindWorker_Normal), true); // 空投袭击
            System.Threading.Thread.Sleep(100); // 等待100ms
            RecordRaidInfo(200, testFaction, typeof(PawnGroupKindWorker_Normal), false);
            
            // 输出所有记录
            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message(GetRaidRecordsDebugInfo());
            }
            
            // 测试查找功能
            if (TryFindClosestRaidRecord(testFaction, out var record, 30))
            {
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"找到最接近的记录: 数量={record.BaseNum}, 时间={record.RecordTime:HH:mm:ss.fff}, 空投={record.IsDropPodRaid}");
                }
            } else
            {
                Log.Warning("未找到任何记录");
            }
            
            // 测试清理功能
            ClearRaidRecordsForFaction(testFaction);
            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message("清理后的记录状态:");
                Log.Message(GetRaidRecordsDebugInfo());
                Log.Message("=== 袭击记录系统测试结束 ===");
            }
        }
    }
}
