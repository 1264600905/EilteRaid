using HarmonyLib;
using System.Collections.Generic;
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
                __result += offset; // 应用体型偏移量
                __result = Mathf.Max(__result, 0.05f); // 最小值限制（参考原逻辑）
            }
        }
    }
}