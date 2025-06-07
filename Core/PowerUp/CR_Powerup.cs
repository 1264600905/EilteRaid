using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using RimWorld;
using Verse;

namespace EliteRaid
{

    [DefOf]
    public static class CR_PowerupDefOf
    {
        public static HediffDef CR_Powerup1;
        public static HediffDef CR_Powerup2;
        public static HediffDef CR_Powerup3;
        public static HediffDef CR_Powerup4;
        public static HediffDef CR_Powerup5;
        public static HediffDef CR_Powerup6;
        public static HediffDef CR_Powerup7;
        public static HediffDef CR_Powerup8;
        public static HediffDef CR_Powerup9;
        public static HediffDef CR_Powerup10;
        public static HediffDef CR_Powerup11;
        public static HediffDef CR_Powerup12;
        public static HediffDef CR_Powerup13;
        public static HediffDef CR_Powerup14;
        public static HediffDef CR_Powerup15;
        public static HediffDef CR_Powerup16;
        public static HediffDef CR_Powerup17;
        public static HediffDef CR_Powerup18;
        public static HediffDef CR_Powerup19;
        public static HediffDef CR_Powerup20;
    }

    [StaticConstructorOnStartup]
    public abstract class CR_Powerup : HediffWithComps
    {
          private int spawnTimes = 0; // 标记是否为首次生成
        private int _delayTicks = 180; // 3秒延迟（60 tick/秒）
        private int _currentTickCount = 0;
        public override void PostMake()
        {
            base.PostMake();
            m_RaidFriendly = pawn.Faction != null && !FactionUtility.HostileTo(Faction.OfPlayer, pawn.Faction);
        }
        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled(); // 调用基类方法（可选）
            if (EliteRaidMod.displayMessageValue)
                Log.Message($"[EliteRaid] {pawn.LabelCap} 已死亡，移除精英状态");
            RemoveThis(); // 执行移除逻辑
        }

        private void RemoveThis()
        {
            if (EliteRaidMod.displayMessageValue)
                Log.Message($"[CompressedRaid] Removing powerup from pawn {pawn.LabelCap} (ID: {pawn.thingIDNumber})");
            this.pawn.health.RemoveHediff(this);
            // 新增：生成白银
            if (EliteRaidMod.enableSilverDrop)
                GenerateSilverOnPawn();
        }

        private bool IsPanicFree()
        {
            bool? work = this.pawn?.mindState?.mentalStateHandler?.CurState?.def == MentalStateDefOf.PanicFlee;
            return work == null ? false : (bool)work;
        }

        private bool IsKidnap()
        {
            bool? work = this.pawn?.CurJob?.def == JobDefOf.Kidnap;
            return work == null ? false : (bool)work;
        }

        private bool IsSteal()
        {
            bool? work = this.pawn?.CurJob?.def == JobDefOf.Steal;
            return work == null ? false : (bool)work;
        }
        public override string Label
        {
            get
            {
                var eliteData = GetEliteLevelData();

                // 新增：明确判断等级是否有效（Level >= 1）
                if (eliteData == null || eliteData.Level < 1)
                {
                    return ""; // 等级无效时返回空字符串，彻底隐藏旧标签
                               // 或：return base.Label; // 若必须保留基础标签，取消注释此行（不推荐）
                }

                return "EliteLevelLabel".Translate(eliteData.Level);
            }
        }
       
