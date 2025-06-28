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
            
            // 初始化体型修改功能
            Log.Message("[EliteRaid] 体型修改功能已初始化");
            
            // 确认虫巢生成补丁已加载
            Log.Message("[EliteRaid] HiveSpawnLoggerPatch 补丁已加载");
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
          //  Log.Message($"[EliteRaid] Harmony initialized with ID: {harmony.Id}");
            CompatibilityPatches.Patcher(harmony, out bool mser, out bool ppai, out bool nmr);
          //  harmony.PatchAll(Assembly.GetExecutingAssembly());
            ManualSafePatch(harmony);
        }

        private static void ManualSafePatch(Harmony harmony)
        {
         //   Log.Message("ManualSafePatch执行了");
            HashSet<TargetMethod> targetMethods = new HashSet<TargetMethod>();
            TranspilerTest(harmony, targetMethods);

            //HiveSpawnLoggerPatch.ApplyPatches(harmony);

            try
            {
                Type targetClass = typeof(TunnelHiveSpawner);
                MethodInfo targetMethod = AccessTools.Method(
                    targetClass,
                    "Spawn",
                    new Type[] { typeof(Map), typeof(IntVec3) }
                );

                if (targetMethod == null)
                {
                    Log.Error($"[EliteRaid] 找不到 TunnelHiveSpawner.Spawn 方法！");
                    return;
                }

                // 获取补丁方法
                MethodInfo prefixMethod = AccessTools.Method(
                    typeof(TunnelHiveSpawner_Spawn_Patch),
                    nameof(TunnelHiveSpawner_Spawn_Patch.Spawn_Prefix)
                );

                if (prefixMethod == null)
                {
                    Log.Error($"[EliteRaid] 找不到 TunnelHiveSpawner_Spawn_Patch.Spawn_Prefix 方法！");
                    return;
                }

                // 创建Harmony补丁
                var prefix = new HarmonyMethod(prefixMethod);

                // 应用补丁
                harmony.Patch(
                    original: targetMethod,
                    prefix: prefix
                );

             //   Log.Message($"[EliteRaid] 成功为 TunnelHiveSpawner.Spawn 添加 Prefix 补丁！");
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册 TunnelHiveSpawner.Spawn 补丁失败: {ex.Message}");
                Log.Error(ex.StackTrace);
            }

            try
            {
                // 获取目标类型和方法
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

                // 获取补丁方法（注意这里获取的是Transpiler方法）
                MethodInfo transpilerMethod = AccessTools.Method(
                    typeof(StorytellerUtility_DefaultThreatPointsNow_Patch),
                    nameof(StorytellerUtility_DefaultThreatPointsNow_Patch.Transpiler)
                );

                if (transpilerMethod == null)
                {
                    Log.Error($"[EliteRaid] 找不到 StorytellerUtility_DefaultThreatPointsNow_Patch.Transpiler 方法！");
                    return;
                }

                // 创建Harmony补丁
                var transpiler = new HarmonyMethod(transpilerMethod);

                // 应用补丁（注意这里使用transpiler参数）
                harmony.Patch(
                    original: targetMethod,
                    transpiler: transpiler
                );

             //   Log.Message($"[EliteRaid] 成功为 StorytellerUtility.DefaultThreatPointsNow 添加 Transpiler 补丁！");
            } catch (Exception ex)
            {
                Log.Error($"[EliteRaid] 注册 StorytellerUtility.DefaultThreatPointsNow 补丁失败: {ex.Message}");
                Log.Error(ex.StackTrace);
            }

            ////设定最大袭击点数
            //try
            //{
            //    // 获取目标方法
            //    MethodInfo targetMethod = AccessTools.Method(
            //        typeof(StorytellerUtility),
            //        "DefaultThreatPointsNow",
            //        new[] { typeof(IIncidentTarget) } // 明确指定参数类型
            //    );

            //    if (targetMethod == null)
            //    {
            //        Log.Error("[EliteRaid] 找不到StorytellerUtility.DefaultThreatPointsNow方法！");
            //        return;
            //    }
            //    var postfix = new HarmonyMethod(typeof(StorytellerUtility_Patch), nameof(StorytellerUtility_Patch.Postfix));
            //    postfix.before = new string[] { "unlimitedthreatscale.1trickpwnyta", "brok.nolimitraidpoint" }; // 确保在其他补丁之前执行

            //    // 应用Postfix补丁
            //    harmony.Patch(
            //        original: targetMethod,
            //        postfix
            //    );

            //    //   Log.Message("[EliteRaid] 成功注册StorytellerUtility.DefaultThreatPointsNow的Postfix补丁！");
            //} catch (Exception ex)
            //{
            //    Log.Error($"[EliteRaid] 注册DefaultThreatPointsNow补丁失败: {ex}");
            //}

            //try
            //{
            //    Type targetClass = typeof(ActiveDropPod);
            //    MethodInfo targetMethod = AccessTools.Method(
            //        targetClass,
            //        "PodOpen", // 私有方法名
            //        Type.EmptyTypes
            //    );

            //    if (targetMethod == null)
            //    {
            //        Log.Error($"[EliteRaid] 找不到 ActiveDropPod.PodOpen 方法！");
            //        return;
            //    }

            //    // 定义 Transpiler 补丁方法（IL 指令修改）
            //    var transpiler = new HarmonyMethod(
            //        typeof(ActiveDropPod_PodOpen_Patch),
            //        nameof(ActiveDropPod_PodOpen_Patch.Transpiler) // 指向 Transpiler 方法
            //    );

            //    harmony.Patch(
            //        original: targetMethod,
            //        transpiler: transpiler // 通过 transpiler 参数注入 IL 修改逻辑
            //    );

            //   // Log.Message($"[EliteRaid] 成功为 ActiveDropPod.PodOpen 添加 Transpiler 补丁！");
            //} catch (Exception ex)
            //{
            //    Log.Error($"[EliteRaid] 注册补丁失败: {ex.Message}");
            //    Log.Error(ex.StackTrace);
            //}
            //try
            //{
            //    // --------------------------
            //    // 目标方法：DropPodUtility.DropThingGroupsNear
            //    // --------------------------
            //    Type targetClass = typeof(DropPodUtility);
            //    MethodInfo targetMethod = AccessTools.Method(
            //        targetClass,
            //        nameof(DropPodUtility.DropThingGroupsNear),
            //        new Type[] {
            //    typeof(IntVec3),
            //    typeof(Map),
            //    typeof(List<List<Thing>>),
            //    typeof(int),
            //    typeof(bool),
            //    typeof(bool),
            //    typeof(bool),
            //    typeof(bool),
            //    typeof(bool),
            //    typeof(bool),
            //    typeof(Faction)
            //        }
            //    );

            //    if (targetMethod == null)
            //    {
            //        Log.Error($"[EliteRaid] 找不到 {nameof(DropPodUtility.DropThingGroupsNear)} 方法！");
            //        return;
            //    }

            //    // --------------------------
            //    // 定义补丁方法（Postfix）
            //    // --------------------------
            //    var postfix = new HarmonyMethod(
            //        typeof(DropPodUtility_Patch),
            //        nameof(DropPodUtility_Patch.DropThingGroupsNear_Postfix)
            //    );

            //    // 设置补丁执行顺序（可选：确保在其他补丁之后执行）
            //    postfix.after = new[] { "other.mod.id.patch" };

            //    // --------------------------
            //    // 应用 Postfix 补丁
            //    // --------------------------
            //    harmony.Patch(
            //        original: targetMethod,
            //        postfix: postfix
            //    );

            //  //  Log.Message($"[EliteRaid] 成功为 {nameof(DropPodUtility.DropThingGroupsNear)} 添加 Postfix 补丁！");
            //} catch (Exception ex)
            //{
            //    Log.Error($"[EliteRaid] 注册 DropThingGroupsNear 补丁失败: {ex.Message}");
            //    Log.Error(ex.StackTrace);
            //}
       

            //GeneratePawns
            Type orgType = typeof(PawnGroupMakerUtility);
            string orgName = nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints);
            MethodInfo method = null;
          //  Log.Message("General.m_CanTranspilerGeneratePawns的值是" + General.m_CanTranspilerGeneratePawns);
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
                } catch (Exception ex)
                {
                    General.SendLog_Debug(General.MessageTypes.DebugError, String.Format("[{0}.{1}] Patch Failed!! reason:{2}{3}", orgType.FullName, orgName, Environment.NewLine, ex.ToString()));
                }
            }
          //  Log.Message("准备遍历目标方法,目标方法数量是"+targetMethods?.Count);
            foreach (TargetMethod targetMethod in targetMethods)
            {
                Type workerType = targetMethod.type;
                method = targetMethod.method;
#if DEBUG
              //  Log.Message($"@@@TargetMethodInfo: workerType={workerType}, method={method.Name}");
#endif
            //    Log.Message("目标方法确实有内容");
                try
                {
                    harmony.Patch(method,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(PawnGroupKindWorker_Patch), nameof(PawnGroupKindWorker_Patch.GeneratePawns_Finalizer), new Type[] { typeof(Exception), typeof(PawnGroupMakerParms), typeof(List<Pawn>) }) { methodType = MethodType.Normal });
                  //  Log.Message("执行了人类袭击的GeneratePawns_Finalizer中Patch方法");
                    if (General.m_CanTranspilerGeneratePawns)
                    {
                        harmony.Patch(method,
                            null,
                            null,
                            new HarmonyMethod(typeof(PawnGroupKindWorker_Patch), nameof(PawnGroupKindWorker_Patch.GeneratePawns_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>), typeof(MethodBase) }) { methodType = MethodType.Normal });
                        General.SendLog_Debug(General.MessageTypes.Debug, String.Format("[{0}] Transpiler patched!!", targetMethod));
                    }
                    General.m_AllowPawnGroupKindWorkerTypes.Add(workerType);
                } catch (Exception ex)
                {
                    General.SendLog_Debug(General.MessageTypes.DebugError, String.Format("[{0}] Patch Failed!! reason:{1}{2}", targetMethod, Environment.NewLine, ex.ToString()));
                }
            }

            //GenerateAnimals
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
                  //  Log.Message("GenerateAnimals_Transpiler和GenerateAnimals_Finalizer成功注册!");
                } catch (Exception ex)
                {
                  //  Log.Message("GenerateAnimals_Transpiler和GenerateAnimals_Finalizer注册失败!");
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
                typeof(PlanetTile),  // 修正为PlanetTile
                typeof(float),
                typeof(int)
              }
          )
          { methodType = MethodType.Normal }
      );

                 //   Log.Message("GenerateAnimals_Prefix成功注册!");
                } catch (Exception ex)
                {
                  //  Log.Message("GenerateAnimals_Prefix注册失败!");
                }
            }

            // 新增：EntitySwarm 补丁
            // 新增：EntitySwarm 补丁（使用 Transpiler）
            Type entitySwarmType = typeof(IncidentWorker_EntitySwarm);
            MethodInfo generateEntitiesMethod = AccessTools.Method(
          typeof(IncidentWorker_EntitySwarm),
          "GenerateEntities",  // 直接使用方法名，而非 nameof
          new Type[] { typeof(IncidentParms), typeof(float) }
              );

            if (General.m_CanTranspilerEntitySwarm)  // 新增配置项
            {
                try
                {
                    // 应用 Transpiler 补丁
                    harmony.Patch(
                        generateEntitiesMethod,
                        null,
                        null,
                        new HarmonyMethod(typeof(EntitySwarmIncidentUtility_Patch), nameof(EntitySwarmIncidentUtility_Patch.GenerateEntities_Transpiler), new Type[] { typeof(IEnumerable<CodeInstruction>) }) { methodType = MethodType.Normal }
                    );

                    // 应用 Finalizer 补丁
                    harmony.Patch(
                        generateEntitiesMethod,
                        null,
                        null,
                        null,
                        new HarmonyMethod(typeof(EntitySwarmIncidentUtility_Patch), nameof(EntitySwarmIncidentUtility_Patch.GenerateEntities_Finalizer),
                            new Type[] { typeof(Exception), typeof(List<Pawn>).MakeByRefType(), typeof(IncidentParms), typeof(float) })
                        { methodType = MethodType.Normal }
                    );

                    General.SendLog_Debug(General.MessageTypes.Debug,
                        $"[IncidentWorker_EntitySwarm.GenerateEntities] Transpiler + Finalizer patched!!");
                } catch (Exception ex)
                {
                    General.SendLog_Debug(General.MessageTypes.DebugError,
                        $"[IncidentWorker_EntitySwarm.GenerateEntities] Patch Failed!! reason:{Environment.NewLine}{ex.ToString()}");
                }
            }
        }

        private static void TranspilerTest(Harmony harmony, HashSet<TargetMethod> targetMethods)
        {
          //  Log.Message("TranspilerTest执行");
            //GeneratePawns
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
          //  Log.Message("TranspilerTest里现在的targetMethods数量是"+ targetMethods.Count);
            //GenerateAnimals
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
            //打个日志
           // Log.Message(String.Format("人类袭击的GeneratePawns_Finalizer执行了"));
            if (parms == null)
            {
                Log.Error("[EliteRaid] PawnGroupMakerParms is null in GeneratePawns_Finalizer");
                return __exception;
            }
            if (__exception == null)
            {
             //   Log.Message(String.Format("准备进入GeneratePawns_Impl"));
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
            Log.Message("[EliteRaid] GenerateAnimals_Prefix 被调用！");
            if (EliteRaidMod.modEnabled || !EliteRaidMod.allowAnimalsValue)
            {
                return true;
            }

            List<Pawn> list = new List<Pawn>();
            int baseNum = (animalCount > 0) ? animalCount : AggressiveAnimalIncidentUtility.GetAnimalsCount(animalKind, points);
            int maxPawnNum = General.GetenhancePawnNumber(baseNum);

            // 新增：使用PatchContinuityHelper存储原始参数
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

            // 调用统一的处理方法
            General.GenerateAnimals_Impl(animalKind, list);

            // 直接返回生成的结果，跳过原始方法
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

        // Finalizer：处理生成结果
        internal static Exception GenerateEntities_Finalizer(Exception __exception, ref List<Pawn> __result, IncidentParms parms, float points)
        {
            if (__exception == null && __result != null)
            {
                // 复用动物生成的处理逻辑（简化版）
                
                int baseNum = (int)points;
                int maxPawnNum = General.GetenhancePawnNumber(baseNum);

                if (maxPawnNum < baseNum && EliteRaidMod.allowEntitySwarmValue)
                {
                    bool allowedCompress = true;
                    // 检查生物类型限制（如机械族、昆虫族）
                    if (!EliteRaidMod.allowMechanoidsValue && __result.Any(p => p.RaceProps.IsMechanoid))
                        allowedCompress = false;
                    if (!EliteRaidMod.allowInsectoidsValue && __result.Any(p => p.RaceProps.FleshType == FleshTypeDefOf.Insectoid))
                        allowedCompress = false;

                    if (allowedCompress)
                    {
                        // 压缩数量并增强
                        __result = __result.Take(maxPawnNum).ToList();
                        General.GenerateEntitys_Impl(__result, baseNum, maxPawnNum); // 复用通用增强方法

                        // 显示消息
                        if(EliteRaidMod.displayMessageValue)
                        Messages.Message("袭击信息：压缩了" + baseNum + "为" + maxPawnNum, MessageTypeDefOf.NeutralEvent);
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

            if (!EliteRaidMod.AllowCompress(parms))
            {
                return true; // 不允许压缩，让原始方法处理
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
                                 //       Log.Message($"[EliteRaid] 虫族已增强: {pawn.LabelCap} → Level {eliteLevel.Level}");
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

                        if (enhancedCount > 0)
                        {
                            int finalNum = General.GetenhancePawnNumber(baseNum);
                            Messages.Message(String.Format("CR_RaidCompressedMassageEnhanced".Translate(), baseNum
                            , finalNum, General.GetcompressionRatio(baseNum, maxPawnNum)
                            , finalNum), MessageTypeDefOf.NeutralEvent, true);
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

            // 新增：尝试次数超限时警告并用当前结果
            if (attempts >= maxAttempts)
            {
                Log.Warning($"[EliteRaid] 生成敌人达到最大尝试次数({maxAttempts})，实际生成{list.Count}，期望{parms.pawnCount}。可能有pawnKind/faction配置问题。");
                if (list.Count == 0)
                {
                    Messages.Message("EliteRaid: 敌人生成失败，未能生成任何单位！", MessageTypeDefOf.NegativeEvent, true);
                }
                else
                {
                    Messages.Message($"EliteRaid: 只生成了{list.Count}个敌人（期望{parms.pawnCount}），请检查mod兼容性或pawnKind配置。", MessageTypeDefOf.NegativeEvent, true);
                }
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
    [HarmonyPatch(typeof(TunnelHiveSpawner))]
    class TunnelHiveSpawner_Spawn_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Spawn")]
        [HarmonyPatch(new Type[] { typeof(Map), typeof(IntVec3) })]
        public static bool Spawn_Prefix(TunnelHiveSpawner __instance, Map map, IntVec3 loc)
        {
            Log.Message("[EliteRaid] 虫族生成补丁已加载"); // 验证补丁是否被加载
            if (!EliteRaidMod.allowInsectoidsValue || EliteRaidMod.modEnabled)
            {
              //  Log.Message($"[EliteRaid] 虫族压缩已禁用 (allowInsectoidsValue={EliteRaidMod.allowInsectoidsValue}, modEnabled={EliteRaidMod.modEnabled})");
                return true;
            }

            if (__instance.spawnHive)
            {
                Hive hive = (Hive)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Hive, null), loc, map, WipeMode.Vanish);
                hive.SetFaction(Faction.OfInsects, null);
                hive.questTags = __instance.questTags;
                foreach (CompSpawner compSpawner in hive.GetComps<CompSpawner>())
                {
                    if (compSpawner.PropsSpawner.thingToSpawn == ThingDefOf.InsectJelly)
                    {
                        compSpawner.TryDoSpawn();
                        break;
                    }
                }
            }

            if (__instance.insectsPoints > 0f)
            {
              //  Log.Message($"[EliteRaid] 开始处理虫族生成 (points={__instance.insectsPoints})");

                __instance.insectsPoints = Mathf.Max(__instance.insectsPoints, Hive.spawnablePawnKinds.Min((PawnKindDef x) => x.combatPower));
                float pointsLeft = __instance.insectsPoints;
                int num = 0;

                // 计算基础数量
                Func<PawnKindDef, bool> func = null;
                while (pointsLeft > 0f)
                {
                    num++;
                    if (num > 1000)
                    {
                     //   Log.Error("[EliteRaid] 虫族生成迭代次数过多");
                        break;
                    }
                    IEnumerable<PawnKindDef> spawnablePawnKinds = Hive.spawnablePawnKinds;
                    Func<PawnKindDef, bool> predicate;
                    if ((predicate = func) == null)
                    {
                        predicate = (func = ((PawnKindDef x) => x.combatPower <= pointsLeft));
                    }
                    PawnKindDef pawnKindDef;
                    if (!spawnablePawnKinds.Where(predicate).TryRandomElement(out pawnKindDef))
                    {
                        num--;
                        break;
                    }
                    pointsLeft -= pawnKindDef.combatPower;
                }

                int baseNum = num;
                int maxPawnNum = General.GetenhancePawnNumber(baseNum);

              //  Log.Message($"[EliteRaid] 虫族基础数量计算完成: {baseNum}");
             //   Log.Message($"[EliteRaid] 最大允许数量: {maxPawnNum}");

                if (maxPawnNum >= baseNum)
                {
                  //  Log.Message($"[EliteRaid] 虫族数量不需要压缩 ({baseNum} ≤ {maxPawnNum})，使用原始逻辑");
                    return true;
                }

                // 生成压缩后的pawns列表
                List<Pawn> list = new List<Pawn>();
                EliteLevelManager.GenerateLevelDistribution(baseNum);
                int order = PowerupUtility.GetNewOrder();

             //   Log.Message($"[EliteRaid] 开始生成压缩后的虫族: {maxPawnNum} 只");

                for (int i = 0; i < maxPawnNum; i++)
                {
                    PawnKindDef pawnKindDef;
                    if (Hive.spawnablePawnKinds.Where(p => p.combatPower <= pointsLeft).TryRandomElement(out pawnKindDef))
                    {
                        PawnGenerationRequest request = new PawnGenerationRequest(
                      kind: pawnKindDef,
                      faction: Faction.OfInsects,
                      context: PawnGenerationContext.NonPlayer,
                      tile: map.Tile,
                      forceGenerateNewPawn: false,
                      allowDead: false,
                      allowDowned: false,
                      canGeneratePawnRelations: false, // 虫族不需要关系
                      mustBeCapableOfViolence: true,
                      colonistRelationChanceFactor: 0f, // 虫族不是殖民者
                      forceAddFreeWarmLayerIfNeeded: false,
                      allowGay: false, // 虫族无性繁殖
                      allowPregnant: false,
                      allowFood: true,
                      allowAddictions: false,
                      inhabitant: false,
                      certainlyBeenInCryptosleep: false,
                      forceRedressWorldPawnIfFormerColonist: false,
                      worldPawnFactionDoesntMatter: false,
                      biocodeWeaponChance: 0f, // 虫族没有生物编码武器
                      biocodeApparelChance: 0f, // 虫族没有生物编码服装
                      validatorPreGear: null,
                      validatorPostGear: null,
                      forcedTraits: null,
                      prohibitedTraits: null,
                      forceNoIdeo: true, // 虫族没有意识形态
                      forceNoBackstory: true, // 虫族没有背景故事
                      forbidAnyTitle: true, // 虫族没有头衔
                      forceDead: false,
                      forcedXenotype: null,
                      forceBaselinerChance: 0f,
                      forceRecruitable: false, // 虫族不可招募
                      dontGiveWeapon: false, // 让游戏为虫族分配武器
                      onlyUseForcedBackstories: false,
                      maximumAgeTraits: -1,
                      minimumAgeTraits: 0,
                      forceNoGear: false
                  );

                        Log.Message($"[EliteRaid][HarmonyPatches.cs@L1160] 调用PawnGenerator.GeneratePawn, pawnKind={request.KindDef?.defName}, faction={request.Faction?.Name ?? request.Faction?.ToString() ?? "null"}");
                        Pawn pawn = PawnGenerator.GeneratePawn(request);
                        Log.Message($"[EliteRaid][HarmonyPatches.cs@L1160] PawnGenerator.GeneratePawn结果: {(pawn == null ? "null" : pawn.LabelCap)}");
                        if (pawn != null)
                        {
                            list.Add(pawn);
                            pointsLeft -= pawnKindDef.combatPower;

                            // 应用增强
                            if (EliteRaidMod.AllowCompress(pawn))
                            {
                                EliteLevel eliteLevel = EliteLevelManager.GetRandomEliteLevel();
                                Hediff powerup = PowerupUtility.SetPowerupHediff(pawn, order);
                                if (powerup != null)
                                {
                                    bool powerupEnable = PowerupUtility.TrySetStatModifierToHediff(powerup, eliteLevel);
                                    if (powerupEnable)
                                    {
                                 //       Log.Message($"[EliteRaid] 虫族已增强: {pawn.LabelCap} → Level {eliteLevel.Level}");
                                    }
                                }
                            }
                        }
                    }
                }

                // 处理生成的pawns
                if (list.Any())
                {
                  //  Log.Message($"[EliteRaid] 虫族生成完成: {list.Count} 只 (原始: {baseNum}只)");

                    foreach (Pawn pawn in list)
                    {
                        GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(loc, map, 2, null), map, WipeMode.Vanish);
                        pawn.mindState.spawnedByInfestationThingComp = __instance.spawnedByInfestationThingComp;
                    }

                    LordMaker.MakeNewLord(Faction.OfInsects, new LordJob_AssaultColony(Faction.OfInsects, true, false, false, false, true, false, false), map, list);

                    // 显示压缩消息
                    if (EliteRaidMod.displayMessageValue)
                    {
                    //    Messages.Message($"[EliteRaid] 污染虫灾已压缩: {baseNum} → {list.Count}", MessageTypeDefOf.NeutralEvent);
                    }

                    return false; // 阻止原始方法执行
                } else
                {
                    Log.Warning($"[EliteRaid] 虫族生成失败: 生成了0只昆虫");
                }
            }

            return false; // 如果没有生成任何pawn，也阻止原始方法执行
        }
    }
    #endregion
    [HarmonyPatch(typeof(StorytellerUtility), "DefaultThreatPointsNow")]
    public static class StorytellerUtility_DefaultThreatPointsNow_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // 找到最后一个 ldc.r4 10000f 指令（袭击点数上限）
            int index = codes.FindLastIndex(
                c => c.opcode == OpCodes.Ldc_R4 &&
                c.operand.ToString() == "10000");

            if (index >= 0)
            {
                // 直接将 10000f 修改为 1000000f
                codes[index].operand = 10000000f;
             //   Log.Message($"[EliteRaid] 成功将袭击点数上限修改为 10000000");
            } else
            {
               // Log.Warning($"[EliteRaid] 未找到袭击点数上限的硬编码值，可能游戏版本已变化");
            }

            return codes;
        }

    }
    [HarmonyPatch(typeof(StorytellerUtility), "DefaultThreatPointsNow")]
    public static class Patch_DefaultThreatPointsNow
    {
        public static void Postfix(ref float __result)
        {
          //  __result *= EliteRaidMod.raidScale;
            if (__result > EliteRaidMod.maxRaidPoint)
            {
                __result = EliteRaidMod.maxRaidPoint;
            }
        }
    }

    #endregion
}
