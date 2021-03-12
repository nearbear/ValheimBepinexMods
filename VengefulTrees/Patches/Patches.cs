using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace VengefulTrees.Patches
{
    [HarmonyPatch]
    public static class Patches
    {
        // Removes the collision damage cap. No one should be safe.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ImpactEffect), nameof(ImpactEffect.OnCollisionEnter))]
        public static IEnumerable<CodeInstruction> OnCollisionEnterPostfix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool flag = false;
            
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction instruction = codes[i];

                if (instruction.opcode == OpCodes.Isinst && (Type)instruction.operand == typeof(Character))
                {
                    flag = true;
                }
                if (!flag) continue;

                if (instruction.Calls(AccessTools.Method(typeof(Utils), nameof(Utils.LerpStep), new Type[] { typeof(float), typeof(float), typeof(float) })))
                {
                    // Keep the ldloc and stloc so that it's basically num = num2 / 4
                    CodeInstruction ldloc = codes[i - 1];

                    codes[i - 5] = new CodeInstruction(OpCodes.Nop);
                    codes[i - 4] = new CodeInstruction(OpCodes.Nop);
                    codes[i - 3] = new CodeInstruction(OpCodes.Nop);
                    codes[i - 2] = ldloc;
                    codes[i - 1] = new CodeInstruction(OpCodes.Ldc_R4, 4f);
                    codes[i - 0] = new CodeInstruction(OpCodes.Div);
                }
            }
            return codes.AsEnumerable();
        }

        // Calls our static method with all the info it needs from local variables
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.SpawnLog))]
        public static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction>  codes = new List<CodeInstruction>(instructions);
            if (codes[9].opcode != OpCodes.Stloc_0)
            {
                throw new Exception("VengefulTrees Transpiler injection point NOT found!! Game has most likely updated and broken this mod!");
            }

            List<Label> retLabels = null;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction instruction = codes[i];
                if (instruction.opcode == OpCodes.Ret)
                {
                    // We need to save off this label and put them on our first instruction so that it doesn't get skipped
                    retLabels = instruction.labels;
                    break;
                }
                yield return instruction;
            }
            
            // Call our static function on the newly created log
            yield return new CodeInstruction(OpCodes.Ldloc_0)
            {
                labels = retLabels
            };
            yield return CodeInstruction.Call(typeof(VengefulTree), nameof(VengefulTree.CheckTreeHatred));
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