        public override void Tick()
        {
            base.Tick();
            Pawn pawn = this.pawn;
            if (pawn == null)
            {
                Log.Message("Pawn是空的!");
                return;
            }

            int tick = Find.TickManager.TicksGame;

            // 恢复原版的 RestoreData() 触发逻辑
            if (Math.Abs(tick - m_GameTick) >= 10)
            {
                RestoreData();
            }
            m_GameTick = tick;

            //// **新增：延迟检查武器逻辑**
            //if (_currentTickCount == _delayTicks) // 当计数达到延迟值时触发检查
            //{
            //    CheckAndReplaceMainWeapon(pawn); // 检查并替换主武器
            //}

            //尝试在小人生成后180Tick后执行方法刷新面板
            if (spawnTimes<=2||_currentTickCount > _delayTicks)
            {
              
                UpdatePawnInfo(this.pawn);
                _currentTickCount = 0;
                spawnTimes++;
            } else
            {
                _currentTickCount++;
            }


            // 独立检查每个条件分支
            bool condition1 = pawn.health.Dead;
            bool condition2 = pawn.Downed;   //生效了
            bool condition3 = m_FirstMapSetting && pawn.Map == null;
            bool condition4 = pawn.Faction != null && pawn.Faction.IsPlayer;  //生效
                                                                              // bool condition5 = IsPanicFree();    //生效了
            bool condition6 = IsKidnap();
            bool condition7 = IsSteal();
            bool condition8 = !m_RaidFriendly &&
                            pawn.Faction != null &&
                            !FactionUtility.HostileTo(Faction.OfPlayer, pawn.Faction);

            // 仅在触发移除时输出详细日志
            if (condition1 || condition2 || condition3 || condition4 || condition6 || condition7 || condition8)
            {
                //Log.Message($"[EliteRaid] {pawn.LabelCap} 触发移除! 条件: " +
                //    $"Dead={condition1}, " +
                //    $"Downed={condition2}, " +
                //    $"FirstMap&&MapNull={condition3}, " +
                //    $"IsPlayerFaction={condition4}, " +
                //  //  $"IsPanicFree={condition5}, " +
                //    $"IsKidnap={condition6}, " +
                //    $"IsSteal={condition7}, " +
                //    $"非敌对非友方={condition8}");


                RemoveThis();
                return;
            }


            //// 调整条件判断结构（根据需求决定是否保留原版逻辑）
            //if (pawn.Dead || pawn.Downed || (m_FirstMapSetting && pawn.Map == null) || (pawn.Faction != null && pawn.Faction.IsPlayer) || IsPanicFree() || IsKidnap() || IsSteal() || (!m_RaidFriendly && pawn.Faction != null && !FactionUtility.HostileTo(Faction.OfPlayer, pawn.Faction)))
            //{
            //    Log.Message("RemoveThis触发!");
            //    RemoveThis();
            //    return;
            //}

            // 仅在 Hediff 首次创建时初始化数据，无需高频恢复
            if (m_FirstMapSetting && pawn.Map != null)
            {
                m_FirstMapSetting = false;
                RestoreData(); // 首次加载时恢复一次
            }
        }


        private void UpdatePawnInfo(Pawn pawn)
        {
            //强制更新属性
            pawn.Notify_Released();
            CheckAndRemoveDuplicatePowerups();
        }

        private void CheckAndRemoveDuplicatePowerups()
        {
            if (pawn == null || !pawn.Spawned || pawn.health == null)
                return;

            // 获取所有CR_Powerup类型的Hediff
            var powerupHediffs = pawn.health.hediffSet.hediffs
                .Where(h => h is CR_Powerup && h != this) // 排除自身
                .ToList();

            if (powerupHediffs.Count > 0)
            {
                // 随机选择一个CR_Powerup类型的Hediff移除
                var hediffToRemove = powerupHediffs.RandomElement();
                if (EliteRaidMod.displayMessageValue)
                    Log.Message($"[EliteRaid] 发现重复的CR_Powerup，移除: {hediffToRemove.LabelCap}");

                pawn.health.RemoveHediff(hediffToRemove);
            }
        }

