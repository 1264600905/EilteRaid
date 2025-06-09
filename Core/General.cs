using EliteRaid;
using RimwoldEliteRaidProject.Core;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static EliteRaid.StaticVariables_ModCompatibility;

namespace EliteRaid
{
    internal static class General
    {
        internal enum MessageTypes : byte
        {
            Infomation = 0x00,
            Warning = 0x01,
            Error = 0x03,
            Debug = 0x04,
            DebugWarning = 0x05,
            DebugError = 0x06,
        }

        internal static void SendLog_Debug(MessageTypes type, string temp, params object[] args)
        {
            return;
#if DEBUG
            if (!Prefs.DevMode && (byte)type >= 0x04)
            {
                return;
            }
            if (args?.Any() ?? false)
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                }
            } else
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                }
            }
#endif
        }
        internal static void SendLog(MessageTypes type, string temp, params object[] args)
        {
            return;
            if (!Prefs.DevMode && (byte)type >= 0x04)
            {
                return;
            }
            if (args?.Any() ?? false)
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, String.Format(temp, args)));
                }
            } else
            {
                if (type == MessageTypes.Error || type == MessageTypes.DebugError)
                {
                    Log.Error(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                } else
                {
                    Log.Message(String.Format("[Compressed Raid] {0}: {1}", type, temp));
                }
            }
        }
        private static int hediffDefCounter = 0;
        public static bool m_CanTranspilerGeneratePawns = true;
        public static bool m_CanTranspilerGenerateAnimals = true;
        public static bool m_CanTranspilerEntitySwarm = true;        //是否启用实体增强
        public static HashSet<Type> m_AllowPawnGroupKindWorkerTypes = new HashSet<Type>();
        public static void GenerateAnything_Impl(List<Pawn> pawns, int baseNum, int maxPawnNum, bool raidFriendly)
        {
           
            EliteLevelManager.GenerateLevelDistribution(baseNum);
            int enhancePawnNumber = EliteLevelManager.getCurrentLevelDistributionNum();
            int order = PowerupUtility.GetNewOrder();
            int enhancedCount = 0;
            if (EliteRaidMod.useCompressionRatio&&EliteRaidMod.displayMessageValue)
            {
              //  Log.Message($"压缩后人数（压缩率{EliteRaidMod.compressionRatio}）：{enhancePawnNumber}");
            } else
            {
             //   Log.Message("最终输出人数" + enhancePawnNumber);
            }
          //  Log.Message($"压缩后人数（压缩率{EliteRaidMod.compressionRatio}）：{enhancePawnNumber}");
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                //DummyForCompatibility付与ここから
                if (MOD_MSER_Active)
                {
                    pawn.health.AddHediff(CR_DummyForCompatibilityDefOf.CR_DummyForCompatibility);
                }
                //DummyForCompatibility付与ここまで

                //Hediff仕込みここから
                if (EliteRaidMod.modEnabled && i < enhancePawnNumber)
                {
                    if (EliteRaidMod.AllowCompress(pawn))
                    {
                        // 创建新的HediffDef实例，确保每个pawn有独立的属性
                        HediffDef originalDef = HediffDef.Named($"CR_Powerup1");
                        //HediffDef cloneDef = new HediffDef();
                        //cloneDef.defName = $"CR_Powerup1";
                       

                        //// 复制所有原始定义的属性，包括UI显示相关的属性
                        //cloneDef.label = originalDef.label;
                        //cloneDef.description = originalDef.description;
                        //cloneDef.stages = new List<HediffStage>();
                        //cloneDef.defaultLabelColor = originalDef.defaultLabelColor;
                        //cloneDef.everCurableByItem = originalDef.everCurableByItem;
                        //cloneDef.hediffClass = originalDef.hediffClass;
                        // 复制其他可能影响UI显示的属性

                        //// 复制原始阶段数据
                        //foreach (var stage in originalDef.stages)
                        //{
                        //    HediffStage newStage = new HediffStage();
                        //    newStage.statOffsets = new List<StatModifier>();
                        //    newStage.statFactors = new List<StatModifier>();
                        //    newStage.capMods = new List<PawnCapacityModifier>();

                        //    // 复制阶段的UI相关属性
                        //    newStage.label = stage.label;
                        //    newStage.lifeThreatening = stage.lifeThreatening;
                        //    // 复制其他需要的阶段属性

                        //    cloneDef.stages.Add(newStage);
                        //}

                        // 添加到DefDatabase
                      //  DefDatabase<HediffDef>.Add(originalDef);

                        // 添加Hediff使用新创建的def
                        pawn.health.AddHediff(originalDef);
                        Hediff powerup = pawn.health.hediffSet.hediffs
                            .FirstOrDefault(h => h.def.defName == originalDef.defName);

                        if (powerup != null)
                        {
                            EliteLevel eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                           // Log.Message($"[EliteRaid] 为 pawn {pawn.Name} 分配精英等级：Level {eliteLevel.Level}同时其承伤是{eliteLevel.DamageFactor}移速是{eliteLevel.MoveSpeedFactor}");

                            // ✅ 修改：使用新的有效性检查
                            if (IsEliteLevelValid(eliteLevel))
                            {
                                GearRefiner.RefineGear(pawn, eliteLevel);
                                BionicsDataStore.AddBionics(pawn, eliteLevel);
                                DrugHediffDataStore.AddDrugHediffs(pawn, eliteLevel);

                                // 修正：检查原始标签是否已包含"精英等级"，避免重复
                                string originalLabel = originalDef.label;
                                powerup.def.label = "EliteLevelLabel".Translate(eliteLevel.Level);

                                //if (originalLabel.Contains("精英等级"))
                                //{
                                //    // 如果原始标签已经包含"精英等级"，则直接替换等级部分
                                //    powerup.def.label = Regex.Replace(originalLabel, @"精英等级 \d+", levelLabel);
                                //} else
                                //{
                                //    // 否则添加等级信息
                                //    powerup.def.label = $"{originalLabel} {levelLabel}";
                                //}

                                //// 构建详细描述
                                //var descSb = new StringBuilder();
                                //descSb.AppendLine(levelLabel);
                                //descSb.AppendLine($"承伤: {eliteLevel.DamageFactor:P0}");
                                //descSb.AppendLine($"移速: {eliteLevel.MoveSpeedFactor:P0}");
                                //descSb.AppendLine($"体型: {eliteLevel.ScaleFactor:F1}");
                                //descSb.AppendLine();
                                //descSb.AppendLine("特性:");

                                //if (eliteLevel.addBioncis) descSb.AppendLine("- 器官强化");
                                //if (eliteLevel.addDrug) descSb.AppendLine("- 药物强化");
                                //if (eliteLevel.refineGear) descSb.AppendLine("- 装备强化");
                                //if (eliteLevel.armorEnhance) descSb.AppendLine("- 护甲强化");
                                //if (eliteLevel.temperatureResist) descSb.AppendLine("- 温度抗性");
                                //if (eliteLevel.meleeEnhance) descSb.AppendLine("- 近战强化");
                                //if (eliteLevel.rangedSpeedEnhance) descSb.AppendLine("- 远程强化");
                                //if (eliteLevel.moveSpeedEnhance) descSb.AppendLine("- 提速强化");
                                //if (eliteLevel.giantEnhance) descSb.AppendLine("- 巨人化");
                                //if (eliteLevel.bodyPartEnhance) descSb.AppendLine("- 身体强化");
                                //if (eliteLevel.painEnhance) descSb.AppendLine("- 无痛强化");
                                //if (eliteLevel.moveSpeedResist) descSb.AppendLine("- 移速抵抗");
                                //if (eliteLevel.consciousnessEnhance) descSb.AppendLine("- 意识强化");

                                //powerup.def.description = descSb.ToString();

                                bool powerupEnable = PowerupUtility.TrySetStatModifierToHediff(powerup, eliteLevel);
                                if (powerupEnable)
                                {
                                    enhancedCount++;
                                }
                            } else
                            {
                                // 等级无效时，移除已添加的Hediff
                                pawn.health.RemoveHediff(powerup);
                              //  Log.Message($"[EliteRaid] 移除无效精英等级Hediff：{pawn.Name}");
                            }
                        }
                    }
                }
            }

            //DummyForCompatibility除去ここから
            if (MOD_MSER_Active)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    CR_DummyForCompatibility dummyHediff = p.health.hediffSet.hediffs.Where(x => x is CR_DummyForCompatibility).Cast<CR_DummyForCompatibility>().FirstOrDefault();
                    if (dummyHediff != null)
                    {
                        p.health.RemoveHediff(dummyHediff);
                    }
                }
            }
            //DummyForCompatibility除去ここまで
          
                if (enhancedCount > 0)
                {
                int finalNum = GetenhancePawnNumber(baseNum,enhancePawnNumber);
                Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum
                 , finalNum, GetcompressionRatio(baseNum, maxPawnNum)
                 , finalNum), MessageTypeDefOf.NeutralEvent, true);
            } else
                {
                   // Messages.Message(String.Format("CR_RaidCompressedMassageNotEnhanced".Translate(), baseNum * EliteRaidMod.multipleRaid, maxPawnNum), MessageTypeDefOf.NeutralEvent, true);
                }
          
        }
        public static float GetcompressionRatio(int baseNum, int maxPawnNum)
        {
            float compressionRatio = (float)(Math.Ceiling((double)(baseNum  / maxPawnNum)));
            if (EliteRaidMod.useCompressionRatio)
            {
                return EliteRaidMod.compressionRatio;
            } else
            {
                return compressionRatio;
            }
        }

        public static int GetenhancePawnNumber(int baseNum,int enhancePawnNumber)
        {
            int tempNum =(int) (baseNum / EliteRaidMod.compressionRatio);
            if (EliteRaidMod.useCompressionRatio&&tempNum<enhancePawnNumber)
            {
                if (tempNum < 20) {
                    return 20;
                }
               return tempNum;
            }
            return enhancePawnNumber;
        }

        internal static void GeneratePawns_Impl(PawnGroupMakerParms parms, List<Pawn> pawns)
        {
            if ((pawns?.EnumerableNullOrEmpty() ?? true) || parms?.groupKind == null)
            {
                return;
            }
            foreach (Pawn pawn in pawns) { 
            PawnWeaponChager.CheckAndReplaceMainWeapon(pawn); // 检查并替换主武器
            }
            PawnWeaponChager.ResetCounter();
               // 新增：输出原始参数和 TryGetCompressWork_GeneratePawnsValues 的结果
               Type pawnGroupWorker = null;
            int baseNum = 0;
            int maxPawnNum = 0;
            bool raidFriendly = false;
            bool tryGetSuccess = PatchContinuityHelper.TryGetCompressWork_GeneratePawnsValues(
                parms.GetHashCode(),
                out pawnGroupWorker,
                out baseNum,
                out maxPawnNum,
                out raidFriendly
            );

           // Log.Message($"[EliteRaid] TryGetCompressWork_GeneratePawnsValues 结果: {tryGetSuccess}");
          //  Log.Message($"[EliteRaid] baseNum: {baseNum}, maxPawnNum: {maxPawnNum}, raidFriendly: {raidFriendly}");

           if (pawns.Count<EliteRaidMod.maxRaidEnemy)
            {
                return;
            }

            //平衡生成的动物和人类的数量
            int humanCount = pawns.Where(p => !p.RaceProps.Animal).ToList().Count ;
            int OthersCount = pawns.Count - humanCount;
            if (humanCount<=0||(pawns.Count>0&&OthersCount/humanCount>1))
            {
                int needReplaceCount =(int)Mathf.Ceil( OthersCount / 2);
                List<Pawn> animalPawns = pawns.Where(p => p.RaceProps.Animal).ToList();
                for (int i = 0; i < needReplaceCount; i++) {

                   

                    if (pawns[i] != null )
                    {
                        // 生成保底人类单位
                        Pawn atleastOnePerson = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                             parms.faction.RandomPawnKind(),
                             parms.faction,
                             PawnGenerationContext.NonPlayer,
                             -1,
                             forceGenerateNewPawn: true,
                             mustBeCapableOfViolence: true,
                             biocodeWeaponChance: 0.7f,
                             biocodeApparelChance: 0.7f
                         ));
                        PawnWeaponChager.CheckAndReplaceMainWeapon(atleastOnePerson);
                        pawns.Replace(animalPawns[i],atleastOnePerson);
                         Log.Message($"[EliteRaid] 添加保底人类单位: {atleastOnePerson.LabelCap}");
                    }
                }
                PawnWeaponChager.ResetCounter();
               // Log.Warning("[Elite Raid] 压缩率过高导致没有正常生成人类  Excessive compression rate caused failure to normally generate human units.");
            }
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly);
        }
        internal static void GenerateAnimals_Impl(PawnKindDef animalKind, List<Pawn> pawns)
        {
            if ((pawns?.EnumerableNullOrEmpty() ?? true) || animalKind == null)
            {
                return;
            }
            if (!PatchContinuityHelper.TryGetCompressWork_GenerateAnimalsValues(animalKind.GetHashCode(), out int baseNum, out int maxPawnNum, out bool raidFriendly))
            {
                return;
            }
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly);
        }
        // 新增：判断精英等级是否有效（等级≥1且有实际强化属性）
        private static bool IsEliteLevelValid(EliteLevel eliteLevel)
        {
            if (eliteLevel == null || eliteLevel.Level < 1)
                return false;

            // 检查是否有实际强化（例如承伤系数或移速不等于1）
            return eliteLevel.DamageFactor != 1f ||
                   eliteLevel.MoveSpeedFactor != 1f ||
                   eliteLevel.ScaleFactor != 1f; // 可根据需求添加更多检查
        }
        internal static void GenerateEntitys_Impl(List<Pawn> pawns, int baseNum, int maxPawnNum)
        {
            if (pawns == null || pawns.Count == 0 || baseNum <= 0 || maxPawnNum <= 0)
            {
                return;
            }
            if(EliteRaidMod.displayMessageValue)
            SendLog_Debug(MessageTypes.Debug, "进入增强逻辑，实体数量：{0}，baseNum：{1}，maxPawnNum：{2}",
                 pawns.Count, baseNum, maxPawnNum); // 添加此行
            GenerateAnything_Impl(pawns, baseNum, maxPawnNum, raidFriendly: false);
        }
    }
}
