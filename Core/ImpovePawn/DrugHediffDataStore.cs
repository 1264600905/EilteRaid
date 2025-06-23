using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

namespace EliteRaid
{
    [StaticConstructorOnStartup]
    public class DrugHediffDataStore
    {
        private const float CALCULATE_WEIGHT_MAX = 20000f;
        private const float CALCULATE_WEIGHT_MIN = 1000f;
        private const float PAIN_WEIGHT_SCALE = 5000f;
        private const float NATURAL_HEALING_WEIGHT_SCALE = 2500f;
        private const float PERMENENT_WEIGHT_FIX_OFFSET = -10000f;
        private static readonly Type HEDIFF_VOLATILITY_TYPE = typeof(HediffComp_SeverityPerDay);
        private static readonly Type HEDIFF_GIVER_TYPE = typeof(IngestionOutcomeDoer_GiveHediff);
        private static readonly Dictionary<PawnCapacityDef, float> PAWN_CAPACITIE_WEIGHT_SCALE = new Dictionary<PawnCapacityDef, float>();
        private static readonly Dictionary<StatDef, float> STAT_WEIGHT_SCALE = new Dictionary<StatDef, float>();
        private static readonly Type[] METHOD_PARAM = new Type[] { typeof(Pawn) };
        private static readonly MethodInfo METHOD_PRE_POST_INGESTED = AccessTools.Method(typeof(Thing), "PrePostIngested", METHOD_PARAM);
        private static readonly MethodInfo METHOD_POST_INGESTED = AccessTools.Method(typeof(Thing), "PostIngested", METHOD_PARAM);

        public static List<AllowDrugHediffsHolder> m_ListupAllowDrugHediffs;
        private static int m_DrugOverdoseFinalStageIndex;

