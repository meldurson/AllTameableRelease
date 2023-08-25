
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace AllTameable.RRRN
{
    internal class RRRNPatches
    {
        [HarmonyPatch(typeof(RRRCore.RRRMobCustomization), "SetupTameable")]
        private static class InterceptSetupTameable
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                int inx_insert = -1;
                Label postIfLabel = il.DefineLabel();
                CodeInstruction getCompCopy = new CodeInstruction(OpCodes.Ldarg_0);
                for (int i = 0; i < code.Count - 1; i++)
                {
                    try
                    {
                        if (code[i].operand.ToString().Contains("AddComponent[Tameable]"))
                        {
                            if (code[i - 1].operand.ToString().Contains("get_gameObject"))
                            {
                                inx_insert = i - 1;
                                getCompCopy = new CodeInstruction(code[inx_insert]);
                                code[inx_insert+3].labels.Add(postIfLabel);
                                i = code.Count;
                            }
                        }
                    } catch{}
                }
                if (inx_insert != -1)
                {
                    var instructToAdd = new List<CodeInstruction>();
                    instructToAdd.Add(getCompCopy);
                    instructToAdd.Add(new CodeInstruction(OpCodes.Ldloca_S, 0)); //ldloca.s 0
                    instructToAdd.Add(new CodeInstruction(OpCodes.Callvirt, typeof(UnityEngine.GameObject).GetMethods().First(m => m.Name == nameof(UnityEngine.GameObject.TryGetComponent) && m.GetParameters().Length == 1 && m.ContainsGenericParameters).MakeGenericMethod(typeof(Tameable))));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Ceq));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Stloc_3));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Ldloc_3));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Brfalse_S, postIfLabel));
                    instructToAdd.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    code.InsertRange(inx_insert, instructToAdd);
                }

                return code;
            }
        }


    }
}
