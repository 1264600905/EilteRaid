using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace EliteRaid
{
    [HarmonyPatch(typeof(IncidentWorker_FleshbeastAttack))]
    public static class FleshbeastAttackCompressionPatch
    {
        private static int timeVal = 60; // 增加间隔时间到60 ticks
        private static int lastEventTick = -1;
        private static bool isProcessing = false;

        [HarmonyPrefix]
        [HarmonyPatch("TryExecuteWorker")]
        public static bool TryExecuteWorker_Prefix(IncidentWorker_FleshbeastAttack __instance, IncidentParms parms, ref bool __result)
        {
            // 添加详细的调试日志
            int currentTick = Find.TickManager.TicksGame;
            Log.Message($"[EliteRaid Debug] 血肉兽袭击补丁被调用 - 当前Tick: {currentTick}, 上次Tick: {lastEventTick}, 正在处理: {isProcessing}");

            // 使用更严格的检查逻辑
            if (isProcessing)
            {
                Log.Warning("[EliteRaid] 检测到并发调用，跳过此次执行");
                __result = false;
                return false;
            }

            if (lastEventTick != -1 && currentTick - lastEventTick <= timeVal)
            {
                Log.Warning($"[EliteRaid] 检测到快速重复调用，间隔: {currentTick - lastEventTick} ticks，跳过此次执行");
                __result = false;
                return false;
            }

            // 检查是否启用血肉兽袭击压缩
            if (!EliteRaidMod.allowEntitySwarmValue || !EliteRaidMod.modEnabled)
            {
                return true;
            }

            try
            {
                isProcessing = true;
                Map map = parms.target as Map;

                // 计算原始血肉兽巢穴数量
                float originalPoints = parms.points;
                float originalBurrowPoints = originalPoints * 0.6f; // 使用原代码中的系数
                int originalBurrowCount = Mathf.CeilToInt(originalBurrowPoints / 500f); // 每个巢穴最多500点

                // 应用压缩逻辑
                int compressedBurrowCount = General.GetenhancePawnNumber(originalBurrowCount);
                int maxBurrowCount = Math.Max(1, compressedBurrowCount);

                if (maxBurrowCount >= originalBurrowCount)
                {
                    return true; // 不需要压缩
                }

                // 获取生成参数
                // 使用反射获取BurrowSpawnParms属性和ForThing方法
                FieldInfo burrowSpawnParmsField = AccessTools.Field(typeof(IncidentWorker_FleshbeastAttack), "BurrowSpawnParms");
                MethodInfo forThingMethod = AccessTools.Method(typeof(LargeBuildingSpawnParms), "ForThing", new[] { typeof(ThingDef) });

                // 创建完整的生成参数
                LargeBuildingSpawnParms spawnParms;
                if (burrowSpawnParmsField != null && forThingMethod != null)
                {
                    var baseParms = burrowSpawnParmsField.GetValue(null);
                    spawnParms = (LargeBuildingSpawnParms)forThingMethod.Invoke(baseParms, new object[] { ThingDefOf.PitBurrow });

                    // 应用我们的自定义设置
                    spawnParms.maxDistanceToColonyBuilding = -1f;
                    spawnParms.minDistToEdge = 10;
                    spawnParms.attemptNotUnderBuildings = true;
                    spawnParms.canSpawnOnImpassable = false;
                    spawnParms.attemptSpawnLocationType = SpawnLocationType.Outdoors;
                } else
                {
                    Log.Error("spawnParms设置错误！"); return false;
                }

                // 获取延迟参数
                FieldInfo emergenceDelayField = AccessTools.Field(typeof(IncidentWorker_FleshbeastAttack), "PitBurrowEmergenceDelayRangeTicks");
                FieldInfo spawnDelayField = AccessTools.Field(typeof(IncidentWorker_FleshbeastAttack), "FleshbeastSpawnDelayTicks");

                IntRange emergenceDelay = emergenceDelayField != null
                    ? (IntRange)emergenceDelayField.GetValue(null)
                    : new IntRange(420, 420);

                IntRange spawnDelay = spawnDelayField != null
                    ? (IntRange)spawnDelayField.GetValue(null)
                    : new IntRange(180, 180);

                // 生成压缩后的血肉兽巢穴
                List<Thing> compressedBurrows = new List<Thing>();
                float remainingPoints = originalPoints * 0.6f;

                for (int i = 0; i < maxBurrowCount && remainingPoints > 0f; i++)
                {
                    IntVec3 spawnPos;
                    if (!LargeBuildingCellFinder.TryFindCell(out spawnPos, map, spawnParms, null, null, false))
                    {
                        break; // 找不到合适位置
                    }
                  
                    float pointsForThisBurrow = Mathf.Min(remainingPoints, 500f);
                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 正在生成第 {i + 1}/{maxBurrowCount} 个血肉兽巢穴，点数: {pointsForThisBurrow}");
                    }
                    // 使用反射调用SpawnFleshbeastsFromPitBurrowEmergence方法
                    MethodInfo spawnMethod = AccessTools.Method(typeof(FleshbeastUtility), "SpawnFleshbeastsFromPitBurrowEmergence", new Type[] {
                        typeof(IntVec3), typeof(Map), typeof(float), typeof(IntRange), typeof(IntRange), typeof(bool)
                    });

                    if (spawnMethod != null)
                    {
                        Thing burrow = (Thing)spawnMethod.Invoke(null, new object[] {
                            spawnPos, map, pointsForThisBurrow, emergenceDelay, spawnDelay, true
                        });

                        if (burrow != null)
                        {
                            compressedBurrows.Add(burrow);
                            remainingPoints -= pointsForThisBurrow;
                        }
                    }
                }

                if (compressedBurrows.Any())
                {
                    // 只在这里输出一次压缩完成的消息
                    if (EliteRaidMod.displayMessageValue)
                    {
                        string compressionInfo = $"血肉兽袭击压缩完成: {originalBurrowCount} → {compressedBurrows.Count} (压缩比: {General.GetcompressionRatio(originalBurrowCount, compressedBurrows.Count):F2})";
                        Log.Message($"[EliteRaid] {compressionInfo}");
                    }

                    Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(),
                        originalBurrowCount, compressedBurrows.Count,
                        General.GetcompressionRatio(originalBurrowCount, compressedBurrows.Count).ToString("F2"), 0),
                        MessageTypeDefOf.NeutralEvent, true);

                    // 使用反射调用SendStandardLetter方法
                    try
                    {
                        MethodInfo sendLetterMethod = AccessTools.Method(typeof(IncidentWorker), "SendStandardLetter",
                            new Type[] { typeof(IncidentParms), typeof(LookTargets), typeof(NamedArgument[]) });
                        if (sendLetterMethod != null)
                        {
                            sendLetterMethod.Invoke(__instance, new object[] {
                                parms,
                                new LookTargets(compressedBurrows),
                                Array.Empty<NamedArgument>()
                            });
                        }

                        Find.TickManager.slower.SignalForceNormalSpeedShort();
                        lastEventTick = currentTick;
                        __result = true;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[EliteRaid] 发送通知时出错: {ex.Message}");
                        lastEventTick = currentTick; // 即使出错也要更新lastEventTick
                        __result = false;
                        return false;
                    }
                }

                lastEventTick = currentTick; // 确保在所有退出点都更新lastEventTick
                return true; // 继续执行原版方法
            }
            catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 血肉兽袭击压缩处理时出错: {ex.Message}");
                return true; // 出错时让原版方法处理
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
}