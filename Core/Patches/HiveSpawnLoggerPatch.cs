using System;
using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;
using System.Collections.Generic;

namespace EliteRaid
{
    [HarmonyPatch(typeof(TileMutatorWorker_InsectMegahive))]
    public static class HiveSpawnLoggerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("GenerateMegahiveEntrance")]
        [HarmonyPatch(new Type[] { typeof(Map), typeof(IntVec3), typeof(bool) })]
        public static bool GenerateMegahiveEntrance_Prefix(Map map, IntVec3 entranceCell, bool spawnGravcore)
        {
            // 极简测试：输出日志确认补丁被调用
            Log.Message($"[HiveSpawnLogger] 虫巢生成补丁被调用！地图ID: {map?.uniqueID}, 入口位置: {entranceCell}, 生成重力核心: {spawnGravcore}");
            
            // 返回 true 让原始方法继续执行
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GeneratePostFog")]
        [HarmonyPatch(new Type[] { typeof(Map) })]
        public static bool GeneratePostFog_Prefix(Map map)
        {
            // 极简测试：输出日志确认补丁被调用
            Log.Message($"[HiveSpawnLogger] 虫巢后雾生成补丁被调用！地图ID: {map?.uniqueID}");
            
            // 返回 true 让原始方法继续执行
            return true;
        }
    }

    [HarmonyPatch(typeof(HiveUtility))]
    public static class HiveUtilitySpawnLoggerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SpawnHive")]
        [HarmonyPatch(new Type[] { typeof(IntVec3), typeof(Map), typeof(WipeMode), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        public static bool SpawnHive_Prefix(
            IntVec3 spawnCell,
            Map map,
            WipeMode wipeMode,
            bool spawnInsectsImmediately,
            bool canSpawnHives,
            bool canSpawnInsects,
            bool dormant,
            bool aggressive,
            bool spawnJellyImmediately,
            bool spawnSludge)
        {
            // 极简测试：输出日志确认补丁被调用
            Log.Message($"[HiveSpawnLogger] HiveUtility.SpawnHive 被调用！位置: {spawnCell}, 地图ID: {map?.uniqueID}, 立即生成虫族: {spawnInsectsImmediately}, 可生成虫巢: {canSpawnHives}, 可生成虫族: {canSpawnInsects}");
            
            // 返回 true 让原始方法继续执行
            return true;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_WastepackInfestation))]
    public static class WastepackInfestationLoggerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("TryExecuteWorker")]
        [HarmonyPatch(new Type[] { typeof(IncidentParms) })]
        public static bool TryExecuteWorker_Prefix(IncidentWorker_WastepackInfestation __instance, IncidentParms parms)
        {
            // 检查是否启用虫族压缩
            if (!EliteRaidMod.allowInsectoidsValue || !EliteRaidMod.modEnabled)
            {
                Log.Message($"[HiveSpawnLogger] 虫族压缩已禁用 (allowInsectoidsValue={EliteRaidMod.allowInsectoidsValue}, modEnabled={EliteRaidMod.modEnabled})");
                return true; // 让原始方法处理
            }

            Map map = parms.target as Map;
            Log.Message($"[HiveSpawnLogger] 废物包虫灾事件即将执行！地图ID: {map?.uniqueID}, 点数: {parms.points}, 生成中心: {parms.spawnCenter}");

            // 计算基础虫茧数量
            var cocoonsToSpawn = CocoonInfestationUtility.GetCocoonsToSpawn(parms.points).ToList();
            int baseNum = cocoonsToSpawn.Count;
            // 先按原来的压缩逻辑计算，然后再减半
            int compressedNum = General.GetenhancePawnNumber(baseNum);
            int maxCocoonNum = Math.Max(1, compressedNum / 2);

            Log.Message($"[HiveSpawnLogger] 虫茧基础数量: {baseNum}, 压缩后数量: {compressedNum}, 最终数量: {maxCocoonNum}");

            if (maxCocoonNum >= baseNum)
            {
                Log.Message($"[HiveSpawnLogger] 虫茧数量不需要压缩 ({baseNum} ≤ {maxCocoonNum})，使用原始逻辑");
                return true; // 让原始方法处理
            }

            // 生成压缩后的虫茧
            IntVec3 spawnCenter = IntVec3.Invalid;
            
            // 通过反射调用私有方法 GetSpawnCenter
            MethodInfo getSpawnCenterMethod = AccessTools.Method(typeof(IncidentWorker_WastepackInfestation), "GetSpawnCenter", new Type[] { typeof(IncidentParms), typeof(Map) });
            if (getSpawnCenterMethod != null)
            {
                spawnCenter = (IntVec3)getSpawnCenterMethod.Invoke(__instance, new object[] { parms, map });
            }
            
            if (!spawnCenter.IsValid)
            {
                Log.Error("[HiveSpawnLogger] 无法找到有效的生成中心");
                return true; // 让原始方法处理
            }

            List<Thing> compressedCocoons = new List<Thing>();
            EliteLevelManager.GenerateLevelDistribution(baseNum);
            int order = PowerupUtility.GetNewOrder();

            Log.Message($"[HiveSpawnLogger] 开始生成压缩后的虫茧: {maxCocoonNum} 个 (原始: {baseNum}个)");

            // 生成压缩后的虫茧
            for (int i = 0; i < maxCocoonNum && i < cocoonsToSpawn.Count; i++)
            {
                ThingDef cocoonDef = cocoonsToSpawn[i];
                IntVec3 spawnPos;
                
                if (CocoonInfestationUtility.TryFindCocoonSpawnPositionNear(spawnCenter, map, cocoonDef, out spawnPos))
                {
                    // 创建虫茧生成器
                    CocoonSpawner cocoonSpawner = (CocoonSpawner)ThingMaker.MakeThing(ThingDefOf.CocoonSpawner, null);
                    cocoonSpawner.cocoon = cocoonDef;
                    cocoonSpawner.groupID = Find.UniqueIDsManager.GetNextCocoonGroupID();

                    // 生成虫茧
                    Thing spawnedCocoon = GenSpawn.Spawn(cocoonSpawner, spawnPos, map, WipeMode.FullRefund);
                    compressedCocoons.Add(spawnedCocoon);

                    Log.Message($"[HiveSpawnLogger] 生成虫茧: {cocoonDef.defName} 在位置 {spawnPos}");
                }
            }

            if (compressedCocoons.Any())
            {
                Log.Message($"[HiveSpawnLogger] 虫茧生成完成: {compressedCocoons.Count} 个 (原始: {baseNum}个)");

                // 显示压缩消息
                if (EliteRaidMod.displayMessageValue)
                {
                    Messages.Message($"[EliteRaid] 污染虫灾已压缩: {baseNum} → {compressedCocoons.Count}", MessageTypeDefOf.NeutralEvent);
                }

                return false; // 阻止原始方法执行
            }
            else
            {
                Log.Warning($"[HiveSpawnLogger] 虫茧生成失败: 生成了0个虫茧");
                return true; // 让原始方法处理
            }
        }
    }

    [HarmonyPatch(typeof(CocoonInfestationUtility))]
    public static class CocoonInfestationUtilityLoggerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SpawnCocoonInfestation")]
        [HarmonyPatch(new Type[] { typeof(IntVec3), typeof(Map), typeof(float) })]
        public static bool SpawnCocoonInfestation_Prefix(IntVec3 root, Map map, float points)
        {
            // 极简测试：输出日志确认补丁被调用
            Log.Message($"[HiveSpawnLogger] 虫茧虫灾生成被调用！生成中心: {root}, 地图ID: {map?.uniqueID}, 点数: {points}");
            
            // 返回 true 让原始方法继续执行
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetCocoonsToSpawn")]
        [HarmonyPatch(new Type[] { typeof(float) })]
        public static bool GetCocoonsToSpawn_Prefix(float points)
        {
            // 极简测试：输出日志确认补丁被调用
            Log.Message($"[HiveSpawnLogger] 获取虫茧生成列表被调用！点数: {points}");
            
            // 返回 true 让原始方法继续执行
            return true;
        }
    }
}