using System;
using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;
using System.Collections.Generic;

namespace EliteRaid
{
 
    [HarmonyPatch(typeof(HiveUtility))]
    public static class HiveUtilitySpawnLoggerPatch
    {

    [HarmonyPatch(typeof(IncidentWorker_WastepackInfestation))]
    public static class WastepackInfestationLoggerPatch
    {
        private static int lastEventTick = -1;
        private static bool isProcessing = false;

        [HarmonyPrefix]
        [HarmonyPatch("TryExecuteWorker")]
        [HarmonyPatch(new Type[] { typeof(IncidentParms) })]
        public static bool TryExecuteWorker_Prefix(IncidentWorker_WastepackInfestation __instance, IncidentParms parms, ref bool __result)
        {
            // 检查是否在同一tick内重复执行
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick == lastEventTick || isProcessing)
            {
                __result = false;
                return false;
            }

            // 检查是否启用虫族压缩
            if (!EliteRaidMod.allowInsectoidsValue || !EliteRaidMod.modEnabled)
            {
                return true;
            }

            try
            {
                isProcessing = true;
                Map map = parms.target as Map;
                
                // 计算基础虫茧数量
                var cocoonsToSpawn = CocoonInfestationUtility.GetCocoonsToSpawn(parms.points).ToList();
                int baseNum = cocoonsToSpawn.Count;
                int compressedNum = General.GetenhancePawnNumber(baseNum);
                int maxCocoonNum = Math.Max(1, compressedNum)*2;

                if (maxCocoonNum >= baseNum)
                {
                    return true;
                }

                // 获取生成中心点
                IntVec3 spawnCenter = IntVec3.Invalid;
                MethodInfo getSpawnCenterMethod = AccessTools.Method(typeof(IncidentWorker_WastepackInfestation), "GetSpawnCenter");
                if (getSpawnCenterMethod != null)
                {
                    spawnCenter = (IntVec3)getSpawnCenterMethod.Invoke(__instance, new object[] { parms, map });
                }
                
                if (!spawnCenter.IsValid)
                {
                    return true;
                }

                // 生成压缩后的虫茧
                List<Thing> compressedCocoons = new List<Thing>();
                for (int i = 0; i < maxCocoonNum && i < cocoonsToSpawn.Count; i++)
                {
                    ThingDef cocoonDef = cocoonsToSpawn[i];
                    if (CocoonInfestationUtility.TryFindCocoonSpawnPositionNear(spawnCenter, map, cocoonDef, out IntVec3 spawnPos))
                    {
                        CocoonSpawner spawner = (CocoonSpawner)ThingMaker.MakeThing(ThingDefOf.CocoonSpawner);
                        spawner.cocoon = cocoonDef;
                        spawner.groupID = Find.UniqueIDsManager.GetNextCocoonGroupID();
                        compressedCocoons.Add(GenSpawn.Spawn(spawner, spawnPos, map));
                    }
                }

                if (compressedCocoons.Any())
                {
                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 污染虫灾已压缩: {baseNum} → {compressedCocoons.Count}");
                    }
                    Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), 
                        baseNum, compressedCocoons.Count/2,
                        General.GetcompressionRatio(baseNum, compressedCocoons.Count/2).ToString("F2"), 0), 
                        MessageTypeDefOf.NeutralEvent, true);

                    // 使用原版方法发送信件
                    MethodInfo sendLetterMethod = AccessTools.Method(typeof(IncidentWorker), "SendStandardLetter", 
                        new Type[] { typeof(IncidentParms), typeof(LookTargets), typeof(NamedArgument[]) });
                    if (sendLetterMethod != null)
                    {
                        sendLetterMethod.Invoke(__instance, new object[] { 
                            parms, 
                            new LookTargets(compressedCocoons), 
                            Array.Empty<NamedArgument>() 
                        });
                    }

                    Find.TickManager.slower.SignalForceNormalSpeedShort();
                    
                    lastEventTick = currentTick;
                    __result = true;
                    return false;
                }

                return true;
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
}}