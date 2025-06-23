//using HarmonyLib;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection.Emit;
//using Verse;
//using static EliteRaid.EliteLevelManager;
//using static EliteRaid.PowerupUtility;
//using static EliteRaid.CR_DummyForCompatibilityDefOf;
//using static EliteRaid.EliteRaidMod;
//using UnityEngine;
//using System.Collections.Concurrent;
//using System.Threading;
//using static System.Collections.Specialized.BitVector32;

//namespace EliteRaid
//{


//    [HarmonyPatch(typeof(DropPodUtility), nameof(DropPodUtility.DropThingGroupsNear))]
//    public static class DropPodUtility_Patch
//    {
//        // 存储待处理的Pawn组（合集）
//        public static ConcurrentDictionary<Guid, GroupData> pendingHostileGroups =
//        new ConcurrentDictionary<Guid, GroupData>();

//        public static Guid groupId;
//        private static string currentRaidTag = null;

//        // 新增：使用HashSet记录已经处理过的Pawn
//        private static readonly HashSet<int> processedPawnIds = new HashSet<int>();
//        // 新增：等级分组存储（等级 -> Pawn ID列表）
//        private static readonly ConcurrentDictionary<int, ConcurrentBag<int>> levelGroups =
//            new ConcurrentDictionary<int, ConcurrentBag<int>>();

//        // 每个等级的最大数量限制
//        private const int MaxPerLevel = 10;
//        // 后置补丁：识别敌对Pawn组并存储
//        public static void DropThingGroupsNear_Postfix(
//       List<List<Thing>> thingsGroups,
//       Map map,
//       Faction faction)
//        {
//            //  Log.Message($"[EliteRaid] DropThingGroupsNear_Postfix 开始执行，派系：{faction?.Name ?? "无"}");

//            if (!EliteRaidMod.allowDropPodRaidValue)
//            {
//                return;
//            }

//            // 检查是否有Pawn
//            var allPawns = new List<Pawn>();
//            foreach (var group in thingsGroups)
//            {
//                var groupPawns = group.OfType<Pawn>().ToList();
//                if (groupPawns.Any())
//                {
//                    //  Log.Message($"[EliteRaid] 从组中提取 {groupPawns.Count} 个 Pawn");
//                    allPawns.AddRange(groupPawns);
//                }
//            }

//            if (allPawns.Any())
//            {
//                // 判断是否为新的Raid
//                bool isNewRaid = currentRaidTag == null;

//                if (isNewRaid)
//                {
//                    // 创建新的RaidTag
//                    currentRaidTag = $"Raid_{DateTime.Now.Ticks}";
//                    //  Log.Message($"[EliteRaid] 创建新的RaidTag: {currentRaidTag}");
//                }

//                var groupId = Guid.NewGuid();
//                var groupData = new GroupData
//                {
//                    Pawns = allPawns,
//                    CreationTime = DateTime.Now,
//                    Faction = faction ?? allPawns[0].Faction,
//                    RaidTag = currentRaidTag,
//                    IsComplete = false // 初始状态为未完成
//                };
//                groupData.IsLevelGenerated = false;
//                pendingHostileGroups[groupId] = groupData;
//                ApplyHostileBuff(groupId, allPawns);
//                //  Log.Message($"[EliteRaid] 记录Pawn组：{allPawns.Count} 单位，ID={groupId}，RaidTag={currentRaidTag}，派系：{groupData.Faction?.Name}");

//                // 如果这是最后一个空投舱，标记Raid完成
//                if (IsLastDropPod(thingsGroups, map))
//                {
//                    MarkRaidComplete(currentRaidTag);
//                    currentRaidTag = null; // 重置当前RaidTag
//                }
//            }
//        }

//        // 判断是否为最后一个空投舱（需要根据游戏逻辑实现）
//        private static bool IsLastDropPod(List<List<Thing>> thingsGroups, Map map)
//        {
//            // 简单实现：如果当前有其他待处理的空投舱，返回false
//            // 实际实现可能需要检查游戏状态或其他指标
//            var activeDropPods = map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod);
//            return activeDropPods.Count <= 1; // 减去当前处理的这个
//        }

