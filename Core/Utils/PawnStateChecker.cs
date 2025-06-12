using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EliteRaid
{
  
    class PawnStateChecker
    {
        public static bool CanCompressPawn(Pawn pawn)
        {
            try
            {
                // 1. 基础过滤：已被标记为删除、正在生成中的pawn
                if (pawn.Discarded || PawnGenerator.IsBeingGenerated(pawn))
                    return false;

                // 2. 派系首领判断（不可压缩）
                if (PawnUtility.IsFactionLeader(pawn))
                    return false;

                // 3. 任务相关判断（不可压缩）
                if (IsInQuestOrTale(pawn))
                    return false;

                // 4. 特殊状态判断（不可压缩）
                if (HasSpecialStatus(pawn))
                    return false;

                // 5. 生成状态与保留标记（不可压缩）
                if (IsSpawnedOrForcedKept(pawn))
                    return false;
            } catch (Exception e) {
               return false;
                Log.Warning("单位身上有任务，无法压缩" + pawn.Name+ "信息"+e.Message);
            }

            // 允许压缩
            return true;
        }
        public static  bool IsInQuestOrTale(Pawn pawn)
        {
            //作为心灵仪式仪式长
            if (pawn.kindDef.isGoodPsychicRitualInvoker)
            {
                return true;
            }
            // 被任务占用或正在生成任务相关pawn
            if (QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn))
                return true;

            // 与活跃故事线相关（如囚犯交换、盟友任务）
            if (Find.TaleManager.AnyActiveTaleConcerns(pawn))
                return true;

            // 任务访客（如暂住的NPC）
            if (pawn.IsQuestLodger())
                return true;

            return false;
        }

        public static bool HasSpecialStatus(Pawn pawn)
        {
            // 精神状态异常（狂暴、痴呆等）
            //if (pawn.InMentalState)
            //    return true;

            //// 倒地或死亡（保留尸体处理逻辑）
            //if (pawn.Downed || !pawn.Dead && pawn.Corpse == null)
            //    return true;

            // 殖民地囚犯或奴隶
            if (pawn.IsPrisonerOfColony || pawn.IsSlave)
                return true;

            // 商队成员或运输舱中的pawn
            if (pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
                return true;

            // 被定居点标记为出售
            if (PawnUtility.ForSaleBySettlement(pawn))
                return true;

            return false;
        }

        public static bool IsSpawnedOrForcedKept(Pawn pawn)
        {
            // 已生成到地图或父级已生成（避免重复处理）
            if (pawn.SpawnedOrAnyParentSpawned)
                return true;

            // 被Mod强制保留（如其他Mod的特殊需求）
            if (Find.WorldPawns.ForcefullyKeptPawns.Contains(pawn))
                return true;

            return false;
        }
    }
}
