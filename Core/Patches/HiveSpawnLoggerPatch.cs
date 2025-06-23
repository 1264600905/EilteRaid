//using HarmonyLib;
//using RimWorld;
//using System.Linq;
//using System.Reflection;
//using Verse;

//namespace EliteRaid
//{
//    [HarmonyPatch(typeof(GenStep_CaveHives), "TrySpawnHive")]
//    public static class HiveSpawnLoggerPatch
//    {
//        // 目标方法参数：Map map
//        // 补丁方法参数：__instance（实例） + 目标方法的参数（Map map）
//        static void Postfix(GenStep_CaveHives __instance, Map map)
//        {
//            // 确认 map 不为空
//            if (map == null) return;

//            // 这里可以通过 map 查找最新生成的 Hive（参考反编译代码中 TrySpawnHive 调用 GenSpawn.Spawn）
//            // 简化示例：仅输出日志确认执行
//            Log.Message("[HiveSpawnLogger] 洞穴虫巢生成补丁执行，地图ID: " + map.uniqueID);
//        }

//        public static void ApplyPatches(Harmony harmony)
//        {
          

//            try
//            {
//                // GenStep_CaveHives.TrySpawnHive 补丁
//                // 直接使用字符串指定方法名，跳过C#编译器检查
//                var originalMethod1 = AccessTools.Method(
//                    typeof(GenStep_CaveHives),
//                    "TrySpawnHive",  // 字符串硬编码
//                    new[] { typeof(Map) }
//                );

//                var postfixMethod1 = AccessTools.Method(
//                    typeof(HiveSpawnLoggerPatch),
//                    nameof(HiveSpawnLoggerPatch.Postfix)
//                );

//                if (originalMethod1 != null && postfixMethod1 != null)
//                {
//                    harmony.Patch(originalMethod1, postfix: new HarmonyMethod(postfixMethod1));
//                    Log.Message("[HiveLogger] GenStep_CaveHives.TrySpawnHive 补丁已注册");
//                } else
//                {
//                    Log.Error("[HiveLogger] 无法注册 GenStep_CaveHives.TrySpawnHive 补丁");
//                }
//            } catch (System.Exception e)
//            {
//                Log.Error($"[HiveLogger] 补丁注册异常: {e.Message}\n{e.StackTrace}");
//            }
//        }
//    }


//    [HarmonyPatch(typeof(CompSpawnerHives), "TrySpawnChildHive")]
//    public static class CompSpawnerHivesLoggerPatch
//    {
//        // 目标方法签名：bool TrySpawnChildHive(bool ignoreRoofedRequirement, out Hive newHive)
//        // 补丁方法参数需匹配：__instance + 输入参数 + ref/out 参数（用 ref 修饰）
//        static void Postfix(CompSpawnerHives __instance, bool ignoreRoofedRequirement, ref Hive newHive)
//        {
//            if (newHive != null)
//            {
//                Log.Message($"[HiveSpawnLogger] 虫巢繁殖成功！新虫巢坐标：{newHive.Position}");
//            }
//        }
//    }

//}