        static DrugHediffDataStore()
        {
            m_ListupAllowDrugHediffs = new List<AllowDrugHediffsHolder>();

            //combat-related capacities
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Moving] = 4000f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Consciousness] = 2500f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Manipulation] = 2500f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Sight] = 2500f;

            //combat-related stats
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Blunt] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Sharp] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Heat] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MeleeDodgeChance] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MeleeHitChance] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MoveSpeed] = 4000f;
            STAT_WEIGHT_SCALE[StatDefOf.ShootingAccuracyPawn] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.PawnTrapSpringChance] = -5000f;
        }

        public class AllowDrugHediffsHolder
        {
            public float weight;
            public ThingDef drug;
            public HediffDef[] hediffDefs;
            public bool permenent;

            public AllowDrugHediffsHolder(float weight, ThingDef drug, params HediffDef[] hediffDefs)
            {
                this.drug = drug;
                this.hediffDefs = hediffDefs;
                this.weight = weight;
                this.permenent = true;
                foreach (HediffDef hediffDef in hediffDefs)
                {
                    if (!hediffDef.isBad)
                    {
#if DEBUG
                        //test
                        for (int i = 0; i < hediffDef.comps.Count; i++)
                        {
                            HediffCompProperties comp = hediffDef.comps[i];
                            //   Log.Message(String.Format("CR_MESSAGE {0}, tendable={1}, comp[{2}].type={3}", hediffDef.LabelCap, hediffDef.tendable, i, comp.compClass));
                        }
                        //test
#endif
                        this.permenent &= !hediffDef.tendable && !(hediffDef.comps?.Any(x => x.compClass == HEDIFF_VOLATILITY_TYPE) ?? false);
                        if (!this.permenent)
                        {
                            break;
                        }
                    }
                }
                if (this.permenent)
                {
                    this.weight = Math.Max(this.weight + PERMENENT_WEIGHT_FIX_OFFSET, CALCULATE_WEIGHT_MIN);
                }
            }
        }


        private static bool TryCalculateWeight(ThingDef drug, HediffDef hediffDef, ref float weight)
        {
            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine(String.Format("-----{0}------", drug.LabelCap));
            float weightBk = weight;
            bool combatEnhancement = false;
            HediffStage effect = hediffDef?.stages?.FirstOrDefault();
            if (effect == null)
            {
                return false;
            }
            float work;
            //painFactor
            work = (1f - effect.painFactor) * PAIN_WEIGHT_SCALE;
            if (work > 0f)
            {
                combatEnhancement = true;
                //sb.AppendLine(String.Format("+point:{0}={1}", "painFactor",  work));
            }
            //if (sb != null && work < 0f)
            //{
            //    sb.AppendLine(String.Format("-point:{0}={1}", "painFactor", work));
            //}
            weight -= work;

            //painOffset
            work = effect.painOffset * -PAIN_WEIGHT_SCALE;
            if (work > 0f)
            {
                combatEnhancement = true;
                //sb.AppendLine(String.Format("+point:{0}={1}", "painOffset", work));
            }
            //if (sb != null && work < 0f)
            //{
            //    sb.AppendLine(String.Format("-point:{0}={1}", "painOffset", work));
            //}
            weight -= work;
            //natural healing
            if (effect.naturalHealingFactor >= 0f)
            {
                weight += (effect.naturalHealingFactor - 1f) * NATURAL_HEALING_WEIGHT_SCALE;
            }
            if (effect.capMods != null)
            {
                foreach (PawnCapacityModifier cap in effect.capMods)
                {
                    if (PAWN_CAPACITIE_WEIGHT_SCALE.TryGetValue(cap.capacity, out float value))
                    {
                        work = cap.offset * cap.postFactor * value;
                        if (work > 0f)
                        {
                            combatEnhancement = true;
                            //sb.AppendLine(String.Format("+point:{0}={1}", cap.capacity.LabelCap, work));
                        }
                        //if (sb != null && work < 0f)
                        //{
                        //    sb.AppendLine(String.Format("-point:{0}={1}", cap.capacity.LabelCap, work));
                        //}
                        weight -= work;
                    }
                }
            }
            if (effect.statOffsets != null)
            {
                foreach (StatModifier stat in effect.statOffsets)
                {
                    if (STAT_WEIGHT_SCALE.TryGetValue(stat.stat, out float value))
                    {
                        work = stat.value * value;
                        if (work > 0f)
                        {
                            combatEnhancement = true;
                            //sb.AppendLine(String.Format("+point:{0}={1}", stat.stat.LabelCap, work));
                        }
                        //if (sb != null && work < 0f)
                        //{
                        //    sb.AppendLine(String.Format("-point:{0}={1}", stat.stat.LabelCap, work));
                        //}
                        weight -= work;
                    }
                }
            }
            if (effect.statFactors != null)
            {
                foreach (StatModifier stat in effect.statFactors)
                {
                    if (STAT_WEIGHT_SCALE.TryGetValue(stat.stat, out float value))
                    {
                        work = (stat.value - 1f) * value;
                        if (work > 0f)
                        {
                            combatEnhancement = true;
                            //sb.AppendLine(String.Format("+point:{0}={1}", stat.stat.LabelCap, work));
                        }
                        //if (sb != null && work < 0f)
                        //{
                        //    sb.AppendLine(String.Format("-point:{0}={1}", stat.stat.LabelCap, work));
                        //}
                        weight -= work;
                    }
                }
            }

            if (!combatEnhancement || weight > CALCULATE_WEIGHT_MAX)
            {
                weight = weightBk;
                return false;
            }

            //if (drug == ThingDefOf.Luciferium)
            //{
            //    weight = CALCULATE_WEIGHT_MIN;
            //}

            if (weight < CALCULATE_WEIGHT_MIN)
            {
                weight = CALCULATE_WEIGHT_MIN;
            }

            //Log.Message(sb.ToString());

            return true;
        }

        private static int OrderKey(ModContentPack mod)
        {
            if (mod.IsCoreMod)
            {
                return -1000;
            } else if (mod.PackageId == ModContentPack.RoyaltyModPackageId)
            {
                return -900;
            } else
            {
                return mod.loadOrder;
            }
        }

        public static void DataRestore()
        {
            m_DrugOverdoseFinalStageIndex = HediffDefOf.DrugOverdose.stages.Count() - 1;
#if DEBUG
            //    Log.Message(String.Format("CR_MESSAGE {0}の最終ステージのインデックス={1}", HediffDefOf.DrugOverdose.LabelCap, m_DrugOverdoseFinalStageIndex));
#endif

            StringBuilder sb = new StringBuilder();
            if (sb.Length > 0)
            {
                sb.Insert(0, "CR_InfoExcludedDrugs".Translate());
                //   Log.Message(sb.ToString());
            }

            m_ListupAllowDrugHediffs = new List<AllowDrugHediffsHolder>();
            foreach (ThingDef drug in DefDatabase<ThingDef>.AllDefs.Where(x => x.ingestible != null && x.ingestible.drugCategory > DrugCategory.None && x.ingestible.outcomeDoers != null))
            {
                List<HediffDef> drugHediffs = new List<HediffDef>();
                float weight = CALCULATE_WEIGHT_MAX;
                foreach (IngestionOutcomeDoer_GiveHediff doer in drug.ingestible.outcomeDoers.Where(x => x.GetType() == HEDIFF_GIVER_TYPE))
                {
                    if (doer.hediffDef?.isBad ?? true)
                    {
                        continue;
                    }
                    if (!TryCalculateWeight(drug, doer.hediffDef, ref weight))
                    {
                        continue;
                    }
                    bool isModContent = drug.modContentPack != null &&
          !string.IsNullOrEmpty(drug.modContentPack.Name) &&
          !drug.modContentPack.Name.StartsWith("ludeon.");
                    //  Log.Message("药物信息:drug.modContentPack" + drug.modContentPack+ "名字"+drug.defName);
                    if (isModContent && !EliteRaidMod.AllowModBionicsAndDrugs)
                    {
                        continue;
                    }
                    drugHediffs.Add(doer.hediffDef);
                }
                if (drugHediffs.Any())
                {
                    m_ListupAllowDrugHediffs.Add(new AllowDrugHediffsHolder(weight, drug, drugHediffs.ToArray()));
                }
            }

            sb.Clear();
            if (Prefs.DevMode)
            {
                if (sb.Length > 0)
                {
                    sb.Insert(0, "CR_InfoMustNegativeEffectDrugs".Translate());
                    //   Log.Message(sb.ToString());
                }
            }
#if DEBUG
            //test start
            m_ListupAllowDrugHediffs.Sort((x, y) => (int)(x.weight - y.weight));
            //   Log.Message("==Elite Raid.DrugDataStore_DataRestore DebugMessage START==");

            //   Log.Message(String.Format("適用可能なドラッグリスト件数:{0}", m_ListupAllowDrugHediffs.Count));
            foreach (AllowDrugHediffsHolder drug in m_ListupAllowDrugHediffs)
            {
                //   Log.Message(String.Format("drug={0}({1}), permenent={2}, category={3}, weight={4}, hediffs={5}", drug.drug.LabelCap, drug.drug.defName, drug.permenent, drug.drug.ingestible.drugCategory, drug.weight, string.Join(",", from x in drug.hediffDefs select x.LabelCap)));
            }

            //  Log.Message("==Elite Raid.DrugDataStore_DataRestore DebugMessage END  ==");
            //test end
#endif
        }

        private static List<AllowDrugHediffsHolder> GetDrugHediffs(Pawn pawn)
        {
            List<AllowDrugHediffsHolder> drugs = new List<AllowDrugHediffsHolder>();
            foreach (AllowDrugHediffsHolder drug in m_ListupAllowDrugHediffs.Where(x => {
                // 只允许医疗和硬性药物
                DrugCategory? drugCategory = x.drug.ingestible?.drugCategory;
                if (drugCategory != DrugCategory.Medical && drugCategory != DrugCategory.Hard)
                {
                    return false;
                }

                // 科技等级检查：只允许比当前派系高一级或相同
                TechLevel pawnTechLevel = pawn.Faction?.def.techLevel ?? TechLevel.Animal;
                TechLevel drugTechLevel = x.drug.techLevel;

                // 将科技等级转换为整数进行比较
                int pawnTechLevelInt = (int)pawnTechLevel;
                int drugTechLevelInt = (int)drugTechLevel;

                // 药物科技等级不能超过派系科技等级+1
                if (drugTechLevelInt > pawnTechLevelInt + 1)
                {
                    return false;
                }

                return true;
            }))
            {
                bool hasHediff = false;
                foreach (HediffDef hediffDef in drug.hediffDefs)
                {
                    hasHediff = pawn.health.hediffSet.HasHediff(hediffDef);
                    if (hasHediff)
                    {
                        break;
                    }
                }
                if (!hasHediff)
                {
                    drugs.Add(drug);
                }
            }
            return drugs;
        }

        private static bool DisallowDoseMore(Pawn pawn)
        {
            if (pawn == null || pawn.health?.hediffSet == null || pawn.Downed || pawn.Dead)
            {
#if DEBUG
                //   Log.Message(String.Format("CR_MESSAGE {0}はもうダメになってます。これ以上薬物を付与できません。", pawn.LabelShortCap));
#endif
                return true;
            }
            //HediffDefOf.DrugOverdose
            if (pawn.health.hediffSet.hediffs.Any(x => x.def == HediffDefOf.DrugOverdose && x.CurStageIndex == m_DrugOverdoseFinalStageIndex))
            {
#if DEBUG
                //   Log.Message(String.Format("CR_MESSAGE {0}はオーバードーズの最終段階です。これ以上薬物を付与できません。", pawn.LabelShortCap));
#endif
                return true;
            }
            return false;
        }

        private static bool AddDrugHediffs(Pawn pawn)
        {
            return false;
            List<AllowDrugHediffsHolder> drugs = GetDrugHediffs(pawn);
            if (drugs.EnumerableNullOrEmpty())
            {
                return false;
            }
            float addDrugChanceFactorValue = 2f;
            float addDrugChanceNegativeCurveValue = 0.5f;
            float addDrugChanceMaxValue = 0.8f;
            int addDrugMaxNumberValue = (int)2;
            bool giveOnlyActiveIngredientsValue = true;
            int addDrugNum = 0;
            int loopCount = 0;
            int loopMax = Math.Max(100, addDrugMaxNumberValue);
            bool forceEnd = false;

            while (loopMax > loopCount && addDrugMaxNumberValue > addDrugNum && drugs.Any() && !forceEnd)
            {
                float chance = addDrugChanceFactorValue;
                if (addDrugNum > 0)
                {
                    for (int i = 0; i < addDrugNum; i++, chance *= addDrugChanceNegativeCurveValue) ;
                }
                chance = Math.Min(chance, addDrugChanceMaxValue);
                if (!Rand.Chance(chance))
                {
                    break;
                }
                AllowDrugHediffsHolder drug = drugs.RandomElementByWeight(x => x.weight);
                drugs.Remove(drug);
                bool hasHediff = false;
                foreach (HediffDef hediffDef in drug.hediffDefs)
                {
                    hasHediff = pawn.health.hediffSet.HasHediff(hediffDef);
                    if (hasHediff)
                    {
                        break;
                    }
                }
                if (!hasHediff)
                {

                    if (giveOnlyActiveIngredientsValue)
                    {
#if DEBUG
                        //test
                        if (drug.drug == ThingDefOf.Luciferium)
                        {
                            //       Log.Message(String.Format("CR_MESSAGE: {0}がここにきたらおかしい！バグ！", drug.drug.LabelCap));
                        } else
                        {
                            //        Log.Message(String.Format("CR_MESSAGE: {0}は設定に従い有効成分のみ付与！", drug.drug.LabelCap));
                        }
                        //test
#endif
                        foreach (HediffDef hediffDef in drug.hediffDefs)
                        {
                            if (DisallowDoseMore(pawn))
                            {
                                forceEnd = true;
                                break;
                            }
                            pawn.health.AddHediff(hediffDef);
                        }
                    } else
                    {
                        if (DisallowDoseMore(pawn))
                        {
                            forceEnd = true;
                        }
                        if (!forceEnd)
                        {
                            Thing drugThing = ThingMaker.MakeThing(drug.drug);
                            METHOD_PRE_POST_INGESTED.Invoke(drugThing, new object[] { pawn });
                            if (drugThing.def.ingestible.outcomeDoers != null)
                            {
                                for (int j = 0; j < drugThing.def.ingestible.outcomeDoers.Count; j++)
                                {
                                    if (DisallowDoseMore(pawn))
                                    {
                                        forceEnd = true;
                                        break;
                                    }
                                    drugThing.def.ingestible.outcomeDoers[j].DoIngestionOutcome(pawn, drugThing, 0);
                                }
                            }
                            drugThing.Destroy();
                            METHOD_POST_INGESTED.Invoke(drugThing, new object[] { pawn });
                        }
                    }
                    addDrugNum++;
                }
                loopCount++;
            }
            return addDrugNum > 0;
        }

        public static int AddDrugHediffs(List<Pawn> pawns, float gainStatValue, int enhancePawnNumber)
        {
            return 0;
            if (pawns == null || pawns.EnumerableNullOrEmpty() || enhancePawnNumber == 0)
            {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < pawns.Count(); i++)
            {
                Pawn pawn = pawns.ElementAt(i);
                if (!EliteRaidMod.AllowCompress(pawn))
                {
                    continue;
                }
                if (!pawn.RaceProps.IsFlesh)
                {
                    continue;
                }
                if (AddDrugHediffs(pawn))
                {
                    count++;
                }
            }
            return count;
        }

        public static int AddDrugHediffs(Pawn pawn, EliteLevel eliteLevel)
        {
            if (pawn == null || eliteLevel == null || !EliteRaidMod.AllowCompress(pawn) || !eliteLevel.addDrug)
            {
                return 0;
            }

            int addedCount = 0;
            if (!eliteLevel.addDrug) return 0; // 确保开启药物强化

            // 定义目标药物（使用原代码逻辑获取允许的药物）
            List<AllowDrugHediffsHolder> targetDrugs = new List<AllowDrugHediffsHolder>();

            // 活力水（Psychite Tea）
            ThingDef psychiteTea = DefDatabase<ThingDef>.GetNamed("PsychiteTea", false);
            if (psychiteTea != null)
            {
                // 复用原代码的药物筛选逻辑
                AllowDrugHediffsHolder teaHolder = m_ListupAllowDrugHediffs
                    .FirstOrDefault(x => x.drug == psychiteTea);
                if (teaHolder != null) targetDrugs.Add(teaHolder);
            }

            // 清醒丸（Wake-Up Pill）
            ThingDef wakeUpPill = DefDatabase<ThingDef>.GetNamed("WakeUpPill", false);
            if (wakeUpPill != null)
            {
                AllowDrugHediffsHolder pillHolder = m_ListupAllowDrugHediffs
                    .FirstOrDefault(x => x.drug == wakeUpPill);
                if (pillHolder != null) targetDrugs.Add(pillHolder);
            }

            // 确保目标药物存在且未被应用
            foreach (var drugHolder in targetDrugs)
            {
                bool hasHediff = drugHolder.hediffDefs.Any(hediff =>
                    pawn.health.hediffSet.HasHediff(hediff));

                if (!hasHediff)
                {
                    ApplyDrugHediff(pawn, drugHolder);
                    addedCount++;
                }
            }

            return addedCount;
        }

        // 复用原代码的药物应用逻辑
        private static void ApplyDrugHediff(Pawn pawn, AllowDrugHediffsHolder drugHolder)
        {
            if (DisallowDoseMore(pawn)) return; // 复用原代码的剂量限制

            // 优先使用原代码的「仅添加有效成分」逻辑
            if (drugHolder.permenent)
            {
                foreach (var hediffDef in drugHolder.hediffDefs)
                {
                    pawn.health.AddHediff(hediffDef);
                }
            } else
            {
                // 否则使用原代码的完整摄入逻辑
                Thing drugThing = ThingMaker.MakeThing(drugHolder.drug);
                TryApplyIngestionOutcomes(pawn, drugThing);
                drugThing.Destroy();
            }
        }

        // 复用原代码的摄入效果触发逻辑
        private static void TryApplyIngestionOutcomes(Pawn pawn, Thing drugThing)
        {
            if (drugThing.def.ingestible.outcomeDoers != null)
            {
                foreach (var doer in drugThing.def.ingestible.outcomeDoers)
                {
                    doer.DoIngestionOutcome(pawn, drugThing, 0);
                }
            }
        }

    }
}
