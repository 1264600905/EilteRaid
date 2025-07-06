using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace EliteRaid
{
    public static class DebugActions  // 类名可以带后缀
    {
        [DebugAction("EliteRaid精英袭击", "原生虫灾事件（10000点）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnEliteInfestationIncident()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("当前没有有效地图！", MessageTypeDefOf.RejectInput, false);
                return;
            }
            IntVec3 cell = UI.MouseCell();
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 10000f,
                infestationLocOverride = cell
            };
            bool result = IncidentDefOf.Infestation.Worker.TryExecute(parms);
            Messages.Message(result ? "[EliteRaid] 已触发原生虫灾事件（10000点）" : "触发失败", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("EliteRaid精英袭击", "鼠族隧道袭击（10000点）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnRatkinTunnelRaidIncident()
        {
            // 检查鼠族模组是否已加载
            if (ModLister.GetActiveModWithIdentifier("solaris.ratkinracemod") == null &&
    ModLister.GetActiveModWithIdentifier("fxz.Solaris.RatkinRaceMod.odyssey") == null &&
    ModLister.GetActiveModWithIdentifier("fxz.solaris.ratkinracemod.odyssey") == null)
{
    Messages.Message("[EliteRaid] 需要加载鼠族模组才能使用此功能", MessageTypeDefOf.RejectInput, false);
    return;
}

            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("当前没有有效地图！", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 获取鼠族派系
            FactionDef ratkinFactionDef = DefDatabase<FactionDef>.GetNamed("Rakinia");
            if (ratkinFactionDef == null)
            {
                Messages.Message("[EliteRaid] 未找到鼠族派系定义", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Faction ratkinFaction = Find.FactionManager.FirstFactionOfDef(ratkinFactionDef);
            if (ratkinFaction == null || !ratkinFaction.HostileTo(Faction.OfPlayer))
            {
                Messages.Message("[EliteRaid] 未找到敌对的鼠族派系", MessageTypeDefOf.RejectInput, false);
                return;
            }

            IntVec3 cell = UI.MouseCell();

            // 直接生成隧道生成器
            Thing tunnelSpawner = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RK_GuerrillaTunnelSpawner"));
            if (tunnelSpawner == null)
            {
                Messages.Message("[EliteRaid] 未找到鼠族隧道生成器定义", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 设置点数
            var eventPointField = tunnelSpawner.GetType().GetField("eventPoint");
            if (eventPointField != null)
            {
                eventPointField.SetValue(tunnelSpawner, 10000f);
            }

            // 设置派系标记（如果有这个字段）
            var factionField = tunnelSpawner.GetType().GetField("faction");
            if (factionField != null)
            {
                factionField.SetValue(tunnelSpawner, ratkinFaction);
            }

            // 生成隧道生成器
            Thing spawnedThing = GenSpawn.Spawn(tunnelSpawner, cell, map, WipeMode.FullRefund);
            if (spawnedThing != null)
            {
                Messages.Message("[EliteRaid] 已触发鼠族隧道袭击事件（10000点）", MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("触发失败", MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}