//        // 标记Raid完成
//        private static void MarkRaidComplete(string raidTag)
//        {
//            foreach (var kvp in pendingHostileGroups)
//            {
//                if (kvp.Value.RaidTag == raidTag)
//                {
//                    kvp.Value.IsComplete = true;
//                    //   Log.Message($"[EliteRaid] 标记Raid完成: {raidTag}");
//                }
//            }
//        }


//        // 判断是否为敌对组
//        private static bool IsHostileGroup(Faction faction, List<Pawn> pawns)
//        {
//            if (pawns.Count == 0) return false;

//            // 优先使用传入的派系
//            if (faction != null)
//            {
//                bool isHostile = faction.HostileTo(Faction.OfPlayer);
//                //  Log.Message($"[EliteRaid] 基于传入派系判断：{faction.Name} 是否敌对？ {isHostile}");
//                return isHostile;
//            }
//            // 新增：排除机械族（FactionDefOf.Mechanoids）
//            if (faction != null && faction.def == FactionDefOf.Mechanoid)
//            {
//         //       Log.Message("[EliteRaid] 检测到机械族派系，跳过强化");
//                return false;
//            }
//            // 否则检查 Pawn 自身派系（取第一个 Pawn 的派系作为代表）
//            var firstPawnFaction = pawns[0].Faction;
//            if (firstPawnFaction != null)
//            {
//                bool isHostile = firstPawnFaction.HostileTo(Faction.OfPlayer);
//                //  Log.Message($"[EliteRaid] 基于首个 Pawn 派系判断：{firstPawnFaction.Name} 是否敌对？ {isHostile}");
//                return isHostile;
//            }

//            Log.Warning($"[EliteRaid] 无法判断 Pawn 派系，默认非敌对");
//            return false;
//        }

//        // 修改ApplyHostileBuff方法，不再生成等级分布，仅存储pawn组
//        private static void ApplyHostileBuff(Guid groupId, List<Pawn> pawns)
//        {
//            // Log.Message($"[EliteRaid] ApplyHostileBuff 执行，组ID={groupId}，Pawn数量={pawns.Count}");

//            if (pawns.Count == 0) return;

//            // 确保组数据已正确存储
//            if (pendingHostileGroups.TryGetValue(groupId, out var groupData))
//            {
//                // Log.Message($"[EliteRaid] 待增强Pawn组存储完成，ID={groupId}，数量={pawns.Count}");
//            } else
//            {
//                Log.Error($"[EliteRaid] 组ID={groupId} 不存在，无法存储增强数据");
//            }
//        }

//        [HarmonyPatch(typeof(ActiveDropPod), "PodOpen", MethodType.Normal)]
//        public static class ActiveDropPod_PodOpen_Patch
//        {
//            // 修复 innerContainer 访问问题
//            // 使用 Transpiler 在生成物品前拦截
//            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//            {
//                var codes = instructions.ToList();
//                var processMethod = AccessTools.Method(typeof(ActiveDropPod_PodOpen_Patch), nameof(ProcessContents));

//                // 定位 ThingOwner.Count 的 get 方法（虚方法）
//                var countPropertyGetter = AccessTools.PropertyGetter(typeof(ThingOwner), "Count");
//                if (countPropertyGetter == null)
//                {
//                    Log.Error("[EliteRaid] 无法获取 ThingOwner.Count 的 Getter 方法");
//                    yield break;
//                }

//                for (int i = 0; i < codes.Count; i++)
//                {
//                    // 检查是否为 Callvirt ThingOwner.get_Count（虚方法调用）
//                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand == countPropertyGetter)
//                    {
//                        // 在调用 Count 前插入 ProcessContents（加载 this 指针）
//                        yield return new CodeInstruction(OpCodes.Ldarg_0); // 加载 ActiveDropPod 实例（this）
//                        yield return new CodeInstruction(OpCodes.Call, processMethod); // 调用静态方法

//                        // 保留原有指令
//                        yield return codes[i];
//                        continue;
//                    }

//                    yield return codes[i];
//                }
//            }

//            // 处理空投舱内容的方法
//            public static void ProcessContents(ActiveDropPod __instance)
//            {
//                return;

//                if (__instance == null) {
                
//                }
                
//                try
//                {
//                    if (__instance == null || __instance.Contents == null)
//                        return;
//                     Log.Message($"[EliteRaid] 处理空投舱内容，ID={__instance.thingIDNumber}");

