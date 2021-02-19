using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace AntiSlipShoes.Patches
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Character), nameof(Character.GetSlideAngle))]
        public static IEnumerable<CodeInstruction> GetSlideAngleTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int codepoint = -1;

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < instructions.Count(); i++)
            {
                CodeInstruction currentCode = codes[i];
                if (currentCode.opcode == OpCodes.Ldc_R4 && (float)currentCode.operand < 90f)
                {
                    codepoint = i;
                    codes[i].operand = Main.slidingThreshold.Value;
                    break;
                }
            }

            if (codepoint == -1)
                throw new System.Exception("Anti Slip Shoes Transpiler injection point NOT found!! Game has most likely updated and broken this mod!");

            return codes.AsEnumerable();
        }
    }
}
