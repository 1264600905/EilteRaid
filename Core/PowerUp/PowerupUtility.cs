using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EliteRaid
{
    public class PowerupUtility
    {
        private static readonly List<string> usedNames = new List<string>();
        public class PowerupStat
        {
            public StatDef stat { get; set; }
            public float value { get; set; }
            public StatModifier modifierDef { get; set; } // 新增：存储Modifier类型
        }

        // 加算属性集合
        private static IEnumerable<StatModifier> GetAdditiveStatModifiers(EliteLevel eliteLevel)
        {


            // 增加护甲（加算）
            if (eliteLevel.armorEnhance)
            {
                float armorValue = 0.1f + eliteLevel.Level * 0.05f;
                // 检测CE mod是否启用
                bool ceActive = ModLister.GetActiveModWithIdentifier("ceteam.combatextended") != null;

                // 如果CE启用，护甲值乘以10
                if (ceActive)
                {
                    armorValue *= 10f;
                }
                yield return new StatModifier
                {
                    stat = StatDefOf.ArmorRating_Blunt,
                    value = armorValue // 加算，直接叠加
                };
                yield return new StatModifier
                {
                    stat = StatDefOf.ArmorRating_Sharp,
                    value = armorValue // 加算
                };
            }

            // 温度抗性（加算）
            if (eliteLevel.temperatureResist)
            {
                float tempValue = 20 + eliteLevel.Level * 10;
                yield return new StatModifier
                {
                    stat = StatDefOf.ComfyTemperatureMax,
                    value = tempValue * 2.5f // 加算，直接增加温度上限
                };
                yield return new StatModifier
                {
                    stat = StatDefOf.ComfyTemperatureMin,
                    value = -tempValue * 2.5f // 加算，直接降低温度下限
                };
            }

            // 意识强化（PawnCapacityModifier 已处理，此处无需重复）
        }

        // 乘算属性集合
        private static IEnumerable<StatModifier> GetMultiplicativeStatModifiers(EliteLevel eliteLevel, Hediff hediff)
        {



            // 远程攻速（乘算）
            if (eliteLevel.Level>0)
            {
                float cooldownFactor = CalculateCooldownFactor(eliteLevel);
                yield return new StatModifier
                {
                    stat = StatDefOf.RangedCooldownFactor,
                    value = cooldownFactor,

                };
                cooldownFactor = CalculateMeleeCooldownFactor(eliteLevel);
                yield return new StatModifier
                {
                    stat = StatDefOf.MeleeCooldownFactor,
                    value = cooldownFactor,

                };
            }

            ////在拥有VEFcore下更改体型
            //TODO
            //bool hasVEFCore = ModLister.GetActiveModWithIdentifier("oskarpotocki.vanillafactionsexpanded.core") != null;
            //if (eliteLevel.Level >= 5 && hasVEFCore)
            //{
            //    float sizeFactor = eliteLevel.ScaleFactor;
            //    yield return new StatModifier
            //    {
            //        stat = DefDatabase<StatDef>.GetNamedSilentFail("VEF_CosmeticBodySize_Multiplier"), // VEF 体型参数
            //        value = sizeFactor,
            //    };
            //}

            // 通用体型修改（使用BodySizePatch）
            if (eliteLevel.ScaleFactor > 1.0f)
            {
                // 计算体型偏移量：ScaleFactor - 1.0f 得到偏移量
                float sizeOffset = eliteLevel.ScaleFactor - 1.0f;
                // 应用体型修改
                BodySizePatch.SetBodySizeOffset(hediff.pawn, sizeOffset);
                
                if (EliteRaidMod.displayMessageValue)
                {
                   // Log.Message($"[EliteRaid] 应用体型修改: {hediff.pawn.LabelCap} 体型系数={eliteLevel.ScaleFactor:F2}, 偏移量={sizeOffset:F2}");
                }
            }

            // 移速强化（乘算）
            if (eliteLevel.moveSpeedEnhance)
            {
                float speedIncreasePercent = 0.2f + (eliteLevel.Level * 0.05f);
                float totalMoveSpeedFactor = eliteLevel.MoveSpeedFactor * (1f + speedIncreasePercent);
                if (eliteLevel.giantEnhance) totalMoveSpeedFactor *= 0.7f;
                yield return new StatModifier
                {
                    stat = StatDefOf.MoveSpeed,
                    value = totalMoveSpeedFactor // 乘算，作为系数
                };
            } else
            {
                float baseMoveSpeedFactor = eliteLevel.MoveSpeedFactor;
                if (eliteLevel.giantEnhance) baseMoveSpeedFactor *= 0.7f;
                yield return new StatModifier
                {
                    stat = StatDefOf.MoveSpeed,
                    value = baseMoveSpeedFactor // 乘算
                };
            }

            // 承伤系数（乘算）
            float baseDamageFactor = eliteLevel.DamageFactor;
            if (eliteLevel.giantEnhance) baseDamageFactor *= 0.7f;

            // 等级大于3的吞噬兽承伤提高50%
            if (hediff.pawn.kindDef != null && hediff.pawn.kindDef.defName == "Devourer" && eliteLevel.Level >= 3)
            {
                baseDamageFactor *= 1.5f;
                 if(EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 吞噬兽承伤提高50%");
                }
            }
             if(EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 当前被强化物种阵营是"+hediff.pawn.Faction.def.defName+"能否过判断？"+(hediff.pawn.Faction.def.defName == "TribeRoughNeanderthal")+"等级？"+eliteLevel.Level);
                }
            // 尼人族承伤提高30%
            if (hediff.pawn.Faction != null && hediff.pawn.Faction.def.defName == "TribeRoughNeanderthal" && eliteLevel.Level >= 3)
            {
                baseDamageFactor *= 1.3f;
                if(EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 尼人族承伤提高30%");
                }
            }

            baseDamageFactor = Math.Max(baseDamageFactor, 0.08f);
            yield return new StatModifier
            {
                stat = StatDefOf.IncomingDamageFactor,
                value = baseDamageFactor // 乘算，数值越小承伤越低
            };

            // 移速降低抵抗（乘算）
           
            if (eliteLevel.moveSpeedResist)
            {
                float staggerFactor = CalculateStaggerFactor(eliteLevel);
                // 减速持续时间抵抗（原逻辑）
                yield return new StatModifier
                {
                    stat = StatDefOf.StaggerDurationFactor,
                    value = staggerFactor // 例如：0.5 表示减速持续时间变为50%
                };
            }
           
            // 新增：压制抗性（通过降低Suppressability实现）
            bool ceActive = ModLister.GetActiveModWithIdentifier("ceteam.combatextended") != null;
            if (ceActive)
            {
                // CE的压制属性为Suppressability（可压制性），数值越低越难被压制
                StatDef suppressionStat = DefDatabase<StatDef>.GetNamed("Suppressability");
                float suppressability =eliteLevel.moveSpeedResist? -0.9f : -eliteLevel.Level*0.1f;
                if (suppressionStat != null)
                {
                    yield return new StatModifier
                    {
                        stat = suppressionStat,
                        value = suppressability // 负数表示降低可压制性
                    };
                }
            }
            if (hediff.pawn.RaceProps.Humanlike)
            {
                bool cantCompress = hediff.pawn.Faction.Equals(Faction.OfHoraxCult);
                if (eliteLevel.IsBoss && !cantCompress)
                {

                    yield return new StatModifier
                    {
                        stat = StatDefOf.PsychicSensitivity,
                        value = 0f // 心灵敏感度乘0%，完全免疫心灵效果
                    };
                }
            }
         

        }

        // 计算痛觉降低系数（5级开始生效）
        private static void SetPainReduction(EliteLevel eliteLevel,Hediff hediff)
        {
            // 假设gainStatValue为精英等级（5~7）
            if (eliteLevel.Level >= 0)
            {
                float painRatio = 1f;
                if (eliteLevel.Level >= 1) painRatio = 1f;
                if (eliteLevel.Level >= 2) painRatio = 1f;
                if (eliteLevel.Level >= 3) painRatio = 1f;
                if (eliteLevel.Level >= 4) painRatio = 0.9f;
                if (eliteLevel.Level >= 5) painRatio = 0.8f; // 5级
                if (eliteLevel.Level >= 6) painRatio = 0.6f; // 6级
                if (eliteLevel.Level >= 7) painRatio = 0.4f; // 7级及以上
                hediff.CurStage.painFactor = painRatio;
            }
        }

        // 计算冷却时间系数（远程）
        private static float CalculateCooldownFactor(EliteLevel eliteLevel)
        {
            // 基础攻速加成（无攻击强化）
            float baseSpeedBonus = 0f;
            switch (eliteLevel.Level)
            {
                case 1: baseSpeedBonus = 0.2f; break;  // 1级20%攻速加成
                case 2: baseSpeedBonus = 0.3f; break;
                case 3: baseSpeedBonus = 0.4f; break;  // 补全3级（原需求缺失）
                case 4: baseSpeedBonus = 0.5f; break;
                case 5: baseSpeedBonus = 0.8f; break;
                case 6: baseSpeedBonus = 1.0f; break;
                case 7: baseSpeedBonus = 1.5f; break;
                default: baseSpeedBonus = 0f; break;  // 超出等级范围默认0加成
            }

            // 攻击强化词条加成（独立倍率）
            float enhancedSpeedBonus = 0f;
            switch (eliteLevel.Level)
            {
                case 1: enhancedSpeedBonus = 0.5f; break;  // 1级带词条50%攻速加成
                case 2: enhancedSpeedBonus = 0.6f; break;
                case 3: enhancedSpeedBonus = 0.7f; break;  // 补全3级
                case 4: enhancedSpeedBonus = 1.0f; break;
                case 5: enhancedSpeedBonus = 1.3f; break;  // 注意：原需求可能为130%而非1300%
                case 6: enhancedSpeedBonus = 1.5f; break;
                case 7: enhancedSpeedBonus = 2.0f; break;
                default: enhancedSpeedBonus = 0f; break;
            }

            // 最终加成：根据是否有词条选择对应倍率
            float totalSpeedBonus = eliteLevel.rangedSpeedEnhance || eliteLevel.meleeEnhance
                ? enhancedSpeedBonus
                : baseSpeedBonus;

            // 计算冷却系数：1/(1+加成)，最低0.2倍冷却
            float cooldownFactor = 1f / Math.Max(1f + totalSpeedBonus, 1f);
            return Math.Max(cooldownFactor, 0.2f);
        }

        // 近战冷却系数：远程加成×70%，最低0.35倍冷却
        private static float CalculateMeleeCooldownFactor(EliteLevel eliteLevel)
        {
            float remoteSpeedBonus;

            // 处理7级时的特殊情况（冷却系数0.4对应150%加成）
            float remoteCooldown = CalculateCooldownFactor(eliteLevel);
            if (remoteCooldown == 0.2f)
            {
                remoteSpeedBonus = 1.5f;
            } else
            {
                remoteSpeedBonus = (1f / remoteCooldown) - 1f; // 反推远程加成
            }

            float meleeSpeedBonus = remoteSpeedBonus * 0.4f; // 近战加成=远程40%
            float cooldownFactor = 1f / Math.Max(1f + meleeSpeedBonus, 1f);
            return Math.Max(cooldownFactor, 0.35f);
        }

        // 计算移速抵抗系数
        private static float CalculateStaggerFactor(EliteLevel eliteLevel)
        {
            float reduction = 0.3f + (eliteLevel.Level * 0.1f);
            return Math.Max(1f - reduction, 0.01f); // 最低0.01倍减速持续时间
        }


        public static bool TrySetStatModifierToHediff(Hediff hediff, EliteLevel eliteLevel)
        {
            //TODO 区分人类增益和非人类的增益
            if (eliteLevel == null || eliteLevel.Level < 1)
            {
                Log.Warning("[EliteRaid] 拒绝设置等级为0的精英属性");
                return false;
            }
            // 装备强化
            if (eliteLevel.refineGear)
                GearRefiner.RefineGear(hediff.pawn, eliteLevel);

            // 仿生学强化
            if (eliteLevel.addBioncis)
                BionicsDataStore.AddBionics(hediff.pawn, eliteLevel);

            // 药物强化
            if (eliteLevel.addDrug)
                DrugHediffDataStore.AddDrugHediffs(hediff.pawn, eliteLevel);
            // 初始化加算和乘算列表
            hediff.CurStage.statOffsets = new List<StatModifier>(); // 加算
            hediff.CurStage.statFactors = new List<StatModifier>(); // 乘算
            //设置痛觉
            SetPainReduction(eliteLevel, hediff);
            //设置体型

            // 填充加算属性（type=Flat）
            foreach (var sm in GetAdditiveStatModifiers(eliteLevel))
            {
                hediff.CurStage.statOffsets.Add(sm);
            }

            
            // 填充乘算属性（type=Multiply）
            foreach (var sm in GetMultiplicativeStatModifiers(eliteLevel, hediff))
            {
                hediff.CurStage.statFactors.Add(sm);
            }

            // 处理容量加算（意识强化）
            hediff.CurStage.capMods = GetPCMList(eliteLevel).ToList();

            // 其他逻辑保持不变
            CR_Powerup crh = hediff as CR_Powerup;
            if (crh != null) crh.CreateSaveData(eliteLevel);
            return true;
        }

        private static IEnumerable<PawnCapacityModifier> GetPCMList(EliteLevel eliteLevel)
        {
            if (eliteLevel.consciousnessEnhance)
            {
                // 意识强化（百分比）
                float basePercent = 0.2f; // 基础提升20%
                float perLevelPercent = 0.05f; // 每级额外提升5%
                float totalPercent = basePercent + (eliteLevel.Level * perLevelPercent); // 最终百分比

                yield return new PawnCapacityModifier
                {
                    capacity = PawnCapacityDefOf.Consciousness,
                    offset = Math.Min(totalPercent, 1.0f) // 限制最大值为200%（可根据需求调整）
                };
            }

            yield break;

        }



        private static string GetPowerupDefNameStartWith()
        {
            return "CR_Powerup";
        }

        public static Hediff SetPowerupHediff(Pawn pawn, int order, bool removeExisting = true)
        {
            // 移除现有CR_Powerup类型的Hediff
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs.Where(x => x is CR_Powerup).ToList();
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (removeExisting)
                {
                    pawn.health.RemoveHediff(hediff);
                } else
                {
                    return null;
                }
            }

            // 使用基础名称（如 "CR_Powerup1"）
            string defName = "CR_Powerup1";

            // 从DefDatabase中获取已定义的HediffDef
            HediffDef crDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (crDef == null)
            {
                Log.Error($"[EliteRaid] 未找到HediffDef: {defName}");
                return null;
            }

            // 添加Hediff到pawn
            pawn.health.AddHediff(crDef);

            // 返回新添加的CR_Powerup实例
            return pawn.health.hediffSet.hediffs.Where(x => x is CR_Powerup).FirstOrDefault();
        }

        public static int GetNewOrder()
        {
            List<CR_Powerup> crList = new List<CR_Powerup>();

            foreach (Pawn pawn in Current.Game.World.worldPawns.AllPawnsAlive.Where(pawn => pawn.health.hediffSet.hediffs.Any(hediff => hediff is CR_Powerup)))
            {
                if (pawn.Map == null || !(pawn.Map.IsPlayerHome || pawn.Map.mapPawns.AnyColonistSpawned))
                {
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs.Where(x => x is CR_Powerup).ToList();

                    for (int i = 0; i < hediffs.Count; i++)
                    {
                        Hediff hediff = hediffs[i];
                        pawn.health.RemoveHediff(hediff);
                    }
                    continue;
                }
                CR_Powerup cr = pawn.health.hediffSet.hediffs.OfType<CR_Powerup>().FirstOrDefault();
                if (cr != null && cr.Order >= 1 && cr.Order <= 20 && !crList.Any(x => x.Order == cr.Order))
                {
                    crList.Add(cr);
                }
                if (crList.Count >= 20)
                {
                    break;
                }
            }

            foreach (Map map in Current.Game.Maps.Where(map => map.IsPlayerHome || map.mapPawns.AnyColonistSpawned))
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns.Where(pawn => pawn.health.hediffSet.hediffs.Any(hediff => hediff is CR_Powerup)))
                {
                    CR_Powerup cr = pawn.health.hediffSet.hediffs.Where(h => h is CR_Powerup).FirstOrDefault() as CR_Powerup;
                    if (cr != null && !crList.Any(x => x.Order == cr.Order))
                    {
                        crList.Add(cr);
                    }
                    if (crList.Count >= 20)
                    {
                        break;
                    }
                }
                if (crList.Count >= 20)
                {
                    break;
                }
            }

            crList.Sort((a, b) => a.Order - b.Order);
            int order = 1;
            bool isNewOrder = false;
            foreach (CR_Powerup cr in crList)
            {
                if (order < cr.Order)
                {
                    isNewOrder = true;
                    break;
                }
                order++;
            }
            if (!isNewOrder)
            {
                if (crList.Any() && crList.Max(x => x.Order) == 20)
                {
                    order = 1;
                }
            }
            return order;
        }

        public List<BionicHediffDefHolder> ListupBionicHediff()
        {
            List<BionicHediffDefHolder> ret = new List<BionicHediffDefHolder>();
            foreach (HediffDef def in DefDatabase<HediffDef>.AllDefs.Where(x => !x.isBad && x.spawnThingOnRemoved != null))
            {
                BionicHediffDefHolder holder = new BionicHediffDefHolder(def);
                ret.Add(holder);
            }
            return ret;
        }

        public struct BionicHediffDefHolder
        {
            private HediffDef def;

            public BionicHediffDefHolder(HediffDef def)
            {
                this.def = def;
            }
        }

    }


}