//                    // 方法 1：使用泛型方法获取内容
//                    var innerContainer = __instance.Contents.innerContainer as ThingOwner<Thing>;
//                    if (innerContainer == null)
//                    {
//                          Log.Warning("[EliteRaid] 容器类型非 ThingOwner<Thing>，跳过处理");
//                        return;
//                    }
//                    List<Thing> allContents = innerContainer.InnerListForReading.ToList();

//                    if (allContents.Count == 0)
//                    {
//                           Log.Warning("[EliteRaid] 容器中无有效物品");
//                        return;
//                    }

//                    // 限制单次处理时间
//                    var startTime = Time.realtimeSinceStartup;
//                    const float maxProcessTime = 0.1f; // 100ms

//                    foreach (Thing thing in allContents)
//                    {
//                        if (Time.realtimeSinceStartup - startTime > maxProcessTime)
//                        {
//                            Log.Warning("[EliteRaid] 处理超时，中断本次循环");
//                            break;
//                        }

//                        if (thing is Pawn pawn)
//                        {
//                            // 新增：跳过玩家派系和机械族
//                            if (pawn.Faction == Faction.OfPlayer || pawn.Faction?.def == FactionDefOf.Mechanoid)
//                            {
//                                continue;
//                            }
//                            // 新增：检查Pawn是否已经被处理过
//                            if (!processedPawnIds.Contains(pawn.thingIDNumber))
//                            {
//                                ProcessSinglePawn(pawn);
//                                processedPawnIds.Add(pawn.thingIDNumber);
//                            }
//                        }
//                    }
//                } catch (Exception ex) {
//                    Log.Warning($"[EliteRaid] ProcessContents 空头仓里可能是物品: {ex.Message}");

//                }
               
//            }


//            // 修复 ApplyEliteBuffToSinglePawn 中的索引问题
//            private static void ApplyEliteBuffToSinglePawn(Pawn pawn, int baseNum, int maxNum)
//            {
//                if (!modEnabled) return;

//                // 修正：使用正确的 groupId 查找当前Pawn所在组
//                Guid matchingGroupId = FindMatchingGroupId(pawn);
//                if (matchingGroupId == Guid.Empty) return;

//                if (DropPodUtility_Patch.pendingHostileGroups.TryGetValue(matchingGroupId, out var groupData) && groupData != null)
//                {
//                    List<Pawn> group = groupData.Pawns;
//                    int pawnIndex = group.IndexOf(pawn); // 使用正确的组获取索引

//                    // 计算目标等级（基于索引）
//                    int targetLevel = pawnIndex % 8; // 0-7级

//                    // 从等级分组中获取等级
//                    // 由于没有GetLevelByGroup方法，我们需要重新生成一个等级
//                    var eliteLevel = EliteLevelManager.GetEliteLevelByLevel(targetLevel);
//                    if (eliteLevel == null)
//                    {
//                        eliteLevel = GetRandomEliteLevelWithFallback(targetLevel);
//                    }

//                    if (pawnIndex < maxNum && AllowCompress(pawn, true))
//                    {
//                        if (IsLevelValid(eliteLevel))
//                        {
//                            ApplyLevelToPawn(pawn, eliteLevel);
//                            Log.Message($"[EliteRaid] 成功为 {pawn.Name} 应用等级: Level {eliteLevel.Level}");
//                        } else
//                        {
//                            Log.Warning($"[EliteRaid] 无效等级: Level {eliteLevel?.Level ?? -1}，跳过应用");
//                        }
//                    } else
//                    {
//                        Log.Message($"[EliteRaid] {pawn.Name} 不在增强范围内（索引 {pawnIndex} >= 最大数 {maxNum}）");
//                    }
//                }
//            }
//            // 尝试将Pawn添加到指定等级的分组
//            private static bool TryAddToLevelGroup(int level, int pawnId)
//            {
//                // 获取或创建等级分组
//                var levelGroup = levelGroups.GetOrAdd(level, _ => new ConcurrentBag<int>());

//                // 检查是否已达上限
//                if (levelGroup.Count >= MaxPerLevel)
//                {
//                    return false;
//                }

//                // 添加到分组
//                levelGroup.Add(pawnId);
//                return true;
//            }

//            // 获取随机等级，带降级处理
//            private static EliteLevel GetRandomEliteLevelWithFallback(int preferredLevel = -1)
//            {
//                EliteLevel eliteLevel;

