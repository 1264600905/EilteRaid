﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using System.Security.Cryptography;

namespace EliteRaid
{
    public class BionicsDataStore
    {
        public static List<BionicRecipeDefHolder> m_ListupBionicRecipe = new List<BionicRecipeDefHolder>();
        private static DeathAcidifierHolder m_DeathAcidifierHolder = null;

        public class BionicRecipeDefHolder
        {
            public RecipeDef recipeDef;
            public HediffDef hediffDef;
            public float weight;

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

        public class DeathAcidifierHolder : BionicRecipeDefHolder
        {
            public bool Verified => this.recipeDef != null && this.hediffDef != null && this.PartDef != null && this.RecipeUsers != null;

            public BodyPartDef PartDef { get; set; }

            public List<ThingDef> RecipeUsers { get; set; }

            public DeathAcidifierHolder(RecipeDef recipeDef, HediffDef hediffDef) : base(recipeDef, hediffDef)
            {
                PartDef = this.recipeDef.appliedOnFixedBodyParts.FirstOrDefault();
                RecipeUsers = this.recipeDef.recipeUsers;
            }

            public bool TryCanAddDeathAcidifierHediff(Pawn pawn, out BodyPartRecord part)
            {
                part = null;
                if (m_DeathAcidifierHolder?.hediffDef == null)
                {
                    return false;
                }
                if (pawn.health.hediffSet.HasHediff(m_DeathAcidifierHolder.hediffDef))
                {
                    return false;
                }
                if (!(this.RecipeUsers.Contains(pawn.def)))
                {
                    return false;
                }
                if (pawn.health.hediffSet.hediffs.Any(x => x.Part?.def == this.PartDef && x.def.countsAsAddedPartOrImplant))
                {
                    return false;
                }
                part = pawn.RaceProps.body.AllParts.Where(x => x.def == this.PartDef).FirstOrDefault();
                if (part == null)
                {
                    return false;
                }
                if (TryIsUsedPart(pawn.health.hediffSet, part, out Hediff causeHediff, out BodyPartRecord causePart))
                {
                    return false;
                }
                return true;
            }
        }

        public class BionicRecipeDefPartHolder : BionicRecipeDefHolder
        {
            public BodyPartRecord part;
            public bool allow;
            public int index;

            public BionicRecipeDefPartHolder(RecipeDef recipeDef, HediffDef hediffDef, BodyPartRecord part, bool allow = true) : base(recipeDef, hediffDef)
            {
                this.part = part;
                this.allow = allow;
                this.index = part.Index;
            }
        }

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
                x.modContentPack.Name != "ludeon.rimworld"&&
                x.modContentPack.Name != "ludeon.rimworld.royalty"&&
                x.modContentPack.Name != "ludeon.rimworld.ideology" &&
                x.modContentPack.Name != "ludeon.rimworld.biotech"&&
                 x.modContentPack.Name != "ludeon.rimworld.anomaly";
              //  Log.Message("植入体信息:drug.modContentPack" + x.modContentPack + "名字" + x.defName);
                if (isModContent && !EliteRaidMod.AllowModBionicsAndDrugs)
                {
                    return false; // 不允许模组内容时跳过
                }
                return true;
            }))
            {
                HediffDef hediffDef = def.addsHediff;
                BionicRecipeDefHolder holder = new BionicRecipeDefHolder(def, hediffDef);
                m_ListupBionicRecipe.Add(holder);
            }

            // add@@add@@add@@add@@add
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed("InstallDeathAcidifier", false);
            if (recipe != null)
            {
                HediffDef hediff = recipe.addsHediff;
                m_DeathAcidifierHolder = new DeathAcidifierHolder(recipe, hediff);
            }
            // add@@add@@add@@add@@add
        }

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

        private static bool AddBionics(Pawn pawn)
        {
            return false;
            if (!AllowByTechLevel(pawn))
            {
                return false;
            }

            List<BionicRecipeDefPartHolder> bionicRecipePartDatas = GetBionicRecipePartDatas(pawn).OrderBy(x => x.index).ToList();

            float addBionicsChanceFactorValue = 2.0f;
            float addBionicsChanceNegativeCurveValue = 0.5f;
            float addBionicsChanceMaxValue = 1.0f;
            int addBionicsMaxNumberValue = 3;
            int bionicsNum = 0;
            int loopCount = 0;
            int loopMax = 100;

            while (loopMax > loopCount && addBionicsMaxNumberValue > bionicsNum && bionicRecipePartDatas.Any(x => x.allow))
            {
                float chance =  addBionicsChanceFactorValue;
                if (bionicsNum > 0)
                {
                    for (int i = 0; i < bionicsNum; i++, chance *= addBionicsChanceNegativeCurveValue) ;
                }
                chance = Math.Min(chance, addBionicsChanceMaxValue);

                if (!Rand.Chance(chance))
                {
                    break;
                }


                BionicRecipeDefPartHolder bionicRecipePartData = bionicRecipePartDatas.Where(x => x.allow).RandomElementByWeight(x => x.weight);

                pawn.health.AddHediff(bionicRecipePartData.hediffDef, bionicRecipePartData.part);
                bionicsNum++;
                bionicRecipePartDatas = GetBionicRecipePartDatas(pawn).OrderBy(x => x.index).ToList();

                loopCount++;
            }

            return bionicsNum > 0;
        }

        public static int AddBionics(List<Pawn> pawns, float gainStatValue, int enhancePawnNumber)
        {
            return 0;
            if (pawns == null || pawns.EnumerableNullOrEmpty() || enhancePawnNumber == 0)
            {
                return 0;
            }
            //StringBuilder sb = new StringBuilder();
            //DateTime startTime = DateTime.Now;
            int count = 0;
            for (int i = 0; i < pawns.Count(); i++)
            {
                Pawn pawn = pawns.ElementAt(i);
                if (!EliteRaidMod.AllowCompress(pawn))
                {
                    continue;
                }
                if (AddBionics(pawn))
                {
                    count++;
                }
            }
            //sb.AppendLine(String.Format("所用時間: {0}ms", (DateTime.Now - startTime).Ticks / TimeSpan.TicksPerMillisecond));
            //using (System.IO.StreamWriter sw = new System.IO.StreamWriter("./DEBUG_HEDIFF.txt", false, Encoding.Default))
            //{
            //    sw.WriteLine(sb.ToString());
            //}
            return count;
        }

        //固定添加三个植入体
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

        public static bool AllowByTechLevel(Pawn pawn)
        {
            byte pawnTech = (byte)(pawn?.Faction?.def.techLevel ?? 0x00);
            byte allowTech = (byte)(pawnTech+1);
            return pawnTech >= allowTech;
        }

    }
}
