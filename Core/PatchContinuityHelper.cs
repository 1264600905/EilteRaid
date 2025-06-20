﻿using EliteRaid;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EliteRaid
{
    internal static class PatchContinuityHelper
    {
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

            // 新增压缩率计算（确保 baseNum >= compressionRatio 时才压缩）
            if (EliteRaidMod.useCompressionRatio && baseNum >= StaticVariables.DEFAULT_MAX_ENEMY)
            {
                int temp = (int)(baseNum / EliteRaidMod.compressionRatio);
                maxPawnNum = Math.Max(temp, EliteRaidMod.maxRaidEnemy);
              
            } else
            {
                maxPawnNum = EliteRaidMod.maxRaidEnemy; // 保持原有逻辑或调整为 baseNum
            }
            if (maxPawnNum >= baseNum)
            {
                return StateFalse(options);
            }

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
                int maxPawnNum = EliteRaidMod.maxRaidEnemy;

                if (EliteRaidMod.useCompressionRatio && baseNum > StaticVariables.DEFAULT_MAX_ENEMY)
                {
                    int temp = (int)(baseNum / EliteRaidMod.compressionRatio);
                    maxPawnNum = Math.Max(temp, EliteRaidMod.maxRaidEnemy);
                }

                if (maxPawnNum >= baseNum)
                {
                    m_CompressWork_GeneratePawns.allowedCompress = false;
                    __result = criticalOptions.Concat(normalOptions);
                    return;
                }

                // 4. 实现真正的"压缩"：合并多个敌人为一个更强的敌人
                List<PawnGenOption> compressedOptions = CompressPawnOptions(normalOptions, maxPawnNum);

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
        private static List<PawnGenOption> CompressPawnOptions(List<PawnGenOption> options, int targetCount)
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
            int maxPawnNum = EliteRaidMod.maxRaidEnemy;
            // 新增压缩率计算（确保 baseNum >= compressionRatio 时才压缩）
            if (EliteRaidMod.useCompressionRatio && baseNum >= StaticVariables.DEFAULT_MAX_ENEMY)
            {
                int temp = (int)(baseNum / EliteRaidMod.compressionRatio);
                maxPawnNum = Math.Max(temp, EliteRaidMod.maxRaidEnemy);
            } else
            {
                maxPawnNum = EliteRaidMod.maxRaidEnemy; // 保持原有逻辑或调整为 baseNum
            }
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

            int maxPawnNum = EliteRaidMod.maxRaidEnemy;
            // 新增压缩率计算（确保 baseNum >= compressionRatio 时才压缩）
            if (EliteRaidMod.useCompressionRatio && baseNum >= StaticVariables.DEFAULT_MAX_ENEMY)
            {
                int temp = (int)(baseNum / EliteRaidMod.compressionRatio);
                maxPawnNum = Math.Max(temp, EliteRaidMod.maxRaidEnemy);
            } else
            {
                maxPawnNum = EliteRaidMod.maxRaidEnemy; // 保持原有逻辑或调整为 baseNum
            }
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
    }
}
