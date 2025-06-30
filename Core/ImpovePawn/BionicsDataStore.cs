using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using System.Security.Cryptography;

namespace EliteRaid
{
    /// <summary>
    /// 植入体数据管理类
    /// 功能：管理游戏中所有可用的植入体数据，提供植入体的加载、筛选和添加功能
    /// 主要职责：
    /// 1. 加载和存储所有有效的植入体配方
    /// 2. 为特定单位筛选可用的植入体
    /// 3. 处理植入体的添加逻辑
    /// </summary>
    public class BionicsDataStore
    {
        private static DeathAcidifierHolder m_DeathAcidifierHolder = new DeathAcidifierHolder();
        public static List<BionicRecipeDefHolder> m_ListupBionicRecipe = new List<BionicRecipeDefHolder>();

        /// <summary>
        /// 死亡酸液器数据持有者
        /// 功能：管理死亡酸液器植入体的数据和验证
        /// </summary>
        private class DeathAcidifierHolder
        {
            public HediffDef hediffDef;
            public bool Verified => hediffDef != null;

            public DeathAcidifierHolder()
            {
                hediffDef = DefDatabase<HediffDef>.GetNamed("DeathAcidifier");
            }

            /// <summary>
            /// 检查是否可以添加死亡酸液器
            /// 输入：pawn-目标单位
            /// 输出：part-可植入的身体部位
            /// 返回值：是否可以添加死亡酸液器
            /// </summary>
            public bool TryCanAddDeathAcidifierHediff(Pawn pawn, out BodyPartRecord part)
            {
                part = null;
                if (pawn.health.hediffSet.HasHediff(hediffDef))
                {
                    return false;
                }
                part = pawn.RaceProps.body.corePart;
                return true;
            }
        }

        /// <summary>
        /// 植入体配方数据持有者
        /// 功能：存储单个植入体配方的基本信息和权重
        /// 属性说明：
        /// - recipeDef: 植入体的配方定义
        /// - hediffDef: 植入体对应的健康状态定义
        /// - weight: 植入体被选中的权重
        /// </summary>
        public class BionicRecipeDefHolder
        {
            public RecipeDef recipeDef;
            public HediffDef hediffDef;
            public float weight;

            /// <summary>
            /// 创建植入体配方数据持有者
            /// 输入：
            /// - recipeDef: 植入体配方定义
            /// - hediffDef: 植入体健康状态定义
            /// 功能：初始化植入体数据并计算其权重
            /// </summary>
            public BionicRecipeDefHolder(RecipeDef recipeDef, HediffDef hediffDef)
            {
                this.recipeDef = recipeDef;
                this.hediffDef = hediffDef;
                if (hediffDef.addedPartProps == null)
                {
                    this.weight = 1f;
                } else
                {
                    this.weight = Math.Max(hediffDef.addedPartProps.betterThanNatural ? 1f / 1.25f : 1f, 1f / hediffDef.addedPartProps.partEfficiency);
                }
            }
        }

        /// <summary>
        /// 植入体配方与身体部位关联数据
        /// 功能：扩展基本植入体数据，添加身体部位相关信息
        /// 属性说明：
        /// - part: 目标身体部位
        /// - allow: 是否允许在该部位安装
        /// - index: 部位索引
        /// </summary>
        public class BionicRecipeDefPartHolder : BionicRecipeDefHolder
        {
            public BodyPartRecord part;
            public bool allow;
            public int index;

            /// <summary>
            /// 创建植入体配方与身体部位的关联数据
            /// 输入：
            /// - recipeDef: 植入体配方定义
            /// - hediffDef: 植入体健康状态定义
            /// - part: 目标身体部位
            /// - allow: 是否允许安装（默认为true）
            /// </summary>
            public BionicRecipeDefPartHolder(RecipeDef recipeDef, HediffDef hediffDef, BodyPartRecord part, bool allow = true) : base(recipeDef, hediffDef)
            {
                this.part = part;
                this.allow = allow;
                this.index = part.Index;
            }
        }



