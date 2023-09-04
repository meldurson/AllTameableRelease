
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using CreatureLevelControl;

namespace AllTameable.CLLC
{
    internal class CLLCPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetLevel")]
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


        [HarmonyPrefix]
        [HarmonyPatch(typeof(API), "SetExtraEffectCreature", new Type[] { typeof(Character), typeof(CreatureExtraEffect) })]
        private static bool Prefix_API_SetExtraEffect(Character character, CreatureExtraEffect effect) 
        {
            return SetCLLCData(character, "ExtraEffect", effect.ToString());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(API), "SetInfusionCreature", new Type[] { typeof(Character), typeof(CreatureInfusion) })]
        private static bool Prefix_API_SetInfusionCreature(Character character, CreatureInfusion infusion)
        {
            return SetCLLCData(character, "Infusion", infusion.ToString());
        }

        private static bool SetCLLCData(Character character,string key,string val)
        {
            //DBG.blogDebug("in "+key+ " Prefix");
            if ((bool)character.m_nview)
            {
                return true;
            }
            ItemDrop iDrop = character.gameObject.GetComponent<ItemDrop>();
            if (!(bool)iDrop)
            {
                DBG.blogDebug("No item Drop");
                return true;
            }
            Utils2.addOrUpdateCustomData(iDrop.m_itemData.m_customData, key, val);
            DBG.blogDebug("Added " + key + " to ItemDrop, skipping set creature with " + key + ": " + val);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EggGrow), "Start")]
        private static void EggGrow_Start_Postfix(EggGrow __instance)
        {
            DBG.blogDebug("in egg Start");
            setstack(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EggGrow), "GetHoverText")]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void EggGrow_GetHoverText_Postfix(EggGrow __instance)
        {
            //DBG.blogDebug("in egg HoverText");
            setstack(__instance);
        }

        private static void setstack(EggGrow instance)
        {
            ItemDrop iDrop = instance.GetComponent<ItemDrop>();
            if ((bool)iDrop)
            {
                ItemDrop.ItemData iData = iDrop.m_itemData;
                if (!iData.m_customData.TryGetValue("Infusion", out string infStr))
                {
                    infStr = "None";
                }
                if (!iData.m_customData.TryGetValue("ExtraEffect", out string effStr))
                {
                    effStr = "None";
                }
                //DBG.blogDebug("infusion=" + infStr + ", effect=" + effStr);
                if (infStr != "None" || effStr != "None")
                {
                    //DBG.blogDebug("Setting max stack 1");
                    iData.m_shared.m_maxStackSize = iData.m_stack;
                }
                else
                {
                    iData.m_shared.m_maxStackSize = Math.Max(iData.m_stack, 20);
                }
                

            }
        }




        //[HarmonyEmitIL("./dumps")]
        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        public static class InterceptProcreation
        {
            private static GameObject OnProcreation(GameObject child, Procreation procreation)
            {
                DBG.blogDebug("in OnProcreation");
                if (!Plugin.UseCLLC)
                {
                    return child;
                }

                Character childchar = child.GetComponent<Character>();
                Character motherRef = procreation.gameObject.GetComponent<Character>();
                EggGrow childEggGrow = child.GetComponent<EggGrow>();
                if (!(bool)childchar && !(bool)childEggGrow)
                {
                    DBG.blogWarning("Procreation, No Child");
                    return child;
                }
                else if (!(bool)motherRef)
                {
                    DBG.blogWarning("No Parent, setting tame and level manually");
                    if ((bool)childchar)
                    {
                        childchar.SetTamed(true);
                        childchar.SetLevel(1);
                    }
                    if ((bool)childEggGrow)
                    {
                        child.GetComponent<ItemDrop>().SetQuality(1);
                        
                    }
                    return child;
                }

                //try
                //{
                    if (!child.gameObject.TryGetComponent<ProcreationInfo>(out var childProc))
                    {
                        DBG.blogDebug("Adding Procinfo");
                        childProc = child.gameObject.AddComponent<ProcreationInfo>();
                    }
                DBG.blogDebug("in onProcreate");
                //DBG.blogDebug("has dropPrefab=" + child.GetComponent<ItemDrop>().m_itemData.m_dropPrefab);
                //child.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = child;
               // DBG.blogDebug("has dropPrefab2=" + child.GetComponent<ItemDrop>().m_itemData.m_dropPrefab);
                DBG.blogDebug("Attempting Custom Procreation");
                    childProc.SetCreature(motherRef);
                //}
                //catch
                //{
                //    DBG.blogWarning("Failed Custom Procreation, using normal procreation as backup");
                //}

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
        [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GrowUpdate))]
        public static class InterceptEggGrowup
        {

            private static GameObject OnEggGrowup(GameObject adult, EggGrow growup)
            {
                //DBG.blogDebug("inOnGrowup");
                DBG.blogDebug("EggGrow adult=" + adult.name);
                if (!Plugin.UseCLLC)
                {
                    return adult;
                }
                Character adultchar = adult.GetComponent<Character>();
                ItemDrop iDrop = growup.GetComponent<ItemDrop>();
                if (!(bool)adultchar)
                {
                    DBG.blogWarning("Growup, No Adult");
                    return adult;
                }

                //DBG.blogDebug("Both child and growup valid, inheriting tame and level");
                adultchar.SetTamed(growup.m_tamed);
                adultchar.SetLevel(iDrop.m_itemData.m_quality);
                try
                {
                    DBG.blogDebug("Attempting custom Growup");
                    if (!growup.gameObject.TryGetComponent<ProcreationInfo>(out var procinfo))
                    {
                        DBG.blogDebug("Adding Procinfo to Growup");
                        procinfo = growup.gameObject.AddComponent<ProcreationInfo>();
                    }
                    //ProcreationInfo procinfo = adultchar.gameObject.AddComponent<ProcreationInfo>();
                    //throw new Exception("Try to duplicate");
                    procinfo.SetGrow(adultchar);
                }
                catch
                {
                    DBG.blogWarning("Failed Custom Egg Growup");
                }
                return adult;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo growupHook = AccessTools.DeclaredMethod(typeof(InterceptEggGrowup), nameof(OnEggGrowup));
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

        //[HarmonyEmitIL("./dumps")]
        [HarmonyPatch(typeof(Growup), nameof(Growup.GrowUpdate))]
        public static class InterceptGrowup
        {

            private static GameObject OnGrowup(GameObject adult, Growup growup)
            {
                //DBG.blogDebug("inOnGrowup");
                DBG.blogDebug("adult="+ adult.name);
                if (!Plugin.UseCLLC)
                {
                    return adult;
                }
                Character adultchar = adult.GetComponent<Character>();
                Character growchar = growup.gameObject.GetComponent<Character>();
                if (!(bool)adultchar)
                {
                    DBG.blogWarning("Growup, No Adult");
                    return adult;
                }
                else if (!(bool)growchar)
                {
                    DBG.blogWarning("No Growup, setting tame and level manually");
                    adultchar.SetTamed(true);
                    adultchar.SetLevel(1);
                    return adult;
                }

                //DBG.blogDebug("Both child and growup valid, inheriting tame and level");
                adultchar.SetTamed(growchar.IsTamed());
                adultchar.SetLevel(growchar.GetLevel());
                try
                {
                    DBG.blogDebug("Attempting custom Growup");
                    if (!growchar.gameObject.TryGetComponent<ProcreationInfo>(out var procinfo))
                    {
                        //DBG.blogDebug("Adding Procinfo to Growup");
                        procinfo = growchar.gameObject.AddComponent<ProcreationInfo>();
                    }
                    //ProcreationInfo procinfo = adultchar.gameObject.AddComponent<ProcreationInfo>();
                    //throw new Exception("Try to duplicate");
                    procinfo.SetGrow(adultchar);
                }
                catch
                {
                    DBG.blogWarning("Failed Custom Growup");
                }
                return adult;
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
