using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;
using UnityEngine;
using System.Reflection.Emit;
using static EliteRaid.StaticVariables_ModCompatibility;
using System.Runtime.InteropServices;
using RimwoldEliteRaidProject.Core;
using Verse.Noise;
using RimWorld.Planet;
using static EliteRaid.DropPodUtility_Patch;

namespace EliteRaid
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("trigger.eliteraid");
            Patcher(harmony);
            harmony.PatchAll();
            // harmony.PatchAll(Assembly.GetExecutingAssembly());
            BionicsDataStore.DataRestore();
            DrugHediffDataStore.DataRestore();
        
        }

        struct TargetMethod
        {
            public Type type;
            public MethodInfo method;
            public TargetMethod(Type type, MethodInfo method)
            {
                this.type = type;
                this.method = method;
            }
            public override string ToString()
            {
                return String.Format("{0}.{1}", type.FullName, method.Name);
            }
            public override bool Equals(object obj)
            {
                MethodInfo m = obj as MethodInfo;
                if (m == null)
                {
                    return false;
                }
                return this.GetHashCode() == m.GetHashCode();
            }
            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }
        }

        private static void Patcher(Harmony harmony)
        {
            CompatibilityPatches.Patcher(harmony, out bool mser, out bool ppai, out bool nmr);
            ManualSafePatch(harmony);
        }

        private static void ManualSafePatch(Harmony harmony)
        {
            HashSet<TargetMethod> targetMethods = new HashSet<TargetMethod>();
            TranspilerTest(harmony, targetMethods);

            try
            {
                Type targetClass = typeof(StorytellerUtility);
                MethodInfo targetMethod = AccessTools.Method(
                    targetClass,
                    "DefaultThreatPointsNow",
                    new Type[] { typeof(IIncidentTarget) }
                );

                if (targetMethod == null)
                {
                    Log.Error($"[EliteRaid] 找不到 StorytellerUtility.DefaultThreatPointsNow 方法！");
                    return;
                }

                MethodInfo transpilerMethod = AccessTools.Method(
                    typeof(StorytellerUtility_DefaultThreatPointsNow_Patch),
                    nameof(StorytellerUtility_DefaultThreatPointsNow_Patch.Transpiler)
                );

                if (transpilerMethod == null)
                {
                    Log.Error($"[EliteRaid] 找不到 StorytellerUtility_DefaultThreatPointsNow_Patch.Transpiler 方法！");
                    return;
                }

                var transpiler = new HarmonyMethod(transpilerMethod);

                harmony.Patch(
                    original: targetMethod,
                    transpiler: transpiler
                );

            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册 StorytellerUtility.DefaultThreatPointsNow 补丁失败: {ex.Message}");
                Log.Error(ex.StackTrace);
            }

            //注册鼠族隧道补丁
            RatkinTunnelPatch.RegisterRatkinTunnelPatches(harmony);

            try
            {
               Type targetClass = typeof(ActiveTransporter);
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 开始注册ActiveTransporter.PodOpen补丁，目标类: {targetClass.FullName}");
               }
               
               MethodInfo targetMethod = AccessTools.Method(
                   targetClass,
                   "PodOpen",
                   Type.EmptyTypes
               );

               if (targetMethod == null)
               {
                   Log.Error($"[EliteRaid] 找不到 ActiveDropPod.PodOpen 方法！");
                   return;
               }
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 成功找到目标方法: {targetMethod.Name}");
               }

               var transpiler = new HarmonyMethod(
                   typeof(ActiveDropPod_PodOpen_Patch),
                   nameof(ActiveDropPod_PodOpen_Patch.Transpiler)
               );
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 创建Transpiler补丁方法: {typeof(ActiveDropPod_PodOpen_Patch).FullName}.{nameof(ActiveDropPod_PodOpen_Patch.Transpiler)}");
               }

               harmony.Patch(
                   original: targetMethod,
                   transpiler: transpiler
               );

               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 成功为 ActiveDropPod.PodOpen 添加 Transpiler 补丁！");
               }
            } catch (Exception ex)
            {
               Log.Error($"[EliteRaid] 注册补丁失败: {ex.Message}");
               Log.Error(ex.StackTrace);
            }

            try
            {
               Type targetClass = typeof(DropPodUtility);
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 开始注册DropPodUtility.DropThingGroupsNear补丁，目标类: {targetClass.FullName}");
               }
               
               MethodInfo targetMethod = AccessTools.Method(
                   targetClass,
                   nameof(DropPodUtility.DropThingGroupsNear),
                   new Type[] {
                       typeof(IntVec3),
                       typeof(Map),
                       typeof(List<List<Thing>>),
                       typeof(int),
                       typeof(bool),
                       typeof(bool),
                       typeof(bool),
                       typeof(bool),
                       typeof(bool),
                       typeof(bool),
                       typeof(Faction)
                   }
               );

               if (targetMethod == null)
               {
                   Log.Error($"[EliteRaid] 找不到 {nameof(DropPodUtility.DropThingGroupsNear)} 方法！");
                   return;
               }
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 成功找到目标方法: {targetMethod.Name}");
               }

               var postfix = new HarmonyMethod(
                   typeof(DropPodUtility_Patch),
                   nameof(DropPodUtility_Patch.DropThingGroupsNear_Postfix)
               );
               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 创建Postfix补丁方法: {typeof(DropPodUtility_Patch).FullName}.{nameof(DropPodUtility_Patch.DropThingGroupsNear_Postfix)}");
               }

               postfix.after = new[] { "other.mod.id.patch" };

               harmony.Patch(
                   original: targetMethod,
                   postfix: postfix
               );

               if (EliteRaidMod.displayMessageValue)
               {
                   Log.Message($"[EliteRaid] 成功为 {nameof(DropPodUtility.DropThingGroupsNear)} 添加 Postfix 补丁！");
               }
            } catch (Exception ex)
            {
               Log.Error($"[EliteRaid] 注册 DropThingGroupsNear 补丁失败: {ex.Message}");
               Log.Error(ex.StackTrace);
            }

            Type orgType = typeof(PawnGroupMakerUtility);
            string orgName = nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints);
            MethodInfo method = null;

            if (!General.m_CanTranspilerGeneratePawns)
            {
                method = AccessTools.Method(orgType, orgName, new Type[] { typeof(float), typeof(List<PawnGenOption>), typeof(PawnGroupMakerParms) });
                try
                {
                    harmony.Patch(method,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(PawnGroupMakerUtility_Patch), nameof(PawnGroupMakerUtility_Patch.ChoosePawnGenOptionsByPoints_Finalizer), new Type[] { typeof(Exception), typeof(float), typeof(List<PawnGenOption>), typeof(PawnGroupMakerParms), typeof(IEnumerable<PawnGenOption>).MakeByRefType() }) { methodType = MethodType.Normal });
                        if (EliteRaidMod.displayMessageValue)
                        {
                            Log.Message($"[EliteRaid] PawnGroupMakerUtility_Patch.ChoosePawnGenOptionsByPoints_Finalizer注册成功");
                        }
                } catch (Exception ex)
                {
                    
                        Log.Error($"[EliteRaid] PawnGroupMakerUtility_Patch.ChoosePawnGenOptionsByPoints_Finalizer注册失败: {ex.ToString()}");
                    
                }
            }

            foreach (TargetMethod targetMethod in targetMethods)
            {
                Type workerType = targetMethod.type;
                method = targetMethod.method;

                try
                {
                    harmony.Patch(method,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(PawnGroupKindWorker_Patch), nameof(PawnGroupKindWorker_Patch.GeneratePawns_Finalizer), new Type[] { typeof(Exception), typeof(PawnGroupMakerParms), typeof(List<Pawn>) }) { methodType = MethodType.Normal });

                    if (General.m_CanTranspilerGeneratePawns)
                    {
                        harmony.Patch(method,
                            null,
                            null,
                            new HarmonyMethod(typeof(PawnGroupKindWorker_Patch), nameof(PawnGroupKindWorker_Patch.GeneratePawns_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>), typeof(MethodBase) }) { methodType = MethodType.Normal });
                        if (EliteRaidMod.displayMessageValue)
                        {
                            Log.Message($"[EliteRaid] PawnGroupKindWorker_Patch.GeneratePawns_Transpiler注册成功");
                        }
                    }
                    General.m_AllowPawnGroupKindWorkerTypes.Add(workerType);
                } catch (Exception ex)
                {
                  
                        Log.Error($"[EliteRaid] PawnGroupKindWorker_Patch.GeneratePawns_Finalizer注册失败: {ex.ToString()}");
                   
                }
            }

            orgType = typeof(AggressiveAnimalIncidentUtility);
            orgName = nameof(AggressiveAnimalIncidentUtility.GenerateAnimals);
            method = AccessTools.Method(orgType, orgName, new Type[] { typeof(PawnKindDef), typeof(PlanetTile), typeof(float), typeof(int) });
            if (General.m_CanTranspilerGenerateAnimals)
            {
                try
                {
                    harmony.Patch(method,
                        null,
                        null,
                        new HarmonyMethod(typeof(ManhunterPackIncidentUtility_Patch), nameof(ManhunterPackIncidentUtility_Patch.GenerateAnimals_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>) }) { methodType = MethodType.Normal });

                    harmony.Patch(method,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(ManhunterPackIncidentUtility_Patch), nameof(ManhunterPackIncidentUtility_Patch.GenerateAnimals_Finalizer), new Type[] { typeof(Exception), typeof(List<Pawn>).MakeByRefType(), typeof(PawnKindDef) }) { methodType = MethodType.Normal });
                } catch (Exception ex)
                {
                    Log.Error($"[EliteRaid] GenerateAnimals_Transpiler和GenerateAnimals_Finalizer注册失败: {ex}");
                }
            } else
            {
                try
                {
                    harmony.Patch(
                        method,
                        new HarmonyMethod(
                            typeof(ManhunterPackIncidentUtility_Patch),
                            nameof(ManhunterPackIncidentUtility_Patch.GenerateAnimals_Prefix),
                            new Type[] {
                                typeof(List<Pawn>).MakeByRefType(),
                                typeof(PawnKindDef),
                                typeof(PlanetTile),
                                typeof(float),
                                typeof(int)
                            }
                        )
                        { methodType = MethodType.Normal }
                    );
                } catch (Exception ex)
                {
                    Log.Error($"[EliteRaid] GenerateAnimals_Prefix注册失败: {ex}");
                }
            }

            Type entitySwarmType = typeof(IncidentWorker_EntitySwarm);
            MethodInfo generateEntitiesMethod = AccessTools.Method(
                typeof(IncidentWorker_EntitySwarm),
                "GenerateEntities",
                new Type[] { typeof(IncidentParms), typeof(float) }
            );

            if (General.m_CanTranspilerEntitySwarm)
            {
                try
                {
                    harmony.Patch(
                        generateEntitiesMethod,
                        new HarmonyMethod(typeof(EntitySwarmIncidentUtility_Patch), nameof(EntitySwarmIncidentUtility_Patch.GenerateEntities_Prefix))
                    );

                    harmony.Patch(
                        generateEntitiesMethod,
                        null,
                        null,
                        new HarmonyMethod(typeof(EntitySwarmIncidentUtility_Patch), nameof(EntitySwarmIncidentUtility_Patch.GenerateEntities_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>) }) { methodType = MethodType.Normal }
                    );

                    harmony.Patch(
                        generateEntitiesMethod,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(EntitySwarmIncidentUtility_Patch), nameof(EntitySwarmIncidentUtility_Patch.GenerateEntities_Finalizer),
                            new Type[] { typeof(Exception), typeof(List<Pawn>).MakeByRefType(), typeof(IncidentParms), typeof(float) })
                        { methodType = MethodType.Normal }
                    );

                  
                } catch (Exception ex)
                {
                   
                }
            }

            try
            {
                Type shamblerAssaultType = typeof(IncidentWorker_ShamblerAssault);
                MethodInfo postProcessMethod = AccessTools.Method(
                    shamblerAssaultType,
                    "PostProcessSpawnedPawns",
                    new Type[] { typeof(IncidentParms), typeof(List<Pawn>) }
                );

                if (postProcessMethod != null)
                {
                    harmony.Patch(
                        postProcessMethod,
                        null,
                        new HarmonyMethod(typeof(ShamblerAssault_Patch), nameof(ShamblerAssault_Patch.PostProcessSpawnedPawns_Postfix))
                    );

                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 成功为 IncidentWorker_ShamblerAssault.PostProcessSpawnedPawns 添加 Postfix 补丁！");
                    }
                }

                MethodInfo tryGenerateRaidInfoMethod = AccessTools.Method(
                    typeof(IncidentWorker_Raid),
                    "TryGenerateRaidInfo",
                    new Type[] { typeof(IncidentParms), typeof(List<Pawn>).MakeByRefType(), typeof(bool) }
                );

                if (tryGenerateRaidInfoMethod != null)
                {
                    harmony.Patch(
                        tryGenerateRaidInfoMethod,
                        new HarmonyMethod(typeof(ShamblerAssault_Patch), nameof(ShamblerAssault_Patch.TryGenerateRaidInfo_Prefix))
                    );

                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 成功为 IncidentWorker_Raid.TryGenerateRaidInfo 添加 Prefix 补丁！");
                    }
                }
                
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册蹒跚怪袭击补丁失败: {ex.Message}");
                Log.Error(ex.StackTrace);
            }

            try
            {
                Type shamblerSwarmAnimalsType = typeof(IncidentWorker_ShamblerSwarmAnimals);
                MethodInfo shamblerSwarmAnimalsGenerateEntitiesMethod = AccessTools.Method(
                    shamblerSwarmAnimalsType,
                    "GenerateEntities",
                    new Type[] { typeof(IncidentParms), typeof(float) }
                );

                if (shamblerSwarmAnimalsGenerateEntitiesMethod != null)
                {
                    harmony.Patch(
                        shamblerSwarmAnimalsGenerateEntitiesMethod,
                        null,
                        new HarmonyMethod(typeof(ShamblerSwarmAnimals_Patch), nameof(ShamblerSwarmAnimals_Patch.GenerateEntities_Postfix))
                    );

                    if (EliteRaidMod.displayMessageValue)
                    {
                        Log.Message($"[EliteRaid] 成功为 IncidentWorker_ShamblerSwarmAnimals.GenerateEntities 添加 Postfix 补丁！");
                    }
                }
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册 IncidentWorker_ShamblerSwarmAnimals 补丁失败: {ex.Message}");
            }
        }

        private static void TranspilerTest(Harmony harmony, HashSet<TargetMethod> targetMethods)
        {
            MethodInfo methodGeneratePawns_TestTramspiler = AccessTools.Method(typeof(PawnGroupKindWorker_Patch), nameof(PawnGroupKindWorker_Patch.GeneratePawns_Test_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>) });
            MethodInfo orgMethod;
            foreach (AllowPawnGroupKindWorkerTypeDef allowedWorker in DefDatabase<AllowPawnGroupKindWorkerTypeDef>.AllDefs)
            {
                if (allowedWorker.workerTypeNames != null)
                {
                    foreach (string workerTypeName in allowedWorker.workerTypeNames.Distinct())
                    {
                        Type workerType = AccessTools.TypeByName(workerTypeName);
                        if ((workerType?.IsSubclassOf(typeof(PawnGroupKindWorker)) ?? false))
                        {
                            orgMethod = AccessTools.Method(workerType, nameof(PawnGroupKindWorker.GeneratePawns), new Type[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(List<Pawn>), typeof(bool) });
                            TargetMethod targetMethod = new TargetMethod(workerType, orgMethod);
                            if (orgMethod?.DeclaringType == workerType && !targetMethods.Contains(targetMethod))
                            {
                                targetMethods.Add(targetMethod);
                                if (General.m_CanTranspilerGeneratePawns)
                                {
                                    try
                                    {
                                        harmony.Patch(orgMethod,
                                            null,
                                            null,
                                            new HarmonyMethod(methodGeneratePawns_TestTramspiler) { methodType = MethodType.Normal });
                                        harmony.Unpatch(orgMethod, methodGeneratePawns_TestTramspiler);
                                    } catch
                                    {
                                        General.m_CanTranspilerGeneratePawns = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            MethodInfo methodGenerateAnimals_TestTramspiler = AccessTools.Method(typeof(ManhunterPackIncidentUtility_Patch), nameof(ManhunterPackIncidentUtility_Patch.GenerateAnimals_Test_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>) });
            try
            {
                orgMethod = AccessTools.Method(typeof(AggressiveAnimalIncidentUtility), nameof(AggressiveAnimalIncidentUtility.GenerateAnimals), new Type[] { typeof(PawnKindDef), typeof(PlanetTile), typeof(float), typeof(int) });
                harmony.Patch(orgMethod,
                    null,
                    null,
                    new HarmonyMethod(methodGenerateAnimals_TestTramspiler) { methodType = MethodType.Normal });
                harmony.Unpatch(orgMethod, methodGenerateAnimals_TestTramspiler);
            } catch
            {
                General.m_CanTranspilerGenerateAnimals = false;
            }
        }
    }

    #region Patch Manually

    #region PawnGroupKindWorker_Normal Patches
    [StaticConstructorOnStartup]
    class PawnGroupKindWorker_Patch
    {
        internal static IEnumerable<CodeInstruction> GeneratePawns_Test_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int index = 0;
            CodeInstruction pre = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched && ci.opcode == OpCodes.Callvirt && pre?.opcode == OpCodes.Call)
                {
                    MemberInfo member = pre.operand as MemberInfo;
                    matched = member?.Name == nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints);
                }
                pre = ci;
                index++;
            }
            General.m_CanTranspilerGeneratePawns &= matched;
            return instructions;
        }

        internal static IEnumerable<CodeInstruction> GeneratePawns_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            int index = 0;
            CodeInstruction pre = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched && ci.opcode == OpCodes.Callvirt && pre?.opcode == OpCodes.Call)
                {
                    MemberInfo member = pre.operand as MemberInfo;
                    if (member?.Name == nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints))
                    {
#if DEBUG
                        Log.Message(String.Format("@@@ target={0}.{1} index={2} HIT!!!", original.DeclaringType, original.Name, index));
#endif
                        matched = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_1); //parms(PawnGroupMakerParms parms)
                        yield return CodeInstruction.Call(typeof(PatchContinuityHelper), nameof(PatchContinuityHelper.SetCompressWork_GeneratePawns), new Type[] { typeof(IEnumerable<PawnGenOptionWithXenotype>), typeof(PawnGroupMakerParms) });
                    }
                }
                yield return ci;
                pre = ci;
                index++;
            }
        }

        internal static Exception GeneratePawns_Finalizer(Exception __exception, PawnGroupMakerParms parms, List<Pawn> outPawns)
        {
            if (parms == null)
            {
                Log.Error("[EliteRaid] PawnGroupMakerParms is null in GeneratePawns_Finalizer");
                return __exception;
            }
            if (__exception == null)
            {
                General.GeneratePawns_Impl(parms, outPawns);
            }
            return __exception;
        }
    }
    #endregion

    #region PawnGroupMakerUtility Patch
    [StaticConstructorOnStartup]
    class PawnGroupMakerUtility_Patch
    {
        public static Exception ChoosePawnGenOptionsByPoints_Finalizer(Exception __exception, float pointsTotal, List<PawnGenOption> options, PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOption> __result)
        {
            if (__exception == null)
            {
                // Store raid type distribution info
                if (EliteRaidMod.displayMessageValue && options != null)
                {
                    var distribution = options.GroupBy(x => x.kind?.defName ?? "unknown")
                        .Select(g => new { Type = g.Key, Count = g.Count() });
                    
                    Log.Message($"[EliteRaid] Raid type distribution before compression:");
                    foreach (var entry in distribution)
                    {
                        Log.Message($"  {entry.Type}: {entry.Count}");
                    }
                }

                // Store the compressed distribution for later use
                if (options != null)
                {
                    PatchContinuityHelper.CompressedPawnGenOptions = options.ToList();
                }

                PatchContinuityHelper.SetCompressWork_GeneratePawns(groupParms, ref __result);
            }
            return __exception;
        }
    }

    #endregion

    #region ManhunterPackGenerate.GenerateAnimals
    [StaticConstructorOnStartup]
    class ManhunterPackIncidentUtility_Patch
    {

        internal static IEnumerable<CodeInstruction> GenerateAnimals_Test_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction pre1 = null;
            CodeInstruction pre2 = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched)
                {
                    matched = ci.opcode == OpCodes.Ldc_I4_0 && pre1?.opcode == OpCodes.Stloc_1 && pre2?.opcode == OpCodes.Ldarg_3;
                }
                pre2 = pre1;
                pre1 = ci;
            }
            General.m_CanTranspilerGenerateAnimals &= matched;
