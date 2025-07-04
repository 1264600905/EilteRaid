using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static EliteRaid.EliteLevelManager;
using static EliteRaid.PowerupUtility;
using static EliteRaid.CR_DummyForCompatibilityDefOf;
using static EliteRaid.EliteRaidMod;
using UnityEngine;
using System.Collections.Concurrent;
using System.Reflection;

namespace EliteRaid
{
    [HarmonyPatch(typeof(NewRatkin.Building_GuerrillaTunnel))]
    public static class RatkinTunnelPatch
    {
        // 存储当前隧道的Pawn等级映射
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, EliteLevel>> tunnelPawnLevelMappings =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, EliteLevel>>();

        // 存储已处理的Pawn
        private static readonly HashSet<int> processedPawnIds = new HashSet<int>();

        // 存储隧道的原始生成数量
        private static readonly ConcurrentDictionary<string, int> tunnelBaseNumbers =
            new ConcurrentDictionary<string, int>();

        // 每个等级的最大数量限制
        private const int MaxPerLevel = 10;

        // 压缩比例（可以通过设置调整）
        private static float compressionRatio = 0.5f;

        private static FieldInfo spawnedPawnsField; // 添加这一行声明字段
                                                    // 获取Building_GuerrillaTunnel实例中的spawnedPawns列表
        private static List<Pawn> GetSpawnedPawns(NewRatkin.Building_GuerrillaTunnel instance)
        {
            if (instance == null)
                return null;

            // 使用反射安全获取私有字段
            if (spawnedPawnsField == null)
            {
                spawnedPawnsField = AccessTools.Field(typeof(NewRatkin.Building_GuerrillaTunnel), "spawnedPawns");

                if (spawnedPawnsField == null)
                {
                    Log.Error("[EliteRaid] 无法通过反射找到Building_GuerrillaTunnel的spawnedPawns字段");
                    return null;
                }
            }

            return spawnedPawnsField.GetValue(instance) as List<Pawn>;
        }
        [HarmonyPatch("TrySpawnPawn")]
        [HarmonyPrefix]
        public static bool TrySpawnPawn_Prefix(NewRatkin.Building_GuerrillaTunnel __instance, ref bool __result)
        {
            try
            {
                if(EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid]TrySpawnPawn_Prefix已经执行!");
                if (__instance == null)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Error("[EliteRaid] TrySpawnPawn_Prefix: __instance为空");
                    return true;
                }

                string tunnelKey = $"{__instance.thingIDNumber}_{__instance.Map?.GetHashCode() ?? 0}";

                // 获取或初始化该隧道的基础生成数量
                if (!tunnelBaseNumbers.TryGetValue(tunnelKey, out int baseNum))
                {
                    baseNum = (int)(__instance.eventPoint / 500f); // 根据eventPoint估算基础数量
                    tunnelBaseNumbers[tunnelKey] = baseNum;
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 初始化隧道 {tunnelKey} 的基础生成数量: {baseNum}, eventPoint: {__instance.eventPoint}");
                }

                // 计算压缩后的最大数量
                int maxPawnNum = General.GetenhancePawnNumber(baseNum);
                float calculatedRatio = General.GetcompressionRatio(baseNum, maxPawnNum);
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 基础数量: {baseNum}, 计算的最大数量: {maxPawnNum}, 压缩比例: {calculatedRatio:F2}");

                if (maxPawnNum >= baseNum)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 不需要压缩 (maxPawnNum >= baseNum), 继续执行原始方法");
                    return true; // 不需要压缩
                }

                // 初始化该隧道的等级分布（如果还没有）
                if (!tunnelPawnLevelMappings.ContainsKey(tunnelKey))
                {
                    var levelMapping = new ConcurrentDictionary<int, EliteLevel>();
                    EliteLevelManager.GenerateLevelDistribution(baseNum);

                    // 生成等级分布
                    var levelGroups = new Dictionary<int, List<EliteLevel>>();
                    int enhancePawnNumber = EliteLevelManager.getCurrentLevelDistributionNum();
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 为隧道 {tunnelKey} 生成等级分布, 基础数量: {baseNum}, 增强数量: {enhancePawnNumber}");

                    for (int i = 0; i < enhancePawnNumber; i++)
                    {
                        var eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                        int cappedLevel = Math.Min(eliteLevel.Level, 7);

                        if (!levelGroups.ContainsKey(cappedLevel))
                        {
                            levelGroups[cappedLevel] = new List<EliteLevel>();
                        }

                        if (levelGroups[cappedLevel].Count < MaxPerLevel)
                        {
                            levelGroups[cappedLevel].Add(eliteLevel);
                        }
                    }
                    if (EliteRaidMod.displayMessageValue)
                    {
                        // 记录每个等级的数量
                        foreach (var group in levelGroups.OrderBy(g => g.Key))
                        {
                            Log.Message($"[EliteRaid] 隧道 {tunnelKey} 等级 {group.Key} 的数量: {group.Value.Count}");
                        }
                    }
                     

                    // 将分组数据展平并存储
                    int pawnIndex = 0;
                    foreach (var group in levelGroups.OrderBy(g => g.Key))
                    {
                        foreach (var level in group.Value)
                        {
                            levelMapping[pawnIndex++] = level;
                        }
                    }

                    tunnelPawnLevelMappings[tunnelKey] = levelMapping;
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 为隧道 {tunnelKey} 生成等级分布，共 {levelMapping.Count} 个等级, 最大允许生成数量: {maxPawnNum}");
                }

