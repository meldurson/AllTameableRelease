
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
        /*
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
        */

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Procreation), "ResetPregnancy")]

        private static void PostfixResetPregnancy(Procreation __instance) //makes sure that offspring is still valid
        {
            if (Plugin.UseCLLC)
            {
                if (__instance.gameObject.GetComponent<ProcreationInfo>() == null)
                {
                    __instance.gameObject.AddComponent<ProcreationInfo>();
                }
            }
        }

        */

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
                    DBG.blogDebug("level is " +level);
                //level = 0;
                    //DBG.blogDebug("Found ProcInfo");
                }
                catch 
                {

                }

            }
        //}

        /*
        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        public static class Patch_Procreation_Procreate
        {
            private static void Prefix(Procreation __instance)
            {
                if (__instance.IsDue())
                {
                    // Set up and change the prefab here
                    string prefabName = Utils.GetPrefabName(__instance.m_offspring);
                    __instance.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
                    // Insert changes to __instance.m_offspringPrefab

                    // Code that will get skipped, need to run it
                    int prefab = __instance.m_nview.GetZDO().GetPrefab();
                    __instance.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
                }
            }
        }

        [HarmonyPatch(typeof(Procreation), nameof(Procreation.ResetPregnancy))]
        public static class Patch_Procreation_ResetPregnancy
        {
            private static void Postfix(Procreation __instance)
            {
                // Reset the original prefab
                __instance.m_offspringPrefab = null;
            }
        }

        */









        //[HarmonyEmitIL("./dumps")]
        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        public static class InterceptProcreation
        {
            private static GameObject OnProcreation(GameObject child, Procreation procreation)
            {

                if (!Plugin.UseCLLC)
                {
                    return child;
                }

                Character childchar = child.GetComponent<Character>();
                Character motherRef = procreation.gameObject.GetComponent<Character>();
                if (!(bool)childchar)
                {
                    DBG.blogWarning("Procreation, No Child");
                    return child;
                }
                else if (!(bool)motherRef)
                {
                    DBG.blogWarning("No Parent, setting tame and level manually");
                    childchar.SetTamed(true);
                    childchar.SetLevel(1);
                    return child;
                }
                try
                {
                    if (!child.gameObject.TryGetComponent<ProcreationInfo>(out var childProc))
                    {
                        //DBG.blogDebug("Adding Procinfo");
                        childProc = child.gameObject.AddComponent<ProcreationInfo>();
                    }
                    DBG.blogDebug("Attempting Custom Procreation");
                    childProc.SetCreature(motherRef);
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

        //[HarmonyEmitIL("./dumps")]
        [HarmonyPatch(typeof(Growup), nameof(Growup.GrowUpdate))]
        public static class InterceptGrowup
        {

            private static GameObject OnGrowup(GameObject child, Growup growup)
            {
                DBG.blogDebug("inOnGrowup");
                DBG.blogDebug("child="+ child.name);
                if (!Plugin.UseCLLC)
                {
                    return child;
                }
                Character childchar = child.GetComponent<Character>();
                Character growchar = growup.gameObject.GetComponent<Character>();
                if (!(bool)childchar)
                {
                    DBG.blogWarning("Growup, No Child");
                    return child;
                }
                else if (!(bool)growchar)
                {
                    DBG.blogWarning("No Growup, setting tame and level manually");
                    childchar.SetTamed(true);
                    childchar.SetLevel(1);
                    return child;
                }

                //DBG.blogDebug("Both child and growup valid, inheriting tame and level");
                childchar.SetTamed(growchar.IsTamed());
                childchar.SetLevel(growchar.GetLevel());
                try
                {
                    DBG.blogDebug("Attempting custom Growup");

                    ProcreationInfo procinfo = childchar.gameObject.AddComponent<ProcreationInfo>();
                    //throw new Exception("Try to duplicate");
                    procinfo.SetGrow(growchar);
                }
                catch
                {
                    DBG.blogWarning("Failed Custom Growup");
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
