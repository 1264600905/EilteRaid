using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EliteRaid
{
    public static class BodySizePatch
    {
        /// <summary>
        /// 修改指定小人的体型（通过偏移量）
        /// </summary>
        /// <param name="pawn">目标小人</param>
        /// <param name="sizeOffset">体型偏移量（可正可负，例如+0.2表示增大20%体型）</param>
        public static void SetBodySizeOffset(Pawn pawn, float sizeOffset)
        {
            if (pawn == null || pawn.Destroyed) return;

            // 更新偏移量字典
            if (sizeOffset == 0)
            {
                if (Pawn_BodySize_Patch.customBodySizeOffsets.ContainsKey(pawn))
                {
                    Pawn_BodySize_Patch.customBodySizeOffsets.Remove(pawn);
                }
            } else
            {
                Pawn_BodySize_Patch.customBodySizeOffsets[pawn] = sizeOffset;
            }

            // 触发缓存更新（如果使用原逻辑的缓存机制）
            // 注意：若原Mod有缓存机制需手动刷新，此处假设需调用RegenerateCache
            //if (pawn.TryGetComp<CompCachedPawnData>() != null) // 示例判断，需根据实际缓存实现调整
            //{
            //    pawn.TryGetComp<CompCachedPawnData>().RegenerateCache();
            //}
        }

        /// <summary>
        /// 清理无效的Pawn引用（防止内存泄漏）
        /// </summary>
        public static void CleanupInvalidPawns()
        {
            var invalidPawns = Pawn_BodySize_Patch.customBodySizeOffsets.Keys
                .Where(pawn => pawn == null || pawn.Destroyed)
                .ToList();

            foreach (var pawn in invalidPawns)
            {
                Pawn_BodySize_Patch.customBodySizeOffsets.Remove(pawn);
            }

            if (invalidPawns.Count > 0 && EliteRaidMod.displayMessageValue)
            {
                Log.Message($"[EliteRaid] 清理了 {invalidPawns.Count} 个无效的体型修改引用");
            }
        }

        /// <summary>
        /// 测试体型修改功能
        /// </summary>
        /// <param name="pawn">测试目标</param>
        public static void TestBodySizeModification(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return;

            float originalSize = pawn.BodySize;
            Log.Message($"[EliteRaid] 测试体型修改 - {pawn.LabelCap} 原始体型: {originalSize:F2}");
            
            // 测试增大体型
            SetBodySizeOffset(pawn, 0.5f);
            Log.Message($"[EliteRaid] 测试体型修改 - {pawn.LabelCap} 增大后体型: {pawn.BodySize:F2}");
            
            // 测试减小体型
            SetBodySizeOffset(pawn, -0.3f);
            Log.Message($"[EliteRaid] 测试体型修改 - {pawn.LabelCap} 减小后体型: {pawn.BodySize:F2}");
            
            // 恢复原始体型
            SetBodySizeOffset(pawn, 0f);
            Log.Message($"[EliteRaid] 测试体型修改 - {pawn.LabelCap} 恢复后体型: {pawn.BodySize:F2}");
        }
    }

    [HarmonyPatch(typeof(Pawn), "get_BodySize")]
    public static class Pawn_BodySize_Patch
    {
        // 存储自定义体型偏移量（键：Pawn，值：体型偏移量）
        public static readonly Dictionary<Pawn, float> customBodySizeOffsets = new Dictionary<Pawn, float>();

        // 补丁方法：修改BodySize返回值
        [HarmonyPostfix]
        public static void Postfix(ref float __result, Pawn __instance)
        {
            if (customBodySizeOffsets.TryGetValue(__instance, out float offset))
            {
                float originalSize = __result;
                __result += offset; // 应用体型偏移量
                __result = Mathf.Max(__result, 0.05f); // 最小值限制（参考原逻辑）
                __result = Mathf.Min(__result, 5.0f); // 最大值限制，防止体型过大
                
                if (EliteRaidMod.displayMessageValue && Math.Abs(originalSize - __result) > 0.01f)
                {
                    Log.Message($"[EliteRaid] 体型修改: {__instance.LabelCap} 原始={originalSize:F2}, 修改后={__result:F2}, 偏移={offset:F2}");
                }
            }
        }
    }
}