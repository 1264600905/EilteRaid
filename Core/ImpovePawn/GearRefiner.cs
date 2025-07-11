﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace EliteRaid
{
    /// <summary>
    /// 装备精炼器
    /// 功能：负责提升装备品质和添加特殊效果
    /// 输入：Pawn及其装备信息
    /// 输出：装备品质提升结果
    /// </summary>
    public class GearRefiner
    {
        //private static StringBuilder sb = new StringBuilder();
        /// <summary>
        /// 批量精炼装备
        /// 功能：对指定数量的Pawn进行装备精炼
        /// 输入：pawns-需要精炼的Pawn列表，gainStatValue-增益值，enhancePawnNumber-需要强化的Pawn数量
        /// 输出：成功精炼的Pawn数量
        /// </summary>
        public static int RefineGear(List<Pawn> pawns, float gainStatValue, int enhancePawnNumber)
        {
            //sb.Clear();
            //sb.AppendLine("====RefineGear処理ここから====");
            int count = 0;
            //for (int i = 0; i < pawns.Count; i++)
            //{
            //    if (!PowerupUtility.EnableEnhancePawn(i, enhancePawnNumber))
            //    {
            //        continue;
            //    }
            //    Pawn pawn = pawns[i];
            //    if (!EliteRaidMod.AllowCompress(pawn))
            //    {
            //        continue;
            //    }
            //    if (RefineGear(pawn, gainStatValue))
            //    {
            //        count++;
            //    }
            //}
            return count;
        }

        /// <summary>
        /// 精炼单个Pawn的装备
        /// 功能：根据精英等级提升Pawn的装备品质
        /// 输入：pawn-目标Pawn，eliteLevel-精英等级配置
        /// 输出：是否成功精炼(0-失败，1-成功)
        /// </summary>
        public static int RefineGear(Pawn pawn, EliteLevel eliteLevel)
        {
            if (pawn == null || eliteLevel == null || !EliteRaidMod.AllowCompress(pawn)||!eliteLevel.refineGear)
            {
                return 0;
            }

            int refinedCount = 0;
            bool hasRefinedApparel = false;

            // 提升所有武器等级2级
            List<ThingWithComps> weapons;
            if (TryGetQualitableWeapons(pawn, out weapons))
            {
                foreach (var weapon in weapons)
                {
                    CompQuality qualityComp = weapon.GetComp<CompQuality>();
                    if (RefineQuality(qualityComp, 2, pawn)) // 固定提升2级
                    {
                        refinedCount++;

                        // 原有生物编码逻辑
                        CompBiocodable biocodeComp = weapon.GetComp<CompBiocodable>();
                        if (biocodeComp != null && !biocodeComp.Biocoded && Rand.Chance(0.8f))
                        {
                            biocodeComp.CodeFor(pawn);
                        }
                    }
                }
            }

            // 随机选择三个护甲提升2级
            List<ThingWithComps> apparels;
            if (TryGetQualitableApparels(pawn, out apparels) && apparels.Count > 0)
            {
                // 随机打乱护甲列表
                var randomApparels = apparels.InRandomOrder().ToList();
                int refineApparelCount = Math.Min(3, randomApparels.Count); // 最多选择3个

                for (int i = 0; i < refineApparelCount; i++)
                {
                    var apparel = randomApparels[i];
                    CompQuality qualityComp = apparel.GetComp<CompQuality>();
                    if (RefineQuality(qualityComp, 2,pawn)) // 固定提升2级
                    {
                        refinedCount++;
                        hasRefinedApparel = true;
                    }
                }

                // 原有死亡酸液植入逻辑
                if (hasRefinedApparel && Rand.Chance(0.8f))
                {
                    BionicsDataStore.TryAddDeathAcidifier(pawn);
                }
            }

            return refinedCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 提升装备品质
        /// 功能：将装备品质提升指定等级
        /// 输入：qualityComp-品质组件，levelsToIncrease-提升等级数，pawn-目标Pawn
        /// 输出：是否成功提升品质
        /// </summary>
        private static bool RefineQuality(CompQuality qualityComp, int levelsToIncrease, Pawn pawn)
        {
            if (qualityComp == null) return false;

            // 确保不超过传奇品质
            byte currentQuality = (byte)qualityComp.Quality;
            byte maxQuality = (byte)QualityCategory.Legendary;
            byte newQuality = (byte)Math.Min(currentQuality + levelsToIncrease, maxQuality);

            if (newQuality > currentQuality)
            {
                qualityComp.SetQuality((QualityCategory)newQuality,
                    pawn.IsColonist ? ArtGenerationContext.Colony : ArtGenerationContext.Outsider);
                return true;
            }
            return false;
        }

        private static bool RefineGear(Pawn pawn)
        {
            //sb.AppendLine("-@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@-");
            //sb.AppendLine(String.Format("--RefineGear:{0}の処理ここから--", pawn.LabelCap));

            HediffDef hediffDeathAcidifier = DefDatabase<HediffDef>.GetNamed("DeathAcidifier");
            //sb.AppendLine(String.Format("-{0}には{1}{2}-", pawn.LabelCap, hediffDeathAcidifier.LabelCap, pawn.health.hediffSet.HasHediff(hediffDeathAcidifier) ? "が埋め込まれています。" : "は埋め込まれていません。"));

            bool refinedFlg = false;

            List<ThingWithComps> weapons;
            if (TryGetQualitableWeapons(pawn, out weapons))
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    ThingWithComps weapon = weapons[i];

                    CompQuality qualityComp = weapon.GetComp<CompQuality>();
                    //sb.AppendLine(String.Format("-リファイン前 : 品質持ち武器:{0}, 品質:{1}({2}), バイオコード={3}-", weapon.LabelCap, qualityComp.Quality, (byte)qualityComp.Quality, (weapon.GetComp<CompBiocodable>()?.Biocoded ?? false) ? "yes" : "no"));

                    bool refined = TryRefineGear(pawn, weapon, qualityComp);
                    refinedFlg |= refined;
                    if (!refined )
                    {
                        continue;
                    }

                    //Set biocode when the weapon quality increase.
                    CompBiocodable biocodeWeaponComp = weapon.GetComp<CompBiocodable>();
                    if (biocodeWeaponComp != null && biocodeWeaponComp.CodedPawn == null)
                    {
                        if (pawn.Name?.ToStringFull != null)
                        {
                            // 新增30%不添加逻辑（80%概率执行绑定，30%跳过）
                            if (Rand.Chance(0.8f)) // 0.8f表示80%概率成功添加，0.2f概率不添加
                            {
                                biocodeWeaponComp.CodeFor(pawn);
                            }
                        }
                    }
                }
            }

            List<ThingWithComps> apparels;
            if (TryGetQualitableApparels(pawn, out apparels))
            {
                bool refinedApparels = false;
                for (int i = 0; i < apparels.Count; i++)
                {
                    ThingWithComps apparel = apparels[i];

                    CompQuality qualityComp = apparel.GetComp<CompQuality>();
                    //sb.AppendLine(String.Format("-リファイン前 : 品質持ち防具:{0}, 品質:{1}({2})-", apparel.LabelCap, qualityComp.Quality, (byte)qualityComp.Quality));
                    refinedApparels |= TryRefineGear(pawn, apparel, qualityComp);
                }
               
                if (refinedApparels)
                {
                    // 新增30%不添加逻辑（80%概率执行绑定，30%跳过）
                    if (Rand.Chance(0.9f)) // 0.8f表示80%概率成功添加，0.2f概率不添加
                    {
                        //Add DeathAcidifier hediff when the apparels quality increase.
                        BionicsDataStore.TryAddDeathAcidifier(pawn);
                    }   
                }
                refinedFlg |= refinedApparels;
            }

            return refinedFlg;
            ////sb.AppendLine("-@@@@@@@ 装備のリファイン中... @@@@@@@-");
            ////sb.AppendLine(String.Format("-{0}には{1}{2}-", pawn.LabelCap, hediffDeathAcidifier.LabelCap, pawn.health.hediffSet.HasHediff(hediffDeathAcidifier) ? "が埋め込まれています。" : "は埋め込まれていません。"));
            //if (TryGetQualitableWeapons(pawn, out weapons))
            //{
            //    for (int i = 0; i < weapons.Count; i++)
            //    {
            //        ThingWithComps weapon = weapons[i];
            //        CompQuality qualityComp = weapon.GetComp<CompQuality>();
            //        //sb.AppendLine(String.Format("-リファイン後 : 品質持ち武器:{0}, 品質:{1}({2}), バイオコード={3}-", weapon.LabelCap, qualityComp.Quality, (byte)qualityComp.Quality, (weapon.GetComp<CompBiocodable>()?.Biocoded ?? false) ? "yes" : "no"));
            //    }
            //}
            //if (TryGetQualitableApparels(pawn, out apparels))
            //{
            //    for (int i = 0; i < apparels.Count; i++)
            //    {
            //        ThingWithComps apparel = apparels[i];
            //        CompQuality qualityComp = apparel.GetComp<CompQuality>();
            //        //sb.AppendLine(String.Format("-リファイン後 : 品質持ち防具:{0}, 品質:{1}({2})-", apparel.LabelCap, qualityComp.Quality, (byte)qualityComp.Quality));
            //    }
            //}
            ////sb.AppendLine(String.Format("--RefineGear:{0}の処理ここまで--", pawn.LabelCap));
            ////sb.AppendLine("-@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@-");
            ////sb.AppendLine();
        }

        /// <summary>
        /// 获取可提升品质的武器列表
        /// 功能：获取Pawn装备的所有具有品质属性的武器
        /// 输入：pawn-目标Pawn
        /// 输出：weapons-可提升品质的武器列表，返回是否成功获取
        /// </summary>
        private static bool TryGetQualitableWeapons(Pawn pawn, out List<ThingWithComps> weapons)
        {
            IEnumerable<ThingWithComps> work = pawn.equipment?.AllEquipmentListForReading?.Where(x => x.GetComp<CompQuality>() != null);
            if (work == null || work.EnumerableNullOrEmpty())
            {
                weapons = null;
                return false;
            }
            weapons = work.ToList();
            return true;
        }

        /// <summary>
        /// 获取可提升品质的护甲列表
        /// 功能：获取Pawn装备的所有具有品质属性的护甲
        /// 输入：pawn-目标Pawn
        /// 输出：apparels-可提升品质的护甲列表，返回是否成功获取
        /// </summary>
        private static bool TryGetQualitableApparels(Pawn pawn, out List<ThingWithComps> apparels)
        {
            IEnumerable<ThingWithComps> work = pawn.apparel?.WornApparel?.Where(x => x.GetComp<CompQuality>() != null);
            if (work == null || work.EnumerableNullOrEmpty())
            {
                apparels = null;
                return false;
            }
            apparels = work.ToList();
            return true;
        }

        private static bool TryRefineGear(Pawn pawn, ThingWithComps equipment, CompQuality qualityComp)
        {
            if (qualityComp.Quality == QualityCategory.Legendary)
            {
                return false;
            }

            if (!TryGetIncreaseQuality(out byte increaseQuality))
            {
                return false;
            }

            QualityCategory refinedQuality = GetRefinedQuality(qualityComp.Quality, increaseQuality);
            qualityComp.SetQuality(refinedQuality, pawn.IsColonist ? ArtGenerationContext.Colony : ArtGenerationContext.Outsider);
            return true;
        }

        /// <summary>
        /// 获取品质提升等级
        /// 功能：随机决定装备品质提升的等级数
        /// 输入：无
        /// 输出：increaseQuality-提升的等级数(1-4)，返回是否成功获取
        /// </summary>
        private static bool TryGetIncreaseQuality(out byte increaseQuality)
        {
            increaseQuality = 0;
            float randValue = Rand.Value;

            if (randValue <= 0.3f)
                increaseQuality = 1;
            else if (randValue <=0.6f&&randValue>0.3)
                increaseQuality = 2;
            else if (randValue <= 0.9f&&randValue>0.6)
                increaseQuality = 3;
            else
                increaseQuality = 4;

            return true; // 总是返回true，因为必定会输出1-4之间的数值
        }

        /// <summary>
        /// 计算精炼后的品质
        /// 功能：根据当前品质和提升等级计算新品质
        /// 输入：currentQuality-当前品质，increaseQuality-提升等级
        /// 输出：精炼后的新品质
        /// </summary>
        private static QualityCategory GetRefinedQuality(QualityCategory currentQuality, byte increaseQuality)
        {
            byte currentQualityByte = (byte)currentQuality;
            byte refinedQualityByte = (byte)Math.Min(currentQualityByte + increaseQuality, (byte)QualityCategory.Legendary);
            QualityCategory refinedQuality = (QualityCategory)Enum.ToObject(typeof(QualityCategory), refinedQualityByte);
            return refinedQuality;
        }
    }
}