#if DEBUG
            Log.Message(String.Format("@@@ GenerateAnimals_Test_Transpiler matched={0}", General.m_CanTranspilerGenerateAnimals));
#endif
            return instructions;
        }

        internal static IEnumerable<CodeInstruction> GenerateAnimals_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction pre1 = null;
            CodeInstruction pre2 = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched && ci.opcode == OpCodes.Ldc_I4_0 && pre1?.opcode == OpCodes.Stloc_1 && pre2?.opcode == OpCodes.Ldarg_3)
                {
                    matched = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //animalKind(PawnKindDef animalKind)
                    yield return new CodeInstruction(OpCodes.Ldloc_1); //baseNum(int num)
                    yield return CodeInstruction.Call(typeof(PatchContinuityHelper), nameof(PatchContinuityHelper.SetCompressWork_GenerateAnimals), new Type[] { typeof(PawnKindDef), typeof(int) });
                    yield return new CodeInstruction(OpCodes.Stloc_1); //store value (int num)
                }
                yield return ci;
                pre2 = pre1;
                pre1 = ci;
            }
        }

        internal static Exception GenerateAnimals_Finalizer(Exception __exception, ref List<Pawn> __result, PawnKindDef animalKind)
        {
            if (__exception == null)
            {
                General.GenerateAnimals_Impl(animalKind, __result);
            }
            return __exception;
        }

        internal static bool GenerateAnimals_Prefix(ref List<Pawn> __result, PawnKindDef animalKind, PlanetTile tile, float points, int animalCount = 0)
        {
            if (EliteRaidMod.modEnabled || !EliteRaidMod.allowAnimalsValue)
            {
                return true;
            }

            List<Pawn> list = new List<Pawn>();
            int baseNum = (animalCount > 0) ? animalCount : AggressiveAnimalIncidentUtility.GetAnimalsCount(animalKind, points);
            int maxPawnNum = General.GetenhancePawnNumber(baseNum);

            PatchContinuityHelper.SetCompressWork_GenerateAnimals(animalKind, baseNum);

            if (maxPawnNum >= baseNum)
            {
                return true;
            }

            bool allowedCompress = true;
            if (!EliteRaidMod.allowMechanoidsValue && (animalKind?.RaceProps?.IsMechanoid ?? false))
            {
                allowedCompress = false;
            }
            if (!EliteRaidMod.allowInsectoidsValue && animalKind?.RaceProps?.FleshType == FleshTypeDefOf.Insectoid)
            {
                allowedCompress = false;
            }

            if (!allowedCompress)
            {
                return true;
            }

            General.GenerateAnimals_Impl(animalKind, list);

            __result = list;
            return false;
        }
    }

    #endregion

    #region EntitySwarm.GenerateEntities
    [StaticConstructorOnStartup]
    class EntitySwarmIncidentUtility_Patch
    {
        // 测试 Transpiler 是否可行
        internal static IEnumerable<CodeInstruction> GenerateEntities_Test_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction pre1 = null;
            CodeInstruction pre2 = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched)
                {
                    // 匹配生成实体的关键指令（根据实际方法调整）
                    matched = ci.opcode == OpCodes.Callvirt && pre1?.opcode == OpCodes.Call && pre2?.opcode == OpCodes.Ldarg_0;
                }
                pre2 = pre1;
                pre1 = ci;
            }
            General.m_CanTranspilerEntitySwarm &= matched;
            return instructions;
        }

        // Transpiler：修改生成数量
        internal static IEnumerable<CodeInstruction> GenerateEntities_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction pre1 = null;
            CodeInstruction pre2 = null;
            bool matched = false;
            foreach (CodeInstruction ci in instructions)
            {
                if (!matched && ci.opcode == OpCodes.Callvirt && pre1?.opcode == OpCodes.Call && pre2?.opcode == OpCodes.Ldarg_0)
                {
                    matched = true;
                    // 插入压缩逻辑：获取参数并调用辅助方法
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // parms (IncidentParms)
                    yield return CodeInstruction.Call(typeof(PatchContinuityHelper), nameof(PatchContinuityHelper.SetCompressWork_EntitySwarm));
                }
                yield return ci;
                pre2 = pre1;
                pre1 = ci;
            }
        }

        // Prefix：拦截生成过程，实现真正的压缩
        public static bool GenerateEntities_Prefix(IncidentWorker_EntitySwarm __instance, IncidentParms parms, float points, ref List<Pawn> __result)
        {
            if (!EliteRaidMod.modEnabled || !EliteRaidMod.allowEntitySwarmValue)
            {
                return true; // 继续执行原始方法
            }

            // 检查是否为蹒跚怪袭击
            bool isShamblerRaid = parms.faction == Faction.OfEntities || 
                                 parms.pawnKind?.defName?.Contains("Shambler") == true ||
                                 parms.pawnKind?.defName?.Contains("Gorehulk") == true;

            if (!isShamblerRaid)
            {
                return true; // 继续执行原始方法
            }
            Log.Message("实体生成捕获到了"+__instance.GetType().Name);
            // 计算基础数量（根据points估算）
            int estimatedBaseNum = Mathf.RoundToInt(points / 100f); // 假设每个pawn约100点
            int maxPawnNum = General.GetenhancePawnNumber(estimatedBaseNum);

            if (maxPawnNum >= estimatedBaseNum)
            {
                return true; // 不需要压缩，继续执行原始方法
            }

            Log.Message($"[EliteRaid] 蹒跚怪袭击Prefix压缩: 估算{estimatedBaseNum} → {maxPawnNum}");

            // 修改parms中的点数，实现真正的压缩
            float originalPoints = parms.points;
            parms.points = maxPawnNum * 100f; // 根据压缩后的数量调整点数

            // 让原始方法执行，但使用修改后的点数
            bool result = true; // 这里应该调用原始方法，但我们用Postfix来处理

            // 在Postfix中处理增强
            return true; // 继续执行原始方法
        }

        // Finalizer：处理生成结果并增强
        internal static Exception GenerateEntities_Finalizer(Exception __exception, ref List<Pawn> __result, IncidentParms parms, float points)
        {
            if (__exception == null && __result != null && __result.Count > 0)
            {
                // 检查是否为蹒跚怪袭击
                bool isShamblerRaid = __result.Any(p => p.Faction == Faction.OfEntities ||
                                                       p.kindDef?.defName?.Contains("Shambler") == true ||
                                                       p.kindDef?.defName?.Contains("Gorehulk") == true);

                if (isShamblerRaid && EliteRaidMod.modEnabled && EliteRaidMod.allowEntitySwarmValue)
                {
                    int baseNum = __result.Count;
                    int maxPawnNum = General.GetenhancePawnNumber(baseNum);

                    if (maxPawnNum < baseNum)
                    {
                        Log.Message($"[EliteRaid] 蹒跚怪袭击Finalizer压缩: {baseNum} → {maxPawnNum}");

                        // 压缩数量
                        __result = __result.Take(maxPawnNum).ToList();

                        // 增强压缩后的pawn
                        General.GenerateEntitys_Impl(__result, baseNum, maxPawnNum);

                        // 显示消息
                        if (EliteRaidMod.displayMessageValue)
                        {
                            Messages.Message($"蹒跚怪袭击已压缩: {baseNum} → {maxPawnNum}", MessageTypeDefOf.NeutralEvent);
                        }
                    }
                }
            }
            return __exception;
        }
    }
    #endregion

    #endregion

    #region Patch Harmony Automatic

    #region RaidStrategyWorker Patches
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(RaidStrategyWorker))]
    class RaidStrategyWorkerPatch
    {
        [HarmonyPatch(new Type[] { typeof(IncidentParms) })]
        public static bool SpawnThreats_Prefix(RaidStrategyWorker __instance, ref List<Pawn> __result, IncidentParms parms)
        {
            if (EliteRaidMod.displayMessageValue)
                Log.Message("输出人数" + parms.pawnCount + "输出种类" + parms.pawnKind);

            // 全面检查事件参数的有效性
            if (parms == null || parms.target == null || !(parms.target is Map map) || !map.IsPlayerHome)
            {
                Log.Error("[EliteRaid] 事件参数无效或地图对象为空，跳过补丁处理");
                return true; // 让原始方法处理
            }
            if (!EliteRaidMod.modEnabled)
            {
                return true; // 如果mod未启用，让原始方法处理
            }
            if (parms?.pawnKind == null || parms?.faction == null || parms?.raidArrivalMode?.Worker == null)
            {
                Log.Error("[EliteRaid] 事件参数不完整，跳过补丁处理");
                return true; // 让原始方法处理
            }

            bool allowedCompress = true;
            if (!EliteRaidMod.allowMechanoidsValue && (parms?.pawnKind?.RaceProps?.IsMechanoid ?? false))
            {
                allowedCompress = false;
            }
            if (!EliteRaidMod.allowInsectoidsValue && parms?.pawnKind?.RaceProps?.FleshType == FleshTypeDefOf.Insectoid)
            {
                allowedCompress = false;
            }
            int maxPawnNum = EliteRaidMod.maxRaidEnemy;
            int baseNum = parms.pawnCount;

            // 如果不需要压缩，让原始方法处理
            if (maxPawnNum >= baseNum)
            {
                return true;
            }

            if (!allowedCompress)
            {
                return true;
            }

            // 限制最大生成数量
            parms.pawnCount = Math.Min(maxPawnNum, parms.pawnCount);

            EliteLevelManager.GenerateLevelDistribution(baseNum);
            int enhancePawnNumber = EliteLevelManager.getCurrentLevelDistributionNum();
            int order = PowerupUtility.GetNewOrder();
            int enhancedCount = 0;
            List<Pawn> list = new List<Pawn>();
            List<Pawn> nonCompressiblePawns = new List<Pawn>(); // 存储不可压缩的pawn

            // 优先使用PatchContinuityHelper保存的压缩分布
            bool usedCompressedOptions = false;
            if (PatchContinuityHelper.CompressedPawnGenOptions != null && PatchContinuityHelper.CompressedPawnGenOptions.Count > 0)
            {
                // 日志：输出乱序前的种类分布
                if (EliteRaidMod.displayMessageValue)
                {
                    var beforeKinds = string.Join(", ", PatchContinuityHelper.CompressedPawnGenOptions.Select(x => x.kind?.defName ?? "null"));
                    Log.Message($"[EliteRaid] 压缩分布乱序前: {beforeKinds}");
                }
                // 乱序后只取前parms.pawnCount个
                var options = PatchContinuityHelper.CompressedPawnGenOptions.OrderBy(x => Rand.Value).Take(enhancePawnNumber).ToList();
                // 日志：输出乱序后的种类分布
                if (EliteRaidMod.displayMessageValue)
                {
                    var afterKinds = string.Join(", ", options.Select(x => x.kind?.defName ?? "null"));
                    Log.Message($"[EliteRaid] 压缩分布乱序后: {afterKinds}");
                }
                foreach (var option in options)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(option.kind, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: __instance.def.pawnsCanBringFood)
                    {
                        BiocodeApparelChance = 1f
                    });
                    if (pawn != null)
                    {
                        list.Add(pawn);
                    }
                }
                PatchContinuityHelper.CompressedPawnGenOptions = null; // 用完清空
                usedCompressedOptions = true;
            }

            // 如果没有用到压缩分布，走原有逻辑
            if (!usedCompressedOptions)
            {
                // 添加安全退出条件，防止无限循环
                int maxAttempts = Math.Min(Math.Max(baseNum * 2, 100), 500); // 最多尝试2倍基础数量，最大不超过500次
                int attempts = 0;

                // 修改循环逻辑，确保生成足够的pawn
                while (list.Count < parms.pawnCount && enhancePawnNumber < baseNum && attempts < maxAttempts)
                {
                    attempts++;

                    PawnKindDef pawnKind = parms.pawnKind;
                    Faction faction = parms.faction;
                    float biocodeWeaponsChance = parms.biocodeWeaponsChance;
                    float biocodeApparelChance = parms.biocodeApparelChance;

                    Log.Message("捕获到当前生成的敌人阵营"+ parms.faction);
                    Messages.Message("捕获到当前生成的敌人阵营"+ parms.faction, MessageTypeDefOf.NeutralEvent);
              
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(parms.pawnKind, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: __instance.def.pawnsCanBringFood)
                   {
                       BiocodeApparelChance = 1f
                    });

                    if (pawn != null)
                    {
                        enhancePawnNumber++;

                        // 使用新的检查方法判断是否可压缩
                        bool canCompress = allowedCompress && PawnStateChecker.CanCompressPawn(pawn);

                        if (canCompress)
                        {
                            // 可压缩的pawn处理逻辑
                            if (MOD_MSER_Active)
                            {
                                pawn.health.AddHediff(CR_DummyForCompatibilityDefOf.CR_DummyForCompatibility);
                            }

                            if (EliteRaidMod.modEnabled)
                            {
                                PawnWeaponChager.CheckAndReplaceMainWeapon(pawn);

                                if (EliteRaidMod.AllowCompress(pawn))
                                {
                                    EliteLevel eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                                    Hediff powerup = PowerupUtility.SetPowerupHediff(pawn, order);
                                    if (powerup != null)
                                    {
                                        bool powerupEnable = PowerupUtility.TrySetStatModifierToHediff(powerup, eliteLevel);
                                        if (powerupEnable)
                                        {
                                     }
                                    }
                                }
                            }

                            list.Add(pawn);
                        } else
                        {
                            // 不可压缩的pawn直接添加到列表
                            list.Add(pawn);
                        }
                    }
                }
            }

            PawnWeaponChager.ResetCounter();

            if (list.Any<Pawn>())
            {
                if (allowedCompress)
                {
                    //DummyForCompatibility除去ここから
                    if (MOD_MSER_Active)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            Pawn p = list[i];
                            CR_DummyForCompatibility dummyHediff = p.health.hediffSet.hediffs.Where(x => x is CR_DummyForCompatibility).Cast<CR_DummyForCompatibility>().FirstOrDefault();
                            if (dummyHediff != null)
                            {
                                p.health.RemoveHediff(dummyHediff);
                            }
                        }
                    }
                    //DummyForCompatibility除去ここまで
                }

                // 确保地图对象仍然有效
                if (map != null && map.IsPlayerHome)
                {
                    try
                    {
                        parms.raidArrivalMode.Worker.Arrive(list, parms);
                        __result = list;

                        if (enhancedCount > 0&&EliteRaidMod.showRaidMessages)
                        {
                            int finalNum = General.GetenhancePawnNumber(baseNum);
                            Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum
                            , finalNum, General.GetcompressionRatio(baseNum, maxPawnNum)
                            , finalNum), MessageTypeDefOf.NeutralEvent, true);
                        }

                        // 保底：如果没有生成人类，则强制生成一个人类pawn
                        if (!list.Any(p => !p.RaceProps.Animal))
                        {
                            PawnKindDef humanKind = null;
                            // 优先从压缩分布中找一个非动物kind
                            if (PatchContinuityHelper.CompressedPawnGenOptions != null)
                            {
                                var humanOption = PatchContinuityHelper.CompressedPawnGenOptions.FirstOrDefault(x => !(x.kind?.RaceProps?.Animal ?? true));
                                if (humanOption != null)
                                    humanKind = humanOption.kind;
                            }
                            // 如果找不到，兜底用parms.pawnKind
                            if (humanKind == null && parms.pawnKind != null && !(parms.pawnKind.RaceProps?.Animal ?? true))
                            {
                                humanKind = parms.pawnKind;
                            }
                            // 生成保底人类
                            if (humanKind != null)
                            {
                                // 日志：输出保底生成人类的种类
                                if (EliteRaidMod.displayMessageValue)
                                {
                                    Log.Message($"[EliteRaid] 保底生成人类: {humanKind.defName}");
                                }
                                Pawn humanPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(humanKind, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: __instance.def.pawnsCanBringFood)
                                {
                                    BiocodeApparelChance = 1f
                                });
                                if (humanPawn != null)
                                {
                                    list.Insert(0, humanPawn); // 插入到最前面
                                }
                            }
                        }

                        return false; // 跳过原始方法
                    } catch (Exception ex)
                    {
                        Log.Error($"[EliteRaid] 部署pawn时出错: {ex.Message}");
                        // 出错时让原始方法处理
                        return true;
                    }
                } else
                {
                    Log.Error("[EliteRaid] 地图对象无效，无法部署pawn");
                    // 地图无效时让原始方法处理
                    return true;
                }
            } else
            {
                Log.Warning("[EliteRaid] 未能生成任何pawn，让原始方法处理");
                // 未能生成任何pawn时让原始方法处理
                return true;
            }
        }


        public static int GetenhancePawnNumber(int baseNum)
        {
            return General.GetenhancePawnNumber(baseNum);
        }

        public static float GetcompressionRatio(int baseNum, int maxPawnNum)
        {
            if (maxPawnNum <= 0) return 1f;
            return (float)baseNum / maxPawnNum;
        }
    }
    #endregion

    #region TunnelHiveSpawner Patches
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(RimWorld.InfestationUtility), "SpawnTunnels")]
public static class InfestationUtility_SpawnTunnels_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ref int hiveCount, Map map, bool spawnAnywhereIfNoGoodCell, bool ignoreRoofedRequirement, string questTag, IntVec3? overrideLoc, ref float? insectsPoints)
    {
         // 输出原始参数，便于调试
            Log.Message($"[EliteRaid] InfestationUtility.SpawnTunnels Prefix: 原始 hiveCount={hiveCount}, map={map}, insectsPoints={insectsPoints}");
            // 假设每个虫巢平均生成2只虫子
            int baseNum = hiveCount * 2;
            int maxPawnNum = General.GetenhancePawnNumber(baseNum);
            int newHiveCount = Math.Max(1, maxPawnNum / 2);
            if (hiveCount > newHiveCount&&EliteRaidMod.showRaidMessages)
            {
                  Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum, newHiveCount*2,
                     General.GetcompressionRatio(baseNum * 2, newHiveCount * 2).ToString("F2"), 0), MessageTypeDefOf.NeutralEvent, true);
                hiveCount = newHiveCount*2;
            }
    }
}
    #endregion
    [HarmonyPatch(typeof(StorytellerUtility), "DefaultThreatPointsNow")]
    public static class StorytellerUtility_DefaultThreatPointsNow_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindLastIndex(
                c => c.opcode == OpCodes.Ldc_R4 &&
                c.operand.ToString() == "10000");

            if (index >= 0)
            {
                codes[index].operand = 10000000f;
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 成功将袭击点数上限修改为 10000000");
                }
            }
            else if (EliteRaidMod.displayMessageValue)
            {
              //  Log.Warning($"[EliteRaid] 未找到袭击点数上限的硬编码值，可能游戏版本已变化");
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(StorytellerUtility), "DefaultThreatPointsNow")]
    public static class Patch_DefaultThreatPointsNow
    {
        public static void Postfix(ref float __result)
        {
            if (__result > EliteRaidMod.maxRaidPoint)
            {
                __result = EliteRaidMod.maxRaidPoint;
            }
        }
    }

    #endregion

    #region ShamblerAssault Patch
    [StaticConstructorOnStartup]
    class ShamblerAssault_Patch
    {
        public static int SaveBaseNum=0;
        public static int SaveMaxPawnNum=0;
       public static void PostProcessSpawnedPawns_Postfix(IncidentWorker_ShamblerAssault __instance, IncidentParms parms, List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            if (!EliteRaidMod.modEnabled || !EliteRaidMod.allowEntitySwarmValue)
            {
                return;
            }

            bool isShamblerAssault = pawns.Any(p => p.Faction == Faction.OfEntities || 
                                                   p.kindDef?.defName?.Contains("Shambler") == true ||
                                                   p.kindDef?.defName?.Contains("Gorehulk") == true);

            if (!isShamblerAssault)
            {
                return;
            }

            if (SaveMaxPawnNum >= SaveBaseNum)
            {
                return;
            }

            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message($"[EliteRaid] 蹒跚怪袭击PostProcess压缩: {SaveBaseNum} → {SaveMaxPawnNum}");
            }

            var compressedPawns = pawns.Take(SaveMaxPawnNum).ToList();
            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message($"[EliteRaid] 蹒跚怪袭击PostProcess压缩: {compressedPawns.Count}");
            }

            General.GenerateAnything_Impl(compressedPawns, SaveBaseNum, SaveMaxPawnNum, false);

            if (EliteRaidMod.displayMessageValue)
            {
                Messages.Message($"蹒跚怪袭击已压缩: {SaveBaseNum} → {SaveMaxPawnNum}", MessageTypeDefOf.NeutralEvent);
            }
        }
        
        [HarmonyPatch(typeof(IncidentWorker_Raid))]
        [HarmonyPatch("TryGenerateRaidInfo")]
        public static bool TryGenerateRaidInfo_Prefix(IncidentWorker_Raid __instance, ref IncidentParms parms, ref List<Pawn> pawns)
        {
            if (EliteRaidMod.displayMessageValue)
            {
                Log.Message($"[EliteRaid] TryGenerateRaidInfo_Prefix开始处理: instance={__instance.GetType().Name}");
            }
            
            if (!EliteRaidMod.modEnabled)
            {
                return true;
            }

            if (__instance is IncidentWorker_ShamblerAssault)
            {
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 检测到蹒跚怪袭击");
                }
                
                int baseNum = (int)(parms.points/50f);
                
                if (EliteRaidMod.displayMessageValue)
                {
                    Log.Message($"[EliteRaid] 蹒跚怪袭击原始参数: points={parms.points}, pawnCount={parms.pawnCount}, baseNum={baseNum}");
                }
                
                if (EliteRaidMod.AllowCompress(parms))
                {
                    int maxPawnNum = General.GetenhancePawnNumber(baseNum);
                    SaveBaseNum=baseNum;
                    SaveMaxPawnNum=maxPawnNum;
                    
                    if (maxPawnNum < baseNum)
                    {
                        if (EliteRaidMod.displayMessageValue)
                        {
                            Log.Message($"[EliteRaid] 蹒跚怪袭击压缩: {baseNum} → {maxPawnNum}");
                        }
                        
                        parms.pawnCount = maxPawnNum;
                        parms.points=maxPawnNum*50f;
                    }
                }
            }

            return true;
        }
    }
    #endregion

    #region ShamblerSwarmAnimals Patch
    [StaticConstructorOnStartup]
    class ShamblerSwarmAnimals_Patch
    {
        public static void GenerateEntities_Postfix(IncidentWorker_ShamblerSwarmAnimals __instance, IncidentParms parms, float points, ref List<Pawn> __result)
        {
            if (__result == null || __result.Count == 0)
            {
                return;
            }

            if (!EliteRaidMod.modEnabled || !EliteRaidMod.allowEntitySwarmValue)
            {
                return;
            }

            // 检查是否为蹒跚怪动物群
            bool isShamblerAnimals = __result.Any(p => p.Faction == Faction.OfEntities || 
                                                       p.kindDef?.defName?.Contains("Shambler") == true ||
                                                       p.kindDef?.defName?.Contains("Chimera") == true);

            if (!isShamblerAnimals)
            {
                return;
            }

            int baseNum = __result.Count;
            int maxPawnNum = General.GetenhancePawnNumber(baseNum);

            if (maxPawnNum >= baseNum)
            {
                return; // 不需要压缩
            }

            Log.Message($"[EliteRaid] 蹒跚怪动物群压缩: {baseNum} → {maxPawnNum}");

            // 压缩数量并增强
            __result = __result.Take(maxPawnNum).ToList();
            
            // 调用增强逻辑
            General.GenerateEntitys_Impl(__result, baseNum, maxPawnNum);

            // 显示消息
            if (EliteRaidMod.displayMessageValue)
            {
                Messages.Message($"蹒跚怪动物群已压缩: {baseNum} → {maxPawnNum}", MessageTypeDefOf.NeutralEvent);
            }
        }
    }
    #endregion

}
