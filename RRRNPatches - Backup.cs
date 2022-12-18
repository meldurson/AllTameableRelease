
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using BepInEx;

namespace AllTameable.RRRN
{
    internal class RRRNPatches
    {



        [HarmonyPatch(typeof(RRRCore.RRRMobCustomization), "SetupTameable")]
        private static class InterceptSetupTameable
        {
            private static GameObject OnSetupTameable(GameObject child, Procreation procreation)
            {
                return child;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                DBG.blogDebug("In Transpiler");
                MethodInfo procreationHook = AccessTools.DeclaredMethod(typeof(InterceptSetupTameable), nameof(OnSetupTameable));
                MethodInfo instantiator = typeof(UnityEngine.Object).GetMethods().First(m => m.Name == nameof(UnityEngine.Object.Instantiate) && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3) && m.GetParameters()[2].ParameterType == typeof(Quaternion) && m.ContainsGenericParameters).MakeGenericMethod(typeof(GameObject));
                MethodInfo addTameable = typeof(UnityEngine.GameObject).GetMethods().First(n => n.Name == nameof(UnityEngine.GameObject.AddComponent) && n.GetParameters().Length == 1);
                MethodInfo[] allmethods = typeof(SpawnSystem).GetMethods();
                MethodInfo tempmethod1;
                MethodInfo tempmethod2;
                var code = new List<CodeInstruction>(instructions);
                DBG.blogDebug("In Transpiler2");
                if (addTameable != null)
                {
                    DBG.blogDebug("Addtameable=:" + addTameable);
                    try
                    {
                        DBG.blogDebug("Addtameable2=:" + addTameable.GetParameters()[1].Name);
                    }
                    catch { }
                    try
                    {
                        DBG.blogDebug("Addtameable3=:" + addTameable.GetParameters().Length);
                    }
                    catch { }
                    try
                    {
                        DBG.blogDebug("Addtameable4=:" + addTameable.GetParameters()[1].ParameterType);
                    }
                    catch { }

                }
                tempmethod1 = allmethods[31];
                tempmethod2 = allmethods[32];
                int j = 0;
                foreach (MethodInfo method in allmethods)
                {
                    DBG.blogDebug("method " + j + ": " + method);
                    if (method.Name == nameof(UnityEngine.GameObject.TryGetComponent))
                    {
                        if (j == 31)
                        {
                            tempmethod1 = method;
                            DBG.blogDebug("setting method 1");
                        }
                        if (j == 32)
                        {
                            tempmethod2 = method;
                            DBG.blogDebug("setting method 2");
                        }
                        DBG.blogDebug("method " + j + ": " + method);
                        try
                        {
                            DBG.blogDebug("method a =:" + method.GetParameters()[0].Name);
                        }
                        catch { }
                        try
                        {
                            DBG.blogDebug("method b =:" + method.GetParameters().Length);
                        }
                        catch { }
                        try
                        {
                            DBG.blogDebug("method c =:" + method.GetParameters()[0].ParameterType);
                        }
                        catch { }
                        try
                        {
                            DBG.blogDebug("method d =:" + method.ContainsGenericParameters);
                        }
                        catch { }

                    }
                    j++;
                }
                //MethodInfo setlevel = typeof(Character).GetMethods().First(n => n.Name == nameof(Character.SetLevel));
                //MethodInfo setTamed = typeof(Character).GetMethods().First(n => n.Name == nameof(Character.SetTamed));
                int i = 0;
                /*
                foreach (CodeInstruction instruction in instructions)
                {
                    DBG.blogDebug("RRRN"+i+": "+instruction.operand);
                    try { DBG.blogDebug("RRRN(2)" + i + ": " + instruction.operand.GetType()); } catch { }
                    try { DBG.blogDebug("RRRN(3)" + i + ": " + instruction.operand.ToString()); } catch { }
                    try { DBG.blogDebug("RRRN(4)" + i + ": " + instruction.ToString()); } catch { }


                    //DBG.blogDebug("RRRN" + i + ":op(" + instruction.opcode + ") instruct:" + instruction + ": operand(" + instruction.operand);
                    //DBG.blogDebug(instruction.opcode);
                    if (instruction.OperandIs(tempmethod1))
                    {
                        DBG.blogDebug("method1 at " + i + ":" + instruction);
                    }
                    if (instruction.OperandIs(tempmethod2))
                    {
                        DBG.blogDebug("method2 at " + i + ":" + instruction);
                    }
                    if (instruction.opcode == OpCodes.Call && instruction.OperandIs(instantiator))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, procreationHook);
                    }
                    if (i > 0)
                    {
                        yield return instruction;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        //yield return new CodeInstruction(OpCodes.Call, Instance);
                    }
                    i++;

                }
                */
                DBG.blogDebug("Starting Code Find");
                int inx_insert = -1;
                Label postIfLabel = il.DefineLabel();
                CodeInstruction getCompCopy = new CodeInstruction(OpCodes.Ldarg_0);
                for (int a = 0; a < code.Count - 1; a++)
                {
                    try { DBG.blogDebug(a + ":" + code[a].operand.ToString()); } catch { }

                    try
                    {
                        if (code[a].operand.ToString().Contains("AddComponent[Tameable]"))
                        {
                            DBG.blogDebug("Found add at index " + a);
                            if (code[a - 1].operand.ToString().Contains("get_gameObject"))
                            {
                                DBG.blogDebug("Found assign before index " + a);
                                inx_insert = a - 1;
                                getCompCopy = new CodeInstruction(code[inx_insert]);
                                code[inx_insert+3].labels.Add(postIfLabel);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                MethodInfo tryGetMethod = typeof(UnityEngine.GameObject).GetMethods().First(m => m.Name == nameof(UnityEngine.GameObject.TryGetComponent) && m.GetParameters().Length == 1 &&  m.ContainsGenericParameters).MakeGenericMethod(typeof(Tameable));

                var instructToAdd = new List<CodeInstruction>();
                instructToAdd.Add(getCompCopy);
                instructToAdd.Add(new CodeInstruction(OpCodes.Ldloca_S, 0)); //ldloca.s 0
                instructToAdd.Add(new CodeInstruction(OpCodes.Callvirt, tryGetMethod));
                //instructToAdd.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnityEngine.GameObject), nameof(UnityEngine.GameObject.TryGetComponent), generics: new Type[] { typeof(Tameable) }).MakeGenericMethod(typeof(Tameable))));
                //instructToAdd.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.GameObject), "TryGetComponent")));
                instructToAdd.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                instructToAdd.Add(new CodeInstruction(OpCodes.Ceq));
                instructToAdd.Add(new CodeInstruction(OpCodes.Stloc_3));
                instructToAdd.Add(new CodeInstruction(OpCodes.Ldloc_3));
                instructToAdd.Add(new CodeInstruction(OpCodes.Brfalse_S, postIfLabel));
                instructToAdd.Add(new CodeInstruction(OpCodes.Ldarg_0));
                //instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)10));
                //instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.GameObject), "TryGetComponent", new Type[] { typeof(Tameable) })));
                //instructionsToInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, return566Label));
                //instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldc_I4, 183));
                //instructionsToInsert.Add(new CodeInstruction(OpCodes.Ret));

                if (inx_insert != -1)
                {
                    code.InsertRange(inx_insert, instructToAdd);
                }
                for (int a = 0; a < code.Count - 1; a++)
                {
                    try {DBG.blogDebug("Post:" + code[a]);} catch { }

                }
                return code;
                /*
                foreach (CodeInstruction instruction in instructions)
                {
                    DBG.blogDebug("Post:"+instruction);
                }
                */
            }
        }


    }
}