//                if (preferredLevel >= 0)
//                {
//                    // 尝试生成指定等级
//                    eliteLevel = EliteLevelManager.GetRandomEliteLevel();

//                    // 调整到首选等级
//                    if (eliteLevel.Level != preferredLevel)
//                    {
//                        // 这里需要根据实际情况调整等级
//                        // 简单实现：重新生成直到获得所需等级或尝试次数用尽
//                        int attempts = 0;
//                        while (eliteLevel.Level != preferredLevel && attempts < 5)
//                        {
//                            eliteLevel = EliteLevelManager.GetRandomEliteLevel();
//                            attempts++;
//                        }
//                    }
//                } else
//                {
//                    // 正常随机等级生成
//                    eliteLevel = EliteLevelManager.GetRandomEliteLevel();
//                }

//                // 验证等级有效性
//                if (!IsLevelValid(eliteLevel))
//                {
//                    Log.Warning($"[EliteRaid] 生成的等级无效: Level {eliteLevel?.Level ?? -1}，使用后备等级");

//                    // 使用后备方法生成有效等级
//                    eliteLevel = EliteLevelManager.GetRandomEliteLevel();
//                }

//                return eliteLevel;
//            }


//            // 修改ProcessSinglePawn方法，使用等级分组管理器
//            private static void ProcessSinglePawn(Thing thing)
//            {
//                try
//                {
//                    if (thing == null) return;
//                    Pawn pawn = thing as Pawn;
//                    if (pawn == null)
//                    {
//                        Log.Warning($"[EliteRaid] 无效Pawn对象: {thing.def.defName}");
//                        return;
//                    }
//                    // 新增：检查是否为玩家派系的单位，若是则直接返回不处理
//                    if (pawn.Faction == Faction.OfPlayer)
//                    {
//                        Log.Message($"[EliteRaid] 检测到玩家派系单位 {pawn.Name}，跳过处理");
//                        return;
//                    }

//                    // 新增：判断是否为机械族
//                    if (pawn.Faction?.def == FactionDefOf.Mechanoid)
//                    {
//                        //   Log.Message($"[EliteRaid] 机械族单位 {pawn.Name}，跳过强化");
//                        return;
//                    }
//                   // Log.Message("pendingHostileGroups"+ pendingHostileGroups.Count+"to string"+ pendingHostileGroups.ToString());
//                    Guid matchingGroupId = FindMatchingGroupId(pawn);
//                    if (matchingGroupId == Guid.Empty)
//                    {
//                        Log.Warning($"[EliteRaid] 未找到匹配的Pawn组，Pawn：{pawn.Name}");
//                        return;
//                    }

//                    if (DropPodUtility_Patch.pendingHostileGroups.TryGetValue(matchingGroupId, out var groupData) && groupData != null)
//                    {
//                        List<Pawn> group = groupData.Pawns;
//                        int baseNum = group.Count;
//                        int maxNum = Mathf.Max(1, (int)(baseNum * compressionRatio));
//                        //     Log.Message($"[EliteRaid] 处理Pawn：{pawn.Name}，组ID={matchingGroupId}，组内数量={baseNum}，最大增强数={maxNum}");
//                        if (group.Count < EliteRaidMod.maxRaidEnemy)
//                        {
//                            return;
//                        }
//                        // 新增：如果还没有生成等级分布，生成它
//                        if (!groupData.IsLevelGenerated)
//                        {
//                            GenerateLevelDistribution(group.Count);
//                            groupData.IsLevelGenerated = true;
//                        }

//                        // 为当前pawn获取等级
//                        var eliteLevel = GetRandomEliteLevelWithFallback();

//                        // 添加到等级分组
//                        int cappedLevel = Math.Min(eliteLevel.Level, 7); // 限制最大等级为7

//                        if (TryAddToLevelGroup(cappedLevel, pawn.thingIDNumber))
//                        {
//                              Log.Message($"[EliteRaid] {pawn.Name} 添加到等级 {cappedLevel} 分组");
//                        } else
//                        {
//                               Log.Message($"[EliteRaid] {pawn.Name} 未能添加到等级 {cappedLevel} 分组（已达上限）");
//                            // 尝试降级处理
//                            int lowerLevel = Math.Max(1, cappedLevel - 1);