        /// <summary>
        /// 重置并加载所有可用的植入体数据
        /// 功能：从游戏数据库中加载所有符合条件的植入体配方
        /// 筛选条件：
        /// 1. 必须有固定的身体部位
        /// 2. 植入体属性必须有效
        /// 3. 符合模组内容限制
        /// </summary>
        public static void DataRestore()
        {
            m_ListupBionicRecipe = new List<BionicRecipeDefHolder>();
            foreach (RecipeDef def in DefDatabase<RecipeDef>.AllDefs.Where((x) => {
                HediffDef hediffDef = x.addsHediff;
                if (x.appliedOnFixedBodyParts == null || x.appliedOnFixedBodyParts.Count() != 1)
                {
                    return false;
                }
                if (hediffDef == null || hediffDef.isBad || !hediffDef.countsAsAddedPartOrImplant || hediffDef.addedPartProps == null || !hediffDef.addedPartProps.solid || (hediffDef.addedPartProps.partEfficiency < 1f && !hediffDef.addedPartProps.betterThanNatural))
                {
                    return false;
                }
                //限制模组的药物使用
                bool isModContent = x.modContentPack != null &&
      !string.IsNullOrEmpty(x.modContentPack.Name) &&
      !x.modContentPack.Name.StartsWith("ludeon.");
                //  Log.Message("植入体信息:drug.modContentPack" + x.modContentPack + "名字" + x.defName);
                if (isModContent && !EliteRaidMod.AllowModBionicsAndDrugs)
                {
                    return false; // 不允许模组内容时跳过
                }
                return true;
            }))
            {
                m_ListupBionicRecipe.Add(new BionicRecipeDefHolder(def, def.addsHediff));
            }
        }

        /// <summary>
        /// 获取指定单位可用的植入体配方数据
        /// 输入：
        /// - pawn: 目标单位
        /// 输出：可用的植入体配方与身体部位关联数据集合
        /// 功能：根据单位情况筛选可用的植入体配方，并与可用的身体部位匹配
        /// </summary>
        public static IEnumerable<BionicRecipeDefPartHolder> GetBionicRecipePartDatas(Pawn pawn)
        {
            List<BionicRecipeDefPartHolder> holders = new List<BionicRecipeDefPartHolder>();
            foreach (BionicRecipeDefHolder holder in m_ListupBionicRecipe)
            {
                if (!holder.recipeDef.AllRecipeUsers.Contains(pawn.def))
                {
                    continue;
                }
                BodyPartDef partDef = holder.recipeDef.appliedOnFixedBodyParts[0];
                IEnumerable<BodyPartRecord> parts = pawn.RaceProps.body.AllParts.Where(x => x.def == partDef);
                bool hasPart = parts.Any();
                if (!hasPart)
                {
                    continue;
                }
                foreach (BodyPartRecord part in parts)
                {
                    holders.Add(new BionicRecipeDefPartHolder(holder.recipeDef, holder.hediffDef, part));
                }
            }
            holders = holders.OrderBy(x => x.index).ToList();
            for (int i = 0; i < holders.Count; i++)
            {
                BionicRecipeDefPartHolder holder = holders[i];
                if (!holder.allow)
                {
                    continue;
                }

                BodyPartRecord causePart;
                Hediff causeHediff;
                bool partIsUsed = TryIsUsedPart(pawn.health.hediffSet, holder.part, out causeHediff, out causePart);

                if (partIsUsed)
                {
                    DisallowAllChildren(holder, holders);
                    continue;
                }

                yield return holder;

            }
            yield break;
        }

        /// <summary>
        /// 禁用指定部位及其所有子部位的植入体配方
        /// 输入：
        /// - holder: 要禁用的植入体配方持有者
        /// - holders: 所有植入体配方持有者列表
        /// 功能：将指定部位及其所有子部位标记为不可用
        /// </summary>
        private static void DisallowAllChildren(BionicRecipeDefPartHolder holder, List<BionicRecipeDefPartHolder> holders)
        {
            holder.allow = false;
            List<BionicRecipeDefPartHolder> holderSameParts = holders.Where(x => x.part == holder.part).ToList();
            for (int i = 0; i < holderSameParts.Count; i++)
            {
                holderSameParts[i].allow = false;
            }
            foreach (BodyPartTagDef tag in holder.part.def.tags)
            {
                foreach (BodyPartRecord child in holder.part.GetChildParts(tag))
                {
                    IEnumerable<BionicRecipeDefPartHolder> childHolders = holders.Where(x => x.part == child && x.allow);
                    for (int i = 0; i < childHolders.Count(); i++)
                    {
                        BionicRecipeDefPartHolder childHolder = childHolders.ElementAt(i);
                        childHolder.allow = false;
                    }
                }
            }

        }