        public void CreateSaveData(EliteLevel eliteLevel)
        {
            // 构建数据对象
            var data = new EliteLevelData
            {
                Level = eliteLevel.Level,
                DamageFactor = eliteLevel.DamageFactor,
                MoveSpeedFactor = eliteLevel.MoveSpeedFactor,
                ScaleFactor = eliteLevel.ScaleFactor,
                MaxTraits = eliteLevel.MaxTraits,
                IsBoss = eliteLevel.IsBoss,

                // 保存所有词条开关
                addBioncis = eliteLevel.addBioncis,
                addDrug = eliteLevel.addDrug,
                refineGear = eliteLevel.refineGear,
                armorEnhance = eliteLevel.armorEnhance,
                temperatureResist = eliteLevel.temperatureResist,
                meleeEnhance = eliteLevel.meleeEnhance,
                rangedSpeedEnhance = eliteLevel.rangedSpeedEnhance,
                moveSpeedEnhance = eliteLevel.moveSpeedEnhance,
                giantEnhance = eliteLevel.giantEnhance,
                bodyPartEnhance = eliteLevel.bodyPartEnhance,
                painEnhance = eliteLevel.painEnhance,
                moveSpeedResist = eliteLevel.moveSpeedResist,
                consciousnessEnhance = eliteLevel.consciousnessEnhance,

                // 分别保存加算和乘算属性
                StatOffsets = this.CurStage.statOffsets.Select(sm => new EliteLevelData.StatModifierData
                {
                    StatDefName = sm.stat.defName,
                    Value = sm.value
                }).ToList(),

                StatFactors = this.CurStage.statFactors.Select(sm => new EliteLevelData.StatModifierData
                {
                    StatDefName = sm.stat.defName,
                    Value = sm.value
                }).ToList(),

                // 转换 PawnCapacityModifier 为可序列化的嵌套类
                CapMods = this.CurStage.capMods.Select(pcm => new EliteLevelData.PawnCapacityModifierData
                {
                    CapacityDefName = pcm.capacity.defName,
                    Offset = pcm.offset
                }).ToList()
            };

            // 使用 Json.NET 序列化数据（紧凑格式，节省存储空间）
            string jsonData = JsonHelper.Serialize(data); // 调用自定义序列化方法
            if (!string.IsNullOrEmpty(jsonData))
            {
                this.m_SaveDataField = $"pawnId:{pawn.thingIDNumber}|data:{jsonData}";
            } else
            {
                Log.Error($"[EliteRaid] 序列化失败，EliteLevel: {eliteLevel.Level}");
            }

            // 组合 pawnID 和数据（格式：pawnId:XXX|data:JSON字符串）
            this.m_SaveDataField = $"pawnId:{pawn.thingIDNumber}|data:{jsonData}";
            //if (EliteRaidMod.displayMessageValue)
            //{
            //    Log.Message($"[EliteRaid] 保存的 JSON: {jsonData}");
            //}
        }
        private void RestoreData()
        {
            if (string.IsNullOrEmpty(m_SaveDataField))
            {
                Log.Warning($"[EliteRaid] 尝试恢复空的精英数据，移除Hediff, Pawn: {pawn?.Name}");
                RemoveThis();
                return;
            }

            bool shouldLog = false; // 日志输出标记
            EliteLevelData data = null; // 存储反序列化后的数据

            try
            {
                // 拆分 pawnID 和 JSON 数据
                string[] parts = m_SaveDataField.Split(new[] { "|data:" }, 2, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    Log.Error($"[EliteRaid] 保存数据格式错误: {m_SaveDataField}");
                    RemoveThis();
                    return;
                }

                string pawnId = parts[0].Replace("pawnId:", "");
                string jsonData = parts[1];

                // 验证 pawnID 是否匹配当前 pawn
                if (pawnId != pawn.thingIDNumber.ToString())
                {
                    Log.Error($"[EliteRaid] pawnID 不匹配：保存的={pawnId}，当前={pawn.thingIDNumber}");
                    RemoveThis();
                    return;
                }

                // 反序列化为 EliteLevelData 对象
                data = JsonHelper.Deserialize<EliteLevelData>(jsonData);
                if (data == null)
                {
                    Log.Error("[EliteRaid] 反序列化 EliteLevelData 失败，数据为空");
                    RemoveThis();
                    return;
                }

                shouldLog = true; // 数据有效时标记日志输出

                // 修复：使用当前Hediff的CurStage，不再创建新的Stage对象
                // 清空原有属性，重新添加
                this.CurStage.statOffsets.Clear();
                this.CurStage.statFactors.Clear();
                this.CurStage.capMods.Clear();

                // 定义乘算属性集合（根据游戏机制动态判断）
                var multiplicativeStats = new HashSet<string>
        {
            StatDefOf.MoveSpeed.defName,
            StatDefOf.IncomingDamageFactor.defName,
            StatDefOf.MeleeCooldownFactor.defName,
            StatDefOf.RangedCooldownFactor.defName,
            StatDefOf.StaggerDurationFactor.defName,
            StatDefOf.PsychicSensitivity.defName,
            // 可根据需要扩展更多乘算属性
        };

                // 恢复加算属性（StatOffsets）
                foreach (var smd in data.StatOffsets)
                {
                    try
                    {
                        var statDef = DefDatabase<StatDef>.GetNamed(smd.StatDefName);
                        this.CurStage.statOffsets.Add(new StatModifier
                        {
                            stat = statDef,
                            value = smd.Value
                        });
                    } catch (Exception e)
                    {
                        Log.Error($"[EliteRaid] 恢复加算属性失败: {smd.StatDefName}, 错误: {e.Message}");
                        shouldLog = false; // 单个属性失败则不输出成功日志
                    }
                }

                // 恢复乘算属性（StatFactors）
                foreach (var smd in data.StatFactors)
                {
                    try
                    {
                        var statDef = DefDatabase<StatDef>.GetNamed(smd.StatDefName);
                        // 二次校验是否属于乘算属性（避免逻辑错误）
                        if (multiplicativeStats.Contains(statDef.defName))
                        {
                            this.CurStage.statFactors.Add(new StatModifier
                            {
                                stat = statDef,
                                value = smd.Value
                            });
                        } else
                        {
                            Log.Warning($"[EliteRaid] 发现非乘算属性被标记为乘算: {statDef.defName}，将其转为加算");
                            this.CurStage.statOffsets.Add(new StatModifier
                            {
                                stat = statDef,
                                value = smd.Value
                            });
                        }
                    } catch (Exception e)
                    {
                        Log.Error($"[EliteRaid] 恢复乘算属性失败: {smd.StatDefName}, 错误: {e.Message}");
                        shouldLog = false;
                    }
                }

                // 恢复容量修改器（CapMods）
                foreach (var pcmd in data.CapMods)
                {
                    try
                    {
                        var capacityDef = DefDatabase<PawnCapacityDef>.GetNamed(pcmd.CapacityDefName);
                        this.CurStage.capMods.Add(new PawnCapacityModifier
                        {
                            capacity = capacityDef,
                            offset = pcmd.Offset
                        });
                    } catch (Exception e)
                    {
                        Log.Error($"[EliteRaid] 恢复容量修改器失败: {pcmd.CapacityDefName}, 错误: {e.Message}");
                        shouldLog = false;
                    }
                }

                // 应用额外效果（如外观变化）
                ApplyLevelEffects(data);

                // 输出成功日志（仅在所有属性恢复无异常时）
                //if (EliteRaidMod.displayMessageValue && shouldLog)
                //{
                //    Log.Message($"[EliteRaid] 成功恢复精英数据：" +
                //                $"pawn={pawn.Name}, " +
                //                $"等级={data.Level}, " +
                //                $"加算属性={this.CurStage.statOffsets.Count}, " +
                //                $"乘算属性={this.CurStage.statFactors.Count}, " +
                //                $"容量修改器={this.CurStage.capMods.Count}");
                //}
            } catch (Exception e)
            {
                Log.Error($"[EliteRaid] 恢复数据失败：{e.Message}\n{e.StackTrace}");
                RemoveThis();

            }

        }