//                            // 重新生成一个较低等级
//                            eliteLevel = GetRandomEliteLevelWithFallback(lowerLevel);
//                            cappedLevel = Math.Min(eliteLevel.Level, 7);

//                            if (TryAddToLevelGroup(cappedLevel, pawn.thingIDNumber))
//                            {
//                                   Log.Message($"[EliteRaid] {pawn.Name} 降级添加到等级 {cappedLevel} 分组");
//                            } else
//                            {
//                                  Log.Message($"[EliteRaid] {pawn.Name} 所有等级已满，使用最低有效等级");
//                                eliteLevel = new EliteLevel(0, 1, 1, 1, 0);
//                                cappedLevel = Math.Min(eliteLevel.Level, 7);
//                                TryAddToLevelGroup(cappedLevel, pawn.thingIDNumber);
//                            }
//                        }

//                        // 应用等级
//                        ApplyEliteBuffToSinglePawn(pawn, baseNum, maxNum);

//                        group.Remove(pawn);
//                        if (group.Count == 0)
//                        {
//                            GroupData removedGroup;
//                            DropPodUtility_Patch.pendingHostileGroups.TryRemove(matchingGroupId, out removedGroup);
//                               Log.Message($"[EliteRaid] 安全移除组ID={matchingGroupId}");

//                            // 清理等级分组
//                            levelGroups.Clear();
//                        }
//                    } else
//                    {
//                        Log.Warning($"[EliteRaid] 组ID={matchingGroupId} 已失效，Pawn：{pawn.Name}");
//                    }
//                } catch (Exception ex) {
//                    Log.Error($"[EliteRaid] ProcessSinglePawn 异常: {ex.Message}");
//                }
//            }

//            // 修改FindMatchingGroupId方法（改进版）
//            private static Guid FindMatchingGroupId(Pawn pawn)
//            {
//                if (pawn == null)
//                {
//                    Log.Error("[EliteRaid] FindMatchingGroupId: Pawn参数为空");
//                    return Guid.Empty;
//                }

//                // 尝试通过RaidTag匹配
//                if (pawn.Faction != null)
//                {
//                    // 优先查找已完成的组
//                    var completedGroups = DropPodUtility_Patch.pendingHostileGroups
//                        .Where(g => g.Value.Faction == pawn.Faction && g.Value.IsComplete)
//                        .ToList();

//                    if (completedGroups.Any())
//                    {
//                        var closestGroup = completedGroups
//                            .OrderByDescending(g => g.Value.CreationTime)
//                            .First();

//                          Log.Message($"[EliteRaid] 通过RaidTag找到匹配组，ID={closestGroup.Key}，Pawn：{pawn.Name}");
//                        return closestGroup.Key;
//                    }

//                    // 如果没有已完成的组，查找最近的未完成组
//                    var incompleteGroups = DropPodUtility_Patch.pendingHostileGroups
//                        .Where(g => g.Value.Faction == pawn.Faction && !g.Value.IsComplete)
//                        .ToList();

//                    if (incompleteGroups.Any())
//                    {
//                        var closestGroup = incompleteGroups
//                            .OrderByDescending(g => g.Value.CreationTime)
//                            .First();

//                          Log.Message($"[EliteRaid] 通过RaidTag找到匹配的未完成组，ID={closestGroup.Key}，Pawn：{pawn.Name}");
//                        return closestGroup.Key;
//                    }
//                }

//                // 直接检查Pawn是否存在于任何组中（改进点）
//                foreach (var kvp in DropPodUtility_Patch.pendingHostileGroups)
//                {
//                    if (kvp.Value.Pawns.Contains(pawn))
//                    {
//                          Log.Message($"[EliteRaid] 通过直接匹配找到组，ID={kvp.Key}，Pawn：{pawn.Name}");
//                        return kvp.Key;
//                    }
//                }

//                // 传统匹配方式作为后备
//                foreach (var kvp in DropPodUtility_Patch.pendingHostileGroups)
//                {
//                    if (kvp.Value.Pawns.Any(p => p.thingIDNumber == pawn.thingIDNumber))
//                    {
//                         Log.Message($"[EliteRaid] 通过ID找到匹配组，ID={kvp.Key}，Pawn：{pawn.Name}");
//                        return kvp.Key;
//                    }
//                }

