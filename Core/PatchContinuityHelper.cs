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
        // public static Pawn TryGeneratePawn(PawnKindDef pawnKind, Faction faction)
        // {
        //     Log.Message($"[EliteRaid][PatchContinuityHelper.cs:TryGeneratePawn@L141] 调用PawnGenerator.GeneratePawn, pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}");
        //     Pawn pawn = null;
        //     try
        //     {
        //         if (faction == Faction.OfHoraxCult)
        //         {
        //             PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
        //             int num = -1;
        //             bool flag = false;
        //             bool flag2 = false;
        //             bool flag3 = false;
        //             bool flag4 = true;
        //             bool flag5 = true;
        //             float num2 = 1f;
        //             bool flag6 = false;
        //             bool flag7 = true;
        //             bool flag8 = false;
        //             Log.Message($"[EliteRaid][TryGeneratePawn] HoraxCult专用生成: pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}");
        //             pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, pawnGenerationContext, num, flag, flag2, flag3, flag4, flag5, num2, flag6, flag7, flag8, false, true, false, false, false, false, 0.7f, 0.8f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)
        //             {
        //                 BiocodeApparelChance = 1f,
        //                 ForcedXenotype = XenotypeDefOf.Baseliner,
        //                 ProhibitedTraits = new List<TraitDef>() { TraitDef.Named("psychically deaf") }
        //             });
        //         }
        //         else
        //         {
        //             Log.Message($"[EliteRaid][TryGeneratePawn] 普通阵营生成: pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}");
        //             pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: 0.8f, biocodeApparelChance: 0.8f, allowFood: true)
        //             {
        //                 BiocodeApparelChance = 1f
        //             });
        //         }
        //         if (pawn == null)
        //         {
        //             Log.Warning($"[EliteRaid][TryGeneratePawn] 生成失败: pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}");
        //         }
        //         else
        //         {
        //             Log.Message($"[EliteRaid][TryGeneratePawn] 生成成功: pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}, pawn={pawn.LabelCap} ({pawn.GetType().Name})");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Error($"[EliteRaid][TryGeneratePawn] 生成异常: pawnKind={pawnKind?.defName}, faction={faction?.Name ?? faction?.ToString() ?? "null"}, Exception={ex}");
        //     }
        //     return pawn;
        // }

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
          
                Log.Message("捕获到当前生成的敌人阵营22"+ groupParms.faction);
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

                Log.Message("捕获到当前生成的敌人阵营11"+ faction);
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
    }
}