                // 获取当前已生成的Pawn数量
                int currentCount = GetSpawnedPawns(__instance)?.Count ?? 0;
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 当前已生成Pawn数量: {currentCount}, 最大允许数量: {maxPawnNum}");

                // 如果超过压缩后的最大数量，停止生成
                if (currentCount >= maxPawnNum)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 已达到最大生成数量 {maxPawnNum}，停止生成");
                    __result = false;
                    return false;
                }

                // 继续原始生成逻辑
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 继续生成Pawn, 当前数量: {currentCount}, 最大数量: {maxPawnNum}");
                return true;
            } catch (Exception ex)
            {
                if (EliteRaidMod.displayMessageValue) Log.Error($"[EliteRaid] TrySpawnPawn_Prefix 异常: {ex}");
                return true; // 出错时继续执行原始方法
            }
        }

        // 修正Postfix方法参数，移除___spawnedPawn，使用out参数pawn
        [HarmonyPatch("TrySpawnPawn")]
        [HarmonyPostfix]
        public static void TrySpawnPawn_Postfix(NewRatkin.Building_GuerrillaTunnel __instance, bool __result, Pawn pawn)
        {
            try
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid]TrySpawnPawn_Postfix已经执行!");
                // 通过TrySpawnPawn的out参数获取生成的Pawn，而非不存在的___spawnedPawn
                if (!__result || pawn == null || !EliteRaidMod.modEnabled)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 不处理: __result={__result}, pawn={pawn?.Name.ToString() == "null"}, modEnabled={EliteRaidMod.modEnabled}");
                    return;
                }

                string tunnelKey = $"{__instance.thingIDNumber}_{__instance.Map?.GetHashCode() ?? 0}";

                // 检查是否已处理过这个Pawn
                if (processedPawnIds.Contains(pawn.thingIDNumber))
                {
                    if (EliteRaidMod.displayMessageValue)
                        Log.Message($"[EliteRaid] Pawn {pawn.Name} ({pawn.thingIDNumber}) 已处理过，跳过");
                    return;
                }

                // 获取该隧道的等级映射
                if (tunnelPawnLevelMappings.TryGetValue(tunnelKey, out var levelMapping))
                {
                    int currentIndex = GetSpawnedPawns(__instance)?.Count - 1 ?? 0;
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 尝试为Pawn {pawn.Name} ({pawn.thingIDNumber}) 应用等级, 当前索引: {currentIndex}, 等级映射数量: {levelMapping.Count}");

                    if (levelMapping.TryGetValue(currentIndex, out var eliteLevel))
                    {
                        if (EliteRaidMod.displayMessageValue)
                            Log.Message($"[EliteRaid] 为Pawn {pawn.Name} ({pawn.thingIDNumber}) 应用等级 {eliteLevel.Level}, 当前索引: {currentIndex}");

                        // 应用增益效果
                        if (EliteRaidMod.AllowCompress(pawn))
                        {
                            // 应用等级和Hediff
                            int order = PowerupUtility.GetNewOrder();
                            Hediff powerup = PowerupUtility.SetPowerupHediff(pawn, order);
                            if (powerup != null)
                            {
                                PowerupUtility.TrySetStatModifierToHediff(powerup, eliteLevel);
                                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 成功为Pawn {pawn.Name} ({pawn.thingIDNumber}) 设置Hediff和等级 {eliteLevel.Level}");
                            } else
                            {
                                Log.Error($"[EliteRaid] 无法为Pawn {pawn.Name} ({pawn.thingIDNumber}) 设置Hediff");
                            }

                            // 标记为已处理
                            processedPawnIds.Add(pawn.thingIDNumber);
                        } else
                        {
                            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 不允许压缩Pawn {pawn.Name} ({pawn.thingIDNumber})");
                        }
                    } else
                    {
                       // Log.Error($"[EliteRaid] 找不到索引 {currentIndex} 对应的等级映射, 映射数量: {levelMapping.Count}");
                    }
                } else
                {
                   // Log.Error($"[EliteRaid] 找不到隧道 {tunnelKey} 的等级映射");
                }

                // 检查是否需要显示压缩消息
                if (tunnelBaseNumbers.TryGetValue(tunnelKey, out int baseNum))
                {
                    int currentCount = GetSpawnedPawns(__instance)?.Count ?? 0;
                    int maxPawnNum = General.GetenhancePawnNumber(baseNum);
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 检查是否显示压缩消息: 当前数量 {currentCount}, 最大数量 {maxPawnNum}");

                    if (currentCount == maxPawnNum)
                    {
                        float ratio = General.GetcompressionRatio(baseNum, maxPawnNum);
                        Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(),
                            baseNum, maxPawnNum, ratio.ToString("F2"), maxPawnNum),
                            MessageTypeDefOf.NeutralEvent, true);

                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 隧道 {tunnelKey} 已达到最大数量，显示压缩消息: 基础数量 {baseNum}, 最大数量 {maxPawnNum}, 压缩比例 {ratio:F2}");

                        // 清理该隧道的数据
                        tunnelPawnLevelMappings.TryRemove(tunnelKey, out _);
                        tunnelBaseNumbers.TryRemove(tunnelKey, out _);
                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 清理隧道 {tunnelKey} 的缓存数据");
                    }
                } else
                {
                   // Log.Error($"[EliteRaid] 找不到隧道 {tunnelKey} 的基础数量");
                }
            } catch (Exception ex)
            {
                if (EliteRaidMod.displayMessageValue)
                    Log.Error($"[EliteRaid] TrySpawnPawn_Postfix 异常: {ex}");
            }
        }
        public static void RegisterRatkinTunnelPatches(Harmony harmony)
        {
            //fxz.solaris.ratkinracemod.odyssey
            if (ModLister.GetActiveModWithIdentifier("solaris.ratkinracemod") != null|| ModLister.GetActiveModWithIdentifier("fxz.solaris.ratkinracemod.odyssey") != null)
            {
                if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] 加载鼠族压缩补丁!");
            } else
            {
                if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] 需要加载鼠族模组才能使用此功能");
                return;
            }

            try
            {
                // 目标类：Building_GuerrillaTunnel
                Type targetClass = typeof(NewRatkin.Building_GuerrillaTunnel);
                if (targetClass == null)
                {
                    Log.Error("[EliteRaid] 找不到目标类 NewRatkin.Building_GuerrillaTunnel，跳过注册");
                    return;
                }
                if (EliteRaidMod.displayMessageValue)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 开始注册鼠族隧道补丁，目标类: {targetClass.FullName}");
                }

                // 获取TrySpawnPawn方法
                MethodInfo trySpawnPawnMethod = AccessTools.Method(targetClass, "TrySpawnPawn");
                if (trySpawnPawnMethod == null)
                {
                    // 尝试匹配任意参数的TrySpawnPawn方法
                    trySpawnPawnMethod = targetClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "TrySpawnPawn");
                }
                if (trySpawnPawnMethod == null)
                {
                    Log.Error("[EliteRaid] 找不到 TrySpawnPawn 方法，无法注册补丁");
                    return;
                }

                // 注册TrySpawnPawn的Prefix和Postfix补丁
                HarmonyMethod prefix = new HarmonyMethod(typeof(RatkinTunnelPatch), nameof(TrySpawnPawn_Prefix));
                HarmonyMethod postfix = new HarmonyMethod(typeof(RatkinTunnelPatch), nameof(TrySpawnPawn_Postfix));
                harmony.Patch(trySpawnPawnMethod, prefix: prefix, postfix: postfix);

                // 注册Destroy方法补丁 - 修正：针对基类Verse.ThingWithComps的Destroy方法
                Type baseClass = typeof(Verse.ThingWithComps);
                MethodInfo destroyMethod = AccessTools.Method(baseClass, "Destroy", new[] { typeof(DestroyMode) })
                                         ?? AccessTools.Method(baseClass, "Destroy");

                if (EliteRaidMod.displayMessageValue)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message("[EliteRaid] 鼠族隧道补丁注册成功");
                }
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册鼠族隧道补丁失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}