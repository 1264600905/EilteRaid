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

        // 通过反射获取spawnedPawns字段
        private static List<Pawn> GetSpawnedPawns(object instance)
        {
            if (instance == null) return null;
            var type = instance.GetType();
            var field = AccessTools.Field(type, "spawnedPawns");
            if (field == null) return null;
            return field.GetValue(instance) as List<Pawn>;
        }

        // 通过反射获取属性或字段值
        private static T GetMemberValue<T>(object instance, string memberName)
        {
            if (instance == null) return default;
            var type = instance.GetType();
            // 先找属性
            var prop = AccessTools.Property(type, memberName);
            if (prop != null) return (T)prop.GetValue(instance);
            // 再找字段
            var field = AccessTools.Field(type, memberName);
            if (field != null) return (T)field.GetValue(instance);
            return default;
        }

        [HarmonyPrefix]
        public static bool TrySpawnPawn_Prefix(object __instance, ref bool __result)
        {
            try
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid]TrySpawnPawn_Prefix已经执行!");
                if (__instance == null)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Error("[EliteRaid] TrySpawnPawn_Prefix: __instance为空");
                    return true;
                }

                int thingIDNumber = GetMemberValue<int>(__instance, "thingIDNumber");
                Map map = GetMemberValue<Map>(__instance, "Map");
                float eventPoint = GetMemberValue<float>(__instance, "eventPoint");
                string tunnelKey = $"{thingIDNumber}_{map?.GetHashCode() ?? 0}";

                // 获取或初始化该隧道的基础生成数量
                if (!tunnelBaseNumbers.TryGetValue(tunnelKey, out int baseNum))
                {
                    baseNum = (int)(eventPoint / 500f); // 根据eventPoint估算基础数量
                    tunnelBaseNumbers[tunnelKey] = baseNum;
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 初始化隧道 {tunnelKey} 的基础生成数量: {baseNum}, eventPoint: {eventPoint}");
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
            }
            catch (Exception ex)
            {
                if (EliteRaidMod.displayMessageValue) Log.Error($"[EliteRaid] TrySpawnPawn_Prefix 异常: {ex}");
                return true; // 出错时继续执行原始方法
            }
        }

        [HarmonyPostfix]
        public static void TrySpawnPawn_Postfix(object __instance, bool __result, Pawn pawn)
        {
            try
            {
                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid]TrySpawnPawn_Postfix已经执行!");
                if (!__result || pawn == null || !EliteRaidMod.modEnabled)
                {
                    if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 不处理: __result={__result}, pawn={pawn?.Name.ToString() == "null"}, modEnabled={EliteRaidMod.modEnabled}");
                    return;
                }

                int thingIDNumber = GetMemberValue<int>(__instance, "thingIDNumber");
                Map map = GetMemberValue<Map>(__instance, "Map");
                string tunnelKey = $"{thingIDNumber}_{map?.GetHashCode() ?? 0}";

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
                            int order = PowerupUtility.GetNewOrder();
                            Hediff powerup = PowerupUtility.SetPowerupHediff(pawn, order);
                            if (powerup != null)
                            {
                                PowerupUtility.TrySetStatModifierToHediff(powerup, eliteLevel);
                                if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 成功为Pawn {pawn.Name} ({pawn.thingIDNumber}) 设置Hediff和等级 {eliteLevel.Level}");
                            }
                            else
                            {
                                Log.Error($"[EliteRaid] 无法为Pawn {pawn.Name} ({pawn.thingIDNumber}) 设置Hediff");
                            }

                            processedPawnIds.Add(pawn.thingIDNumber);
                        }
                        else
                        {
                            if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 不允许压缩Pawn {pawn.Name} ({pawn.thingIDNumber})");
                        }
                    }
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

                        tunnelPawnLevelMappings.TryRemove(tunnelKey, out _);
                        tunnelBaseNumbers.TryRemove(tunnelKey, out _);
                        if (EliteRaidMod.displayMessageValue) Log.Message($"[EliteRaid] 清理隧道 {tunnelKey} 的缓存数据");
                    }
                }
            }
            catch (Exception ex)
            {
                if (EliteRaidMod.displayMessageValue)
                    Log.Error($"[EliteRaid] TrySpawnPawn_Postfix 异常: {ex}");
            }
        }

        public static void RegisterRatkinTunnelPatches(Harmony harmony)
        {
            //fxz.solaris.ratkinracemod.odyssey
            if (ModLister.GetActiveModWithIdentifier("solaris.ratkinracemod") != null||
             ModLister.GetActiveModWithIdentifier("fxz.Solaris.RatkinRaceMod.odyssey") != null||
             ModLister.GetActiveModWithIdentifier("fxz.solaris.ratkinracemod.odyssey") != null)
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
                Type targetClass = null;
                MethodInfo trySpawnPawnMethod = null;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    targetClass = assembly.GetType("NewRatkin.Building_GuerrillaTunnel");
                    if (targetClass != null)
                    {
                        trySpawnPawnMethod = AccessTools.Method(targetClass, "TrySpawnPawn");
                        if (trySpawnPawnMethod != null)
                            break;
                    }
                }
                if (targetClass == null || trySpawnPawnMethod == null)
                {
                    Log.Error("[EliteRaid] 找不到目标类或方法 NewRatkin.Building_GuerrillaTunnel.TrySpawnPawn，跳过注册");
                    return;
                }
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 开始注册鼠族隧道补丁，目标类: {targetClass.FullName}");
                }

                // 注册TrySpawnPawn的Prefix和Postfix补丁
                HarmonyMethod prefix = new HarmonyMethod(typeof(RatkinTunnelPatch), nameof(TrySpawnPawn_Prefix));
                HarmonyMethod postfix = new HarmonyMethod(typeof(RatkinTunnelPatch), nameof(TrySpawnPawn_Postfix));
                harmony.Patch(trySpawnPawnMethod, prefix: prefix, postfix: postfix);

                // Destroy方法补丁注册逻辑可保留或移除（如无需求可注释掉）
                // Type baseClass = typeof(Verse.ThingWithComps);
                // MethodInfo destroyMethod = AccessTools.Method(baseClass, "Destroy", new[] { typeof(DestroyMode) })
                //                          ?? AccessTools.Method(baseClass, "Destroy");

                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message("[EliteRaid] 鼠族隧道补丁注册成功");
                }
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册鼠族隧道补丁失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}