        private void GenerateSilverOnPawn()
        {
            // 检查 Pawn 是否在地图上且位置有效
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            // 创建白银物品（使用 ThingDefOf.Silver）
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = GetEliteLevelData().Level * EliteRaidMod.silverDropPerLevel; // 设置数量为50

            // 在 Pawn 所在位置生成白银
            GenPlace.TryPlaceThing(silver, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }

        // 根据恢复的精英等级数据应用额外效果（可选实现）
        private void ApplyLevelEffects(EliteLevelData data)
        {
            try
            {
                ////修改体型?
                //if(data != null && data.ScaleFactor > 1)
                // {
                //     BodySizePatch.SetBodySizeOffset(pawn, (data.ScaleFactor*0.5f));

                // }


                // 可添加其他根据精英等级数据应用的效果
            } catch (Exception e)
            {
                Log.Error($"[EliteRaid] 应用精英等级效果失败：{e.Message}");
            }
        }

        private EliteLevelData GetEliteLevelData()
        {
            if (string.IsNullOrEmpty(m_SaveDataField))
            {
                return null;
            }

            try
            {
                // 拆分 pawnID 和 JSON 数据
                string[] parts = m_SaveDataField.Split(new[] { "|data:" }, 2, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    Log.Error($"[EliteRaid] 保存数据格式错误: {m_SaveDataField}");
                    return null;
                }

                string jsonData = parts[1];
                EliteLevelData data = JsonHelper.Deserialize<EliteLevelData>(jsonData);
                // -------------------- 新增：打印完整 EliteLevelData 内容 --------------------
                //if (data != null&&EliteRaidMod.displayMessageValue)
                //{
                //    Log.Message($"[EliteRaid] 解析到 EliteLevelData：");
                //    Log.Message($"  等级: {data.Level}");
                //    Log.Message($"  承伤系数: {data.DamageFactor:P0}");
                //    Log.Message($"  移速系数: {data.MoveSpeedFactor:P0}");
                //    Log.Message($"  体型系数: {data.ScaleFactor:F1}");
                //    Log.Message($"  最大词条数: {data.MaxTraits}");
                //    Log.Message($"  是否为Boss: {data.IsBoss}");
                //    Log.Message($"  词条激活状态:");
                //    Log.Message($"    器官强化: {data.addBioncis}");
                //    Log.Message($"    药物强化: {data.addDrug}");
                //    Log.Message($"    装备强化: {data.refineGear}");
                //    Log.Message($"    护甲强化: {data.armorEnhance}");
                //    Log.Message($"    温度抗性: {data.temperatureResist}");
                //    Log.Message($"    近战强化: {data.meleeEnhance}");
                //    Log.Message($"    远程强化: {data.rangedSpeedEnhance}");
                //    Log.Message($"    提速强化: {data.moveSpeedEnhance}");
                //    Log.Message($"    巨人化: {data.giantEnhance}");
                //    Log.Message($"    身体强化: {data.bodyPartEnhance}");
                //    Log.Message($"    无痛强化: {data.painEnhance}");
                //    Log.Message($"    移速抵抗: {data.moveSpeedResist}");
                //    Log.Message($"    意识强化: {data.consciousnessEnhance}");
                //    Log.Message($"  激活的词条名称: {string.Join(", ", data.GetActiveTraitNames())}");
                //}
                // -------------------- 日志打印结束 --------------------

                return data; // 使用自定义反序列化
            } catch (Exception e)
            {
                Log.Error($"[EliteRaid] 解析精英数据失败: {e.Message}");
                return null;
            }
        }

        public override string TipStringExtra
        {
            get
            {
                var sb = new StringBuilder();
                var eliteData = GetEliteLevelData();

                if (eliteData == null || eliteData.Level < 1)
                {
                    return ""; // 移除默认显示
                }

                // 等级颜色处理（保持原有逻辑）
                string colorCode;
                if (eliteData.Level == 1) colorCode = "00FF00";   // 绿色
                else if (eliteData.Level == 2) colorCode = "00BFFF"; // 蓝色
                else if (eliteData.Level == 3) colorCode = "0000CD"; // 深蓝色
                else if (eliteData.Level == 4) colorCode = "EE82EE"; // 浅紫色
                else if (eliteData.Level == 5) colorCode = "9400D3"; // 深紫色
                else if (eliteData.Level == 6) colorCode = "FF0000"; // 红色
                else if (eliteData.Level == 7) colorCode = "FFFF00"; // 黄色
                else colorCode = "FFFFFF"; // 默认白色

                // 主标题：精英等级 + 颜色标签（显示在健康面板标题栏）
                sb.AppendLine(string.Format("<color=#{0}>{1}</color>", colorCode, "EliteLevelLabel".Translate(eliteData.Level)));

                // 修改后
                sb.AppendLine(
                    "EliteStats_Damage".Translate() + eliteData.DamageFactor.ToString("P0") +
                    "EliteStats_Separator".Translate() +
                    "EliteStats_MoveSpeed".Translate() + eliteData.MoveSpeedFactor.ToString("P0") +
                    "EliteStats_Separator".Translate() +
                    "EliteStats_Scale".Translate() + eliteData.ScaleFactor.ToString("F1")
                );

                // 激活的词条列表（合并为一句话）
                var activeTraits = eliteData.GetActiveTraitNames()
                .Take(6) // 限制最多6个
                .Select(t => eliteData.GetTraitDisplayName(t))
                .ToList();

                // 修改后的代码
                if (activeTraits.Any())
                {
                    sb.Append("\n<color=#FFD700>" + "TraitEnhancement".Translate() + "</color>");

                    // 逐个添加词条，保留颜色标签
                    for (int i = 0; i < activeTraits.Count; i++)
                    {
                        sb.Append(activeTraits[i]);
                        if (i < activeTraits.Count - 1)
                        {
                            sb.Append(" | ");
                        }
                    }
                }

                // 添加身体部件强化显示
                if (eliteData.IsBoss)
                {
                    sb.AppendLine($"\n<color=#FFD700>Boss强化:</color>");
                }

                // 移除原始额外提示（如果不需要基础内容）
                // var baseTip = base.TipStringExtra;
                // if (!string.IsNullOrEmpty(baseTip)) sb.AppendLine($"\n{baseTip}");
                if (eliteData == null)
                {
                    Log.Warning($"[EliteRaid] EliteLevelData 为空，PawnID: {pawn.thingIDNumber}");
                    return "";
                }


                return sb.ToString();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.m_SaveDataField, "CR_PowerupSaveDataField", null, false);
            Scribe_Values.Look<bool>(ref this.m_FirstMapSetting, "CR_PowerupFirstMapSetting", false, false);
            Scribe_Values.Look<bool>(ref this.m_RaidFriendly, "CR_PowerupRaidFriendly", false, false);
        }

        public abstract int Order { get; }

        private string m_SaveDataField;
        private bool m_FirstMapSetting = true;
        private bool m_RaidFriendly;
        private int m_GameTick = int.MinValue;
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup1 : CR_Powerup
    {
        public override int Order { get { return 1; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup2 : CR_Powerup
    {
        public override int Order { get { return 2; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup3 : CR_Powerup
    {
        public override int Order { get { return 3; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup4 : CR_Powerup
    {
        public override int Order { get { return 4; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup5 : CR_Powerup
    {
        public override int Order { get { return 5; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup6 : CR_Powerup
    {
        public override int Order { get { return 6; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup7 : CR_Powerup
    {
        public override int Order { get { return 7; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup8 : CR_Powerup
    {
        public override int Order { get { return 8; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup9 : CR_Powerup
    {
        public override int Order { get { return 9; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup10 : CR_Powerup
    {
        public override int Order { get { return 10; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup11 : CR_Powerup
    {
        public override int Order { get { return 11; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup12 : CR_Powerup
    {
        public override int Order { get { return 12; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup13 : CR_Powerup
    {
        public override int Order { get { return 13; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup14 : CR_Powerup
    {
        public override int Order { get { return 14; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup15 : CR_Powerup
    {
        public override int Order { get { return 15; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup16 : CR_Powerup
    {
        public override int Order { get { return 16; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup17 : CR_Powerup
    {
        public override int Order { get { return 17; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup18 : CR_Powerup
    {
        public override int Order { get { return 18; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup19 : CR_Powerup
    {
        public override int Order { get { return 19; } }
    }

    [StaticConstructorOnStartup]
    public class CR_Powerup20 : CR_Powerup
    {
        public override int Order { get { return 20; } }
    }
    [DataContract]
    public class EliteLevelData
    {
        [DataMember] // 添加特性
        public int Level { get; set; }
        [DataMember] // 添加特性
        public float DamageFactor { get; set; }
        [DataMember] // 添加特性
        public float MoveSpeedFactor { get; set; }
        [DataMember]
        public float ScaleFactor { get; set; }
        [DataMember]
        public int MaxTraits { get; set; }
        [DataMember]
        public bool IsBoss { get; set; }
        [DataMember]
        public bool addBioncis { get; set; }
        [DataMember]
        public bool addDrug { get; set; }
        [DataMember]
        public bool refineGear { get; set; }
        [DataMember]
        public bool armorEnhance { get; set; }
        [DataMember]
        public bool temperatureResist { get; set; }
        [DataMember]
        public bool meleeEnhance { get; set; }
        [DataMember]
        public bool rangedSpeedEnhance { get; set; }
        [DataMember]
        public bool moveSpeedEnhance { get; set; }
        [DataMember]
        public bool giantEnhance { get; set; }
        [DataMember]
        public bool bodyPartEnhance { get; set; }
        [DataMember]
        public bool painEnhance { get; set; }
        [DataMember]
        public bool moveSpeedResist { get; set; }
        [DataMember]
        public bool consciousnessEnhance { get; set; }

        // 保存 StatModifier 列表
        [DataMember]
        public List<StatModifierData> StatOffsets { get; set; } = new List<StatModifierData>();

        // 新增：保存乘算 StatModifier 列表
        [DataMember]
        public List<StatModifierData> StatFactors { get; set; } = new List<StatModifierData>();

        // 保存 PawnCapacityModifier 列表
        [DataMember]
        public List<PawnCapacityModifierData> CapMods { get; set; } = new List<PawnCapacityModifierData>();

        // 用于序列化 StatModifier 的嵌套类
        [DataContract]
        public class StatModifierData
        {
            [DataMember]
            public string StatDefName { get; set; }
            [DataMember]
            public float Value { get; set; }
        }

        // 用于序列化 PawnCapacityModifier 的嵌套类
        [DataContract]
        public class PawnCapacityModifierData
        {
            [DataMember]
            public string CapacityDefName { get; set; }
            [DataMember]
            public float Offset { get; set; }
        }
        private bool firstCall = true;
        private int lastTraitCount = -1;
        // 获取激活的词条列表（使用普通方法替代表达式）
        public List<string> GetActiveTraitNames()
        {
            var traits = new List<string>();
            if (addBioncis) traits.Add("addBioncis");
            if (addDrug) traits.Add("addDrug");
            if (refineGear) traits.Add("refineGear");
            if (armorEnhance) traits.Add("armorEnhance");
            if (temperatureResist) traits.Add("temperatureResist");
            if (meleeEnhance) traits.Add("meleeEnhance");
            if (rangedSpeedEnhance) traits.Add("rangedSpeedEnhance");
            if (moveSpeedEnhance) traits.Add("moveSpeedEnhance");
            if (giantEnhance) traits.Add("giantEnhance");
            if (bodyPartEnhance) traits.Add("bodyPartEnhance");
            if (painEnhance) traits.Add("painEnhance");
            if (moveSpeedResist) traits.Add("moveSpeedResist");
            if (consciousnessEnhance) traits.Add("consciousnessEnhance");
            if (firstCall || traits.Count != lastTraitCount)
            {
                //  Log.Message($"[EliteRaid] 等级 {Level} 激活的词条：{string.Join(", ", traits)}");
                firstCall = false;
                lastTraitCount = traits.Count;
            }

            return traits;
        }

        // 获取词条显示名称（使用 if-else 替代 switch）
        public string GetTraitDisplayName(string traitName)
        {
            string colorCode;

            switch (traitName)
            {
                case "addBioncis": colorCode = "39FF14"; break;      // 亮绿色（电绿）
                case "addDrug": colorCode = "FFA500"; break;         // 橙色（保持不变）
                case "refineGear": colorCode = "B0E2FF"; break;      // 亮蓝色
                case "armorEnhance": colorCode = "00FFFF"; break;    // 青色（亮蓝绿）
                case "temperatureResist": colorCode = "FF6EC7"; break; // 亮紫色
                case "meleeEnhance": colorCode = "FF3030"; break;    // 亮红色
                case "rangedSpeedEnhance": colorCode = "1E90FF"; break; // 道奇蓝
                case "moveSpeedEnhance": colorCode = "7FFF00"; break; // 春绿色
                case "giantEnhance": colorCode = "FFD700"; break;    // 金色（保持不变）
                case "bodyPartEnhance": colorCode = "FF6347"; break;  // 番茄红（保持不变）
                case "painEnhance": colorCode = "FF4500"; break;     // 橙红色
                case "moveSpeedResist": colorCode = "40E0D0"; break;  // 亮青色
                case "consciousnessEnhance": colorCode = "98FB98"; break; // 苍绿色
                default: colorCode = "FFFFFF"; break;                // 默认白色
            }

            // 使用与等级颜色相同的格式化方式，同时保持原有翻译键
            string translatedText;

            switch (traitName)
            {
                case "addBioncis": translatedText = "TraitAddBioncis".Translate(); break;
                case "addDrug": translatedText = "TraitAddDrug".Translate(); break;
                case "refineGear": translatedText = "TraitRefineGear".Translate(); break;
                case "armorEnhance": translatedText = "TraitArmorEnhance".Translate(); break;
                case "temperatureResist": translatedText = "TraitTemperatureResist".Translate(); break;
                case "meleeEnhance": translatedText = "TraitMeleeEnhance".Translate(); break;
                case "rangedSpeedEnhance": translatedText = "TraitRangedSpeedEnhance".Translate(); break;
                case "moveSpeedEnhance": translatedText = "TraitMoveSpeedEnhance".Translate(); break;
                case "giantEnhance": translatedText = "TraitGiantEnhance".Translate(); break;
                case "bodyPartEnhance": translatedText = "TraitBodyPartEnhance".Translate(); break;
                case "painEnhance": translatedText = "TraitPainEnhance".Translate(); break;
                case "moveSpeedResist": translatedText = "TraitMoveSpeedResist".Translate(); break;
                case "consciousnessEnhance": translatedText = "TraitConsciousnessEnhance".Translate(); break;
                default: translatedText = traitName; break;
            }

            return string.Format("<color=#{0}>{1}</color>", colorCode, translatedText);
        }

    }

    public static class JsonHelper
    {
        // 对象序列化为 JSON
        public static string Serialize<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // JSON 反序列化为对象
        public static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}