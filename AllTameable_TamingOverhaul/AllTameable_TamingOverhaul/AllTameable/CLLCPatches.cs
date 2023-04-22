
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AllTameable.CLLC
{
    internal class CLLCPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Procreation), "Procreate")]
        //private static class Prefix_Procreation_Procreate
        //{

            private static void Prefix(Procreation __instance)
            {
                if (Plugin.UseCustomProcreation.Value)
                {

                    if (__instance.IsPregnant())
                    {
                        if (Plugin.UseCLLC)
                        {
                            if (__instance.gameObject.GetComponent<ProcreationInfo>() == null)
                            {
                                __instance.gameObject.AddComponent<ProcreationInfo>();
                            }
                        }

                    }
                    else
                    {

                        if (__instance.gameObject.GetComponent<ProcreationInfo>())
                        {
                            //DBG.blogDebug("Procreation Complete, Removing Procreation Info");
                            UnityEngine.Object.DestroyImmediate(__instance.gameObject.GetComponent<ProcreationInfo>());
                        }

                    }
                }

            }
        //}
        //[HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetLevel")]
        //private static class Prefix_Character_SetLevel
        //{

            private static void Prefix(Character __instance, ref int level)
            {
                //DBG.blogDebug("in SetLevelPrefix");
                //int old_level;
                //old_level = level;
                try
                {
                    level = __instance.gameObject.GetComponent<ProcreationInfo>().GetLevel(level);
                    //DBG.blogDebug("Found ProcInfo");
                }
                catch 
                {

                }

            }
        //}

        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        public static class InterceptProcreation
        {
            private static GameObject OnProcreation(GameObject child, Procreation procreation)
            {
                //DBG.blogDebug("inOnprocreation");
                try
                {
                    //if (UnityEngine.Random.Range(0, 100) < 10)
                    //{
                    //    DBG.blogDebug("Throwing Error");
                    //    throw new NullReferenceException();
                    //}
                    
                    if (Plugin.UseCLLC)
                    {
                        //DBG.blogDebug("hasCLLC");
                        if (!child.gameObject.TryGetComponent<ProcreationInfo>(out var childProc))
                        {
                            //DBG.blogDebug("Adding Procinfo");
                            childProc = child.gameObject.AddComponent<ProcreationInfo>();
                        }
                        Character motherRef = procreation.gameObject.GetComponent<Character>();
                        if (motherRef != null)
                        {
                            //DBG.blogDebug("Mother not Null");
                            childProc.SetCreature(motherRef);
                        }
                        else
                        {
                            DBG.blogDebug("Mother is Null");
                        }


                    }
                }
                catch
                {
                    DBG.blogWarning("Failed Custom Procreation, using normal procreation as backup");
                }

                return child;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo procreationHook = AccessTools.DeclaredMethod(typeof(InterceptProcreation), nameof(OnProcreation));
                MethodInfo instantiator = typeof(UnityEngine.Object).GetMethods().First(m => m.Name == nameof(UnityEngine.Object.Instantiate) && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3) && m.GetParameters()[2].ParameterType == typeof(Quaternion) && m.ContainsGenericParameters).MakeGenericMethod(typeof(GameObject));
                
                //MethodInfo setlevel = typeof(Character).GetMethods().First(n => n.Name == nameof(Character.SetLevel));
                //MethodInfo setTamed = typeof(Character).GetMethods().First(n => n.Name == nameof(Character.SetTamed));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    //DBG.blogDebug("cllc " + instruction);
                    if (instruction.opcode == OpCodes.Call && instruction.OperandIs(instantiator))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, procreationHook);
                    }
                }
                foreach (CodeInstruction instruction in instructions)
                {
                    //DBG.blogDebug("cllc2 " + instruction);
                }
            }

        }

        [HarmonyPatch(typeof(Growup), nameof(Growup.GrowUpdate))]
        public static class InterceptGrowup
        {

            private static GameObject OnGrowup(GameObject child, Growup growup)
            {
                DBG.blogDebug("inOnGrowup");
                if (Plugin.UseCLLC)
                {
                    try
                    {
                        DBG.blogDebug("hasCLLC");
                        Character childchar = child.GetComponent<Character>();
                        Character growchar = growup.gameObject.GetComponent<Character>();
                        ProcreationInfo procinfo = childchar.gameObject.AddComponent<ProcreationInfo>();
                        //throw new Exception("Try to duplicate");
                        procinfo.SetGrow(growchar);
                    }
                    catch
                    {
                        DBG.blogWarning("Failed Custom Growup");
                    }
                }

                return child;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo growupHook = AccessTools.DeclaredMethod(typeof(InterceptGrowup), nameof(OnGrowup));
                MethodInfo instantiator = typeof(UnityEngine.Object).GetMethods().First(m => m.Name == nameof(UnityEngine.Object.Instantiate) && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3) && m.GetParameters()[2].ParameterType == typeof(Quaternion) && m.ContainsGenericParameters).MakeGenericMethod(typeof(GameObject));
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Call && instruction.OperandIs(instantiator))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, growupHook);
                    }
                }

            }
        }
    }
}
