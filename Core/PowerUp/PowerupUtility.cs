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
                float armorValue = 0.2f + eliteLevel.Level * 0.05f;
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
        private static IEnumerable<StatModifier> GetMultiplicativeStatModifiers(EliteLevel eliteLevel)
        {



            // 远程攻速（乘算）
            if (eliteLevel.rangedSpeedEnhance || eliteLevel.meleeEnhance)
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
            baseDamageFactor = Math.Max(eliteLevel.DamageFactor, 0.08f);
            yield return new StatModifier
            {
                stat = StatDefOf.IncomingDamageFactor,
                value = baseDamageFactor // 乘算，数值越小承伤越低
            };

            // 移速降低抵抗（乘算）
            if (eliteLevel.moveSpeedResist)
            {
                float staggerFactor = CalculateStaggerFactor(eliteLevel);
                yield return new StatModifier
                {
                    stat = StatDefOf.StaggerDurationFactor,
                    value = staggerFactor,

                };
            }

            // 新增：boss级精英敌人的心灵敏感度处理
            if (eliteLevel.IsBoss)
            {
                yield return new StatModifier
                {
                    stat = StatDefOf.PsychicSensitivity,
                    value = 0f // 心灵敏感度乘0%，完全免疫心灵效果
                };
            }
        }

        // 计算冷却时间系数（近战/远程通用）
        private static float CalculateCooldownFactor(EliteLevel eliteLevel)
        {
            float speedIncreasePercent = 0.3f + (eliteLevel.Level * 0.05f);
            return Math.Max(1f - speedIncreasePercent, 0.2f); // 最低0.1倍冷却时间
        }
        // 计算近战冷却时间系数（增加60%效果限制）
        private static float CalculateMeleeCooldownFactor(EliteLevel eliteLevel)
        {
            float baseSpeedIncrease = 0.3f + (eliteLevel.Level * 0.05f);
            float adjustedSpeedIncrease = baseSpeedIncrease * 0.7f; // 近战效果只生效70%
            return Math.Max(1f - adjustedSpeedIncrease, 0.2f); // 最低0.2倍冷却时间
        }

        // 计算移速抵抗系数
        private static float CalculateStaggerFactor(EliteLevel eliteLevel)
        {
            float reduction = 0.3f + (eliteLevel.Level * 0.1f);
            return Math.Max(1f - reduction, 0.01f); // 最低0.01倍减速持续时间
        }

        public static bool TrySetStatModifierToHediff(Hediff hediff, EliteLevel eliteLevel)
        {
            if (eliteLevel == null || eliteLevel.Level < 1)
            {
                Log.Warning("[EliteRaid] 拒绝设置等级为0的精英属性");
                return false;
            }

            // 初始化加算和乘算列表
            hediff.CurStage.statOffsets = new List<StatModifier>(); // 加算
            hediff.CurStage.statFactors = new List<StatModifier>(); // 乘算

            // 填充加算属性（type=Flat）
            foreach (var sm in GetAdditiveStatModifiers(eliteLevel))
            {
                hediff.CurStage.statOffsets.Add(sm);
            }

            // 填充乘算属性（type=Multiply）
            foreach (var sm in GetMultiplicativeStatModifiers(eliteLevel))
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


            // 生成基础名称（如 "CR_Powerup1"）
            string baseName = $"{GetPowerupDefNameStartWith()}{order}";

            // 检查重名，添加随机后缀（两位数字）
            string uniqueName = baseName;
            if (usedNames.Contains(uniqueName))
            {
                int randomSuffix = new Random().Next(10, 99); // 生成两位随机数
                uniqueName = $"{baseName}_{randomSuffix}";
            }
            usedNames.Add(uniqueName); // 记录已使用的名称

            // 创建 HediffDef（注意：需确保名称存在于游戏 Def 中）
            HediffDef crDef = DefDatabase<HediffDef>.GetNamedSilentFail(uniqueName);
            if (crDef == null)
            {
                Log.Error($"[EliteRaid] 未找到 HediffDef: {uniqueName}");
                return null;
            }
            pawn.health.AddHediff(crDef);
            Hediff ret = pawn.health.hediffSet.hediffs.Where(x => x is CR_Powerup).FirstOrDefault();
            return ret;
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