        /// <summary>
        /// 检查身体部位是否已被使用或不可用
        /// 输入：
        /// - hediffSet: 健康状态集合
        /// - current: 当前检查的身体部位
        /// 输出：
        /// - causeHediff: 导致部位不可用的健康状态
        /// - causePart: 导致部位不可用的身体部位
        /// 返回值：true表示部位已被使用或不可用，false表示部位可用
        /// </summary>
        private static bool TryIsUsedPart(HediffSet hediffSet, BodyPartRecord current, out Hediff causeHediff, out BodyPartRecord causePart)
        {
            causeHediff = hediffSet.hediffs.Where(h => h.Part == current && !h.def.isBad && h.def.countsAsAddedPartOrImplant).FirstOrDefault();
            bool currentHasHediff = causeHediff != null;
            if (currentHasHediff)
            {
                // 自分に何かBionicsがついていれば使用不可
                causePart = current;
                return true;
            }
            causeHediff = hediffSet.hediffs.Where(h => h.Part == current.parent && !h.def.isBad && h.def.countsAsAddedPartOrImplant).FirstOrDefault();
            bool parentHasHediff = causeHediff != null;
            if (parentHasHediff)
            {
                // 親に何かBionicsがついていれば使用不可
                causePart = current.parent;
                return true;
            }
            causePart = null;
            // 自分が最上位かつBionicsがない場合、使用可能
            bool currentIsRoot = current.parent == null;
            if (currentIsRoot)
            {
                return false;
            }
            // 親のパーツが除去されていれば使用不可 / 親にも自分にもBionicsがなく、親パーツが健在ならそれより上位の統合Bionicsは無いので使用可能。
            bool parentMissing = hediffSet.hediffs.Any(h => h.Part == current.parent && h is Hediff_MissingPart);
            return parentMissing;
        }

        /// <summary>
        /// 为特定等级的单位添加植入体
        /// 输入：
        /// - pawn: 目标单位
        /// - eliteLevel: 精英等级
        /// 输出：成功添加的植入体数量
        /// 功能：根据精英等级为单位添加固定数量的植入体，最多添加4个
        /// 处理逻辑：
        /// 1. 检查现有植入体数量
        /// 2. 计算需要添加的数量
        /// 3. 按权重随机选择并添加植入体
        /// </summary>
        public static int AddBionics(Pawn pawn, EliteLevel eliteLevel)
        {
            if (pawn == null || eliteLevel == null || !EliteRaidMod.AllowCompress(pawn)||!eliteLevel.addBioncis)
            {
                return 0;
            }

            // 检查小人身上已有的植入体数量
            int existingBionicsCount = pawn.health.hediffSet.hediffs
                .Count(h => h.def.countsAsAddedPartOrImplant && !h.def.isBad);

            // 如果已有4个植入体，则不再添加
            if (existingBionicsCount >= 4)
            {
                return 0;
            }

            int addedCount = 0;
            int targetCount = 4 - existingBionicsCount; // 固定添加3个植入体

            // 获取所有可用的植入体配方（按权重筛选）
            List<BionicRecipeDefPartHolder> availableHolders = GetBionicRecipePartDatas(pawn)
                .Where(x => x.allow)
                .OrderBy(x => x.index)
                .ToList();

            // 循环直到达到目标数量或无可用配方
            while (addedCount < targetCount && availableHolders.Any())
            {
                // 按权重随机选择一个可用的植入体
                BionicRecipeDefPartHolder selectedHolder = availableHolders
                    .RandomElementByWeight(x => x.weight);

                if (selectedHolder == null)
                {
                    break;
                }

                // 添加Hediff到指定身体部位
                pawn.health.AddHediff(selectedHolder.hediffDef, selectedHolder.part);
                addedCount++;

                // 移除已使用的配方（避免重复添加同一部位）
                availableHolders.Remove(selectedHolder);

                // 重新获取可用配方（处理部位占用冲突）
                availableHolders = GetBionicRecipePartDatas(pawn)
                    .Where(x => x.allow)
                    .OrderBy(x => x.index)
                    .ToList();
            }

            return addedCount;
        }

        /// <summary>
        /// 尝试添加死亡酸液器
        /// 功能：为指定单位添加死亡酸液器植入体
        /// 输入：pawn-目标单位
        /// 输出：是否成功添加死亡酸液器
        /// </summary>
        public static bool TryAddDeathAcidifier(Pawn pawn)
        {
            if (m_DeathAcidifierHolder == null || !m_DeathAcidifierHolder.Verified)
            {
                return false;
            }
            if (!m_DeathAcidifierHolder.TryCanAddDeathAcidifierHediff(pawn, out BodyPartRecord part))
            {
                return false;
            }
            pawn.health.AddHediff(m_DeathAcidifierHolder.hediffDef, part);
            return true;
        }
    }
}
