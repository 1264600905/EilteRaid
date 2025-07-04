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
    }
}