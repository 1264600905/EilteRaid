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
    /// <summary>
    /// 药物效果数据管理类
    /// 功能：管理游戏中所有可用的药物数据，提供药物的加载、筛选和添加功能
    /// 主要职责：
    /// 1. 加载和存储所有有效的药物配方
    /// 2. 计算药物效果的权重
    /// 3. 为特定单位添加药物效果
    /// 4. 管理药物使用的限制条件
    /// </summary>
    [StaticConstructorOnStartup]
    public class DrugHediffDataStore
    {
        // 权重计算相关常量
        private const float CALCULATE_WEIGHT_MAX = 20000f;
        private const float CALCULATE_WEIGHT_MIN = 1000f;
        private const float PAIN_WEIGHT_SCALE = 5000f;
        private const float NATURAL_HEALING_WEIGHT_SCALE = 2500f;
        private const float PERMENENT_WEIGHT_FIX_OFFSET = -10000f;

        // 类型和方法引用
        private static readonly Type HEDIFF_VOLATILITY_TYPE = typeof(HediffComp_SeverityPerDay);
        private static readonly Type HEDIFF_GIVER_TYPE = typeof(IngestionOutcomeDoer_GiveHediff);
        private static readonly Dictionary<PawnCapacityDef, float> PAWN_CAPACITIE_WEIGHT_SCALE = new Dictionary<PawnCapacityDef, float>();
        private static readonly Dictionary<StatDef, float> STAT_WEIGHT_SCALE = new Dictionary<StatDef, float>();
        private static readonly Type[] METHOD_PARAM = new Type[] { typeof(Pawn) };
        private static readonly MethodInfo METHOD_PRE_POST_INGESTED = AccessTools.Method(typeof(Thing), "PrePostIngested", METHOD_PARAM);
        private static readonly MethodInfo METHOD_POST_INGESTED = AccessTools.Method(typeof(Thing), "PostIngested", METHOD_PARAM);

        public static List<AllowDrugHediffsHolder> m_ListupAllowDrugHediffs;
        private static int m_DrugOverdoseFinalStageIndex;

        /// <summary>
        /// 静态构造函数
        /// 功能：初始化药物相关的能力和属性权重
        /// </summary>
        static DrugHediffDataStore()
        {
            m_ListupAllowDrugHediffs = new List<AllowDrugHediffsHolder>();

            // 战斗相关能力权重
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Moving] = 4000f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Consciousness] = 2500f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Manipulation] = 2500f;
            PAWN_CAPACITIE_WEIGHT_SCALE[PawnCapacityDefOf.Sight] = 2500f;

            // 战斗相关属性权重
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Blunt] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Sharp] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.ArmorRating_Heat] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MeleeDodgeChance] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MeleeHitChance] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.MoveSpeed] = 4000f;
            STAT_WEIGHT_SCALE[StatDefOf.ShootingAccuracyPawn] = 5000f;
            STAT_WEIGHT_SCALE[StatDefOf.PawnTrapSpringChance] = -5000f;
        }

        /// <summary>
        /// 允许使用的药物效果持有者
        /// 功能：存储单个药物的基本信息和权重
        /// 属性说明：
        /// - weight: 药物效果权重
        /// - drug: 药物定义
        /// - hediffDefs: 药物产生的健康状态定义
        /// - permenent: 是否为永久效果
        /// </summary>
        public class AllowDrugHediffsHolder
        {
            public float weight;
            public ThingDef drug;
            public HediffDef[] hediffDefs;
            public bool permenent;

            /// <summary>
            /// 创建药物效果持有者
            /// 输入：
            /// - weight: 效果权重
            /// - drug: 药物定义
            /// - hediffDefs: 健康状态定义数组
            /// 功能：初始化药物数据并计算其永久性
            /// </summary>
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

        /// <summary>
        /// 计算药物效果权重
        /// 输入：
        /// - drug: 药物定义
        /// - hediffDef: 健康状态定义
        /// - weight: 当前权重（引用传递）
        /// 输出：是否成功计算权重
        /// 功能：根据药物效果计算其战斗增强权重
        /// </summary>
        private static bool TryCalculateWeight(ThingDef drug, HediffDef hediffDef, ref float weight)
        {
            float weightBk = weight;
            bool combatEnhancement = false;
            HediffStage effect = hediffDef?.stages?.FirstOrDefault();
            if (effect == null)
            {
                return false;
            }

            // 计算疼痛相关权重
            float work = (1f - effect.painFactor) * PAIN_WEIGHT_SCALE;
            if (work > 0f)
            {
                combatEnhancement = true;
            }
            weight -= work;

            work = effect.painOffset * -PAIN_WEIGHT_SCALE;
            if (work > 0f)
            {
                combatEnhancement = true;
            }
            weight -= work;

            // 计算自然恢复权重
            if (effect.naturalHealingFactor >= 0f)
            {
                weight += (effect.naturalHealingFactor - 1f) * NATURAL_HEALING_WEIGHT_SCALE;
            }

            // 计算能力修改器权重
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
                        }
                        weight -= work;
                    }
                }
            }

            // 计算属性修改器权重
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
                        }
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
                        }
                        weight -= work;
                    }
                }
            }

            if (!combatEnhancement || weight > CALCULATE_WEIGHT_MAX)
            {
                weight = weightBk;
                return false;
            }

            if (weight < CALCULATE_WEIGHT_MIN)
            {
                weight = CALCULATE_WEIGHT_MIN;
            }

            return true;
        }

        /// <summary>
        /// 重置并加载所有可用的药物数据
        /// 功能：从游戏数据库中加载所有符合条件的药物
        /// 处理逻辑：
        /// 1. 初始化过量用药状态索引
        /// 2. 遍历所有药物定义
        /// 3. 计算药物效果权重
        /// 4. 存储符合条件的药物数据
        /// </summary>
        public static void DataRestore()
        {
            m_DrugOverdoseFinalStageIndex = HediffDefOf.DrugOverdose.stages.Count() - 1;
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
        }

        /// <summary>
        /// 获取单位可用的药物列表
        /// 输入：
        /// - pawn: 目标单位
        /// 输出：可用的药物效果持有者列表
        /// 功能：根据单位情况筛选可用的药物
        /// </summary>
        private static List<AllowDrugHediffsHolder> GetDrugHediffs(Pawn pawn)
        {
            List<AllowDrugHediffsHolder> drugs = new List<AllowDrugHediffsHolder>();
            foreach (AllowDrugHediffsHolder drug in m_ListupAllowDrugHediffs.Where(x => {
                DrugCategory? drugCategory = x.drug.ingestible?.drugCategory;
                if (drugCategory != DrugCategory.Medical && drugCategory != DrugCategory.Hard)
                {
                    return false;
                }

                TechLevel pawnTechLevel = pawn.Faction?.def.techLevel ?? TechLevel.Animal;
                TechLevel drugTechLevel = x.drug.techLevel;
                int pawnTechLevelInt = (int)pawnTechLevel;
                int drugTechLevelInt = (int)drugTechLevel;

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

        /// <summary>
        /// 检查是否禁止继续用药
        /// 输入：
        /// - pawn: 目标单位
        /// 输出：true表示禁止用药，false表示允许用药
        /// 功能：检查单位的状态是否允许继续使用药物
        /// </summary>
        private static bool DisallowDoseMore(Pawn pawn)
        {
            if (pawn == null || pawn.health?.hediffSet == null || pawn.Downed || pawn.Dead)
            {
                return true;
            }
            if (pawn.health.hediffSet.hediffs.Any(x => x.def == HediffDefOf.DrugOverdose && x.CurStageIndex == m_DrugOverdoseFinalStageIndex))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 为特定等级的单位添加药物效果
        /// 输入：
        /// - pawn: 目标单位
        /// - eliteLevel: 精英等级
        /// 输出：成功添加的药物数量
        /// 功能：根据精英等级为单位添加指定的药物效果
        /// </summary>
        public static int AddDrugHediffs(Pawn pawn, EliteLevel eliteLevel)
        {
            if (pawn == null || eliteLevel == null || !EliteRaidMod.AllowCompress(pawn) || !eliteLevel.addDrug)
            {
                return 0;
            }

            int addedCount = 0;
            if (!eliteLevel.addDrug) return 0;

            List<AllowDrugHediffsHolder> targetDrugs = new List<AllowDrugHediffsHolder>();

            // 添加活力水
            ThingDef psychiteTea = DefDatabase<ThingDef>.GetNamed("PsychiteTea", false);
            if (psychiteTea != null)
            {
                AllowDrugHediffsHolder teaHolder = m_ListupAllowDrugHediffs
                    .FirstOrDefault(x => x.drug == psychiteTea);
                if (teaHolder != null) targetDrugs.Add(teaHolder);
            }

            // 添加清醒丸
            ThingDef wakeUpPill = DefDatabase<ThingDef>.GetNamed("WakeUpPill", false);
            if (wakeUpPill != null)
            {
                AllowDrugHediffsHolder pillHolder = m_ListupAllowDrugHediffs
                    .FirstOrDefault(x => x.drug == wakeUpPill);
                if (pillHolder != null) targetDrugs.Add(pillHolder);
            }

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

        /// <summary>
        /// 应用药物效果
        /// 输入：
        /// - pawn: 目标单位
        /// - drugHolder: 药物效果持有者
        /// 功能：为单位添加指定的药物效果
        /// </summary>
        private static void ApplyDrugHediff(Pawn pawn, AllowDrugHediffsHolder drugHolder)
        {
            if (DisallowDoseMore(pawn)) return;

            if (drugHolder.permenent)
            {
                foreach (var hediffDef in drugHolder.hediffDefs)
                {
                    pawn.health.AddHediff(hediffDef);
                }
            } else
            {
                Thing drugThing = ThingMaker.MakeThing(drugHolder.drug);
                TryApplyIngestionOutcomes(pawn, drugThing);
                drugThing.Destroy();
            }
        }

        /// <summary>
        /// 尝试应用摄入效果
        /// 输入：
        /// - pawn: 目标单位
        /// - drugThing: 药物物品
        /// 功能：触发药物的摄入效果
        /// </summary>
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
