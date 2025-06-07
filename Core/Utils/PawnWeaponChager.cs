using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimwoldEliteRaidProject.Core
{
     class PawnWeaponChager
    {

        private static int targetWeaponCount = 0;

        public static void CheckAndReplaceMainWeapon(Pawn pawn)
        {
            bool combatExtendedActive = ModLister.HasActiveModWithName("Combat Extended");
           // Log.Message($"{0} Combat Extended 模组状态: {combatExtendedActive}");

            if (combatExtendedActive)
            {
                Log.Message($" 检测到 CE 模组已启用，跳过武器替换逻辑");
                return;
            }

            var startTime = DateTime.Now;
            string logPrefix = $"[{startTime:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid][CheckAndReplaceMainWeapon]";
            try
            {
               // Log.Message($"{logPrefix} 开始处理 pawn={pawn.LabelCap}, ID={pawn.thingIDNumber}");

                if (pawn.equipment == null)
                {
                  //  Log.Message($"{logPrefix} 处理失败: pawn.equipment 为空");
                    return;
                }

                if (pawn.equipment.Primary == null)
                {
                 //   Log.Message($"{logPrefix} 处理失败: pawn无主武器");
                    return;
                }

                // 获取主武器
                ThingWithComps primaryWeapon = pawn.equipment.Primary;
               // Log.Message($"{logPrefix} 当前主武器: {primaryWeapon.Label}, defName={primaryWeapon.def.defName}");

                // 检查是否为目标武器
                bool isTarget = IsTargetWeapon(primaryWeapon);
                //   Log.Message($"{logPrefix} 是否为目标武器: {isTarget}");

                if (isTarget)
                {
                    // 如果目标武器数量已达到3，强制替换
                    bool replaceChance = targetWeaponCount >= 5 || Rand.Chance(0.7f);
                    Log.Message($"[EliteRaid] 替换概率判定: {replaceChance} (当前数量={targetWeaponCount}, 阈值=3)");

                    if (replaceChance)
                    {
                        ReplaceWithRandomWeapon(pawn);
                    }
                }

                var duration = (DateTime.Now - startTime).TotalMilliseconds;
               // Log.Message($"{logPrefix} 处理完成, 耗时={duration:F1}ms");
            } catch (Exception ex)
            {
               // Log.Error($"{logPrefix} 发生异常: {ex.Message}, StackTrace={ex.StackTrace}");
            }
        }

        // 判断是否为目标武器
        private static bool IsTargetWeapon(ThingWithComps weapon)
        {
            if (weapon == null || weapon.def == null)
            {
              //  Log.Message($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid][IsTargetWeapon] 武器或其def为空, 结果=false");
                return false;
            }

            string[] targetDefNames = { "Gun_DoomsdayRocket", "Gun_TripleRocket" }; // 更新后的DefName
            bool result = targetDefNames.Contains(weapon.def.defName);
          //  Log.Message($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid][IsTargetWeapon] " +
                      //  $"weapon={weapon.Label}, defName={weapon.def.defName}, 结果={result}");
            return result;
        }

        private static void ReplaceWithRandomWeapon(Pawn pawn)
        {
            var startTime = DateTime.Now;
            string logPrefix = $"[{startTime:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid][ReplaceWithRandomWeapon]";
            try
            {
              //  Log.Message($"{logPrefix} 开始为 pawn={pawn.LabelCap} 生成随机武器替换");

                // 前置条件检查
                if (pawn.equipment == null || pawn.equipment.Primary == null)
                {
                 //   Log.Message($"{logPrefix} 替换失败: 基础条件不满足");
                    return;
                }

                // 保存原武器信息
                ThingWithComps oldWeapon = pawn.equipment.Primary;
                float oldWeaponValue = oldWeapon.MarketValue;

                // 模拟创建一个武器生成请求
                var request = new PawnGenerationRequest(
                    kind: pawn.kindDef,
                    faction: pawn.Faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: pawn.Map?.Tile ?? -1,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true,
                    colonistRelationChanceFactor: 1f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowFood: false,
                    inhabitant: false,
                    certainlyBeenInCryptosleep: false,
                    forceRedressWorldPawnIfFormerColonist: false,
                    worldPawnFactionDoesntMatter: false,
                    biocodeWeaponChance: 0f,
                    extraPawnForExtraRelationChance: null,
                    relationWithExtraPawnChanceFactor: 1f
                );

                // 临时保存非主武器装备
                List<ThingWithComps> otherEquipment = new List<ThingWithComps>();
                foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                {
                    if (eq != oldWeapon)
                    {
                        otherEquipment.Add(eq);
                        pawn.equipment.TryDropEquipment(eq, out _, pawn.Position);
                    }
                }

                // 只摧毁主武器
                pawn.equipment.DestroyEquipment(oldWeapon);
              //  Log.Message($"{logPrefix} 已摧毁主武器: {oldWeapon.Label}");

                // 使用游戏内置的武器生成器
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn, request);

                // 检查是否成功生成武器
                ThingWithComps newWeapon = pawn.equipment.Primary;

                // 保底逻辑：如果新武器仍然是目标武器，强制替换为速射机枪
                if (newWeapon != null && IsTargetWeapon(newWeapon))
                {
                  //  Log.Message($"{logPrefix} 生成的武器仍为目标武器: {newWeapon.Label}, 强制替换为速射机枪");

                    // 摧毁生成的目标武器
                    pawn.equipment.DestroyEquipment(newWeapon);

                    // 创建速射机枪
                    ThingDef minigunDef = DefDatabase<ThingDef>.GetNamed("Gun_Minigun", false);
                    if (minigunDef != null)
                    {
                        newWeapon = (ThingWithComps)ThingMaker.MakeThing(minigunDef);

                        // 设置武器风格
                        if (pawn.kindDef.weaponStyleDef != null)
                        {
                            newWeapon.StyleDef = pawn.kindDef.weaponStyleDef;
                        } else if (pawn.Ideo != null)
                        {
                            newWeapon.StyleDef = pawn.Ideo.GetStyleFor(minigunDef);
                        }

                        // 添加速射机枪
                        pawn.equipment.AddEquipment(newWeapon);
                      //  Log.Message($"{logPrefix} 已强制装备速射机枪: {newWeapon.Label}");
                    } else
                    {
                        Log.Error($"{logPrefix} 找不到速射机枪定义(Gun_Minigun)，无法强制替换");
                        // 恢复原武器
                        pawn.equipment.AddEquipment(oldWeapon);
                      //  Log.Message($"{logPrefix} 已恢复原武器");
                    }
                }

                // 处理最终结果
                if (newWeapon != null && !IsTargetWeapon(newWeapon))
                {
                   // Log.Message($"{logPrefix} 最终武器: {newWeapon.Label}, 价值={newWeapon.MarketValue:F1}");

                    // 恢复其他装备
                    foreach (var eq in otherEquipment)
                    {
                        pawn.equipment.AddEquipment(eq);
                    }

                    // 触发武器装备事件
                    TryNotifyWeaponChanged(pawn, oldWeapon, newWeapon);
                } else
                {
                  //  Log.Message($"{logPrefix} 未能生成合适的武器");

                    // 恢复原武器和其他装备
                    pawn.equipment.AddEquipment(oldWeapon);
                    foreach (var eq in otherEquipment)
                    {
                        pawn.equipment.AddEquipment(eq);
                    }

                   // Log.Message($"{logPrefix} 已恢复所有装备");
                }
            } catch (Exception ex)
            {
                Log.Error($"{logPrefix} 异常: {ex.Message}");
            }
        }
        // 尝试通知游戏武器已更改
        private static void TryNotifyWeaponChanged(Pawn pawn, ThingWithComps oldWeapon, ThingWithComps newWeapon)
        {
            try
            {
                // 检查 pawn 是否有装备管理器
                if (pawn.equipment != null)
                {
                    // 如果游戏有事件系统，可以在这里触发武器变更事件
                    // 例如: pawn.equipment.Notify_EquipmentChanged();

                    // 检查 pawn 是否有持械能力
                    if (pawn.stances != null && pawn.stances.curStance is Stance_Busy busyStance)
                    {
                        // 如果 pawn 正在使用武器，可能需要中断当前动作
                        if (busyStance.focusTarg.IsValid)
                        {
                            pawn.stances.CancelBusyStanceSoft();
                         //   Log.Message($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid] 已中断 pawn {pawn.LabelCap} 的当前动作");
                        }
                    }

                  //  Log.Message($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid] 已通知武器变更: {oldWeapon.Label} → {newWeapon.Label}");
                }
            } catch (Exception ex)
            {
                Log.Error($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][EliteRaid] 通知武器变更时出错: {ex.Message}");
            }
        }

     
    }
}