//                 Log.Message($"[EliteRaid] 未找到匹配组，Pawn：{pawn.Name}，派系：{pawn.Faction?.Name ?? "无"}");
//                return Guid.Empty;
//            }

//            // 省略重复方法...

//            private static int hediffDefCounter = 0;
//            // 创建唯一HediffDef（带完整属性复制和错误处理）
//            // 创建唯一HediffDef（匹配游戏实际定义格式）
//            // 创建唯一HediffDef（匹配游戏实际定义格式）
//            private static HediffDef CreateUniqueHediff(Pawn pawn, int level)
//            {
//                try
//                {
//                    if (pawn == null)
//                    {
//                        Log.Error("[EliteRaid] 创建HediffDef时Pawn为空");
//                        return null;
//                    }

//                    // 限制等级范围：1-7
//                    int cappedLevel = Mathf.Clamp(level, 1, 7);

//                    // 计算该等级对应的HediffDef起始编号
//                    int baseId = (cappedLevel - 1) * 10 + 1;

//                    // 从该等级的可用编号中随机选择一个（每个等级10个变体）
//                    int variant = Rand.RangeInclusive(0, 9);
//                    int defId = baseId + variant;

//                    // 构建符合实际定义的名称：CR_Powerup{编号}
//                    string defName = $"CR_Powerup{defId}";

//                    // 尝试获取已存在的定义
//                    HediffDef existingDef = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
//                    if (existingDef != null)
//                    {
//                        Log.Message($"[EliteRaid] 复用现有HediffDef: {defName}");
//                        return existingDef;
//                    }

//                    // 如果定义不存在，记录错误但不尝试创建新定义
//                    Log.Error($"[EliteRaid] 找不到HediffDef: {defName}");
//                    return null;
//                } catch (Exception ex)
//                {
//                    Log.Error($"[EliteRaid] 创建HediffDef失败: {ex.Message}");
//                    return null;
//                }
//            }

//            // 应用等级到单个Pawn（带有效性检查和完整属性应用）
//            private static void ApplyLevelToPawn(Pawn pawn, EliteLevel level)
//            {
//                if (pawn == null || level == null)
//                {
//                    Log.Error("[EliteRaid] 应用等级时Pawn或EliteLevel为空");
//                    return;
//                }
//                Log.Warning($"[EliteRaid]ApplyLevelToPawn!");
//                // 验证等级有效性
//                if (!IsLevelValid(level))
//                {
//                    Log.Warning($"[EliteRaid] 无效等级: Level {level.Level}，跳过应用");
//                    return;
//                }

//                // 创建唯一Hediff
//                HediffDef uniqueDef = CreateUniqueHediff(pawn, level.Level);
//                if (uniqueDef == null) return;
//                Log.Warning($"[EliteRaid]CreateUniqueHediff后的 uniqueDef" + uniqueDef.ToString());
//                // 获取或创建Hediff实例
//                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(uniqueDef) ??
//                               pawn.health.AddHediff(uniqueDef);
//                  Log.Warning($"[EliteRaid]hediff后的 hediff" + hediff.ToString());
//                if (hediff == null)
//                {
//                    Log.Error($"[EliteRaid] 无法获取Hediff实例: {uniqueDef.defName}");
//                    return;
//                }

//                // 应用等级属性到Hediff
//                PowerupUtility.TrySetStatModifierToHediff(hediff, level);

//                //CR_Powerup crPowerup = hediff as CR_Powerup;
//                //if (crPowerup != null)
//                //{
//                //    crPowerup.CreateSaveData(level);
//                //}

//                // 记录日志
//                  Log.Message($"[EliteRaid] 为 {pawn.Name} 应用等级: Level {level.Level}");
//            }



//            // 有效性检查
//            private static bool IsLevelValid(EliteLevel level)
//            {
//                return level != null &&
//                       level.Level != 0&&level.Level<=7;
                     
//            }

//        }

//        public class GroupData
//        {
//            public List<Pawn> Pawns { get; set; }
//            public DateTime CreationTime { get; set; }
//            public Faction Faction { get; set; }
//            public string RaidTag { get; set; } // 新增：标记同一次袭击
//            public bool IsComplete { get; set; } // 新增：标记组是否已完成
//                                                 // 新增：标记是否已生成等级分布
//            public bool IsLevelGenerated { get; set; }
//        }

//    }
//}