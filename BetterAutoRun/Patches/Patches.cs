using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace BetterAutoRun
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        public static IEnumerable<CodeInstruction> SetControlsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundAutoRunCondition = false;
            bool foundMoveDirCondition = false;
            bool foundCancelAutorunCondition = false;
            bool foundMoveDirAssignment = false;
            CodeInstruction branchEndAutoRun = null;

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            //DumpIL(codes);
            for (int i = 0; i < codes.Count(); i++)
            {
                CodeInstruction code = codes[i];

                // We're looking for a ldfld of m_autoRun followed by a brfalse: "if(this.m_autoRun)"
                if (code.opcode == OpCodes.Ldfld 
                    && code.LoadsField(AccessTools.DeclaredField(typeof(Player), nameof(Player.m_autoRun)))
                    && codes[i + 1].opcode == OpCodes.Brfalse)
                {
                    //Debug.Log("foundit1 " + i);
                    foundAutoRunCondition = true;
                }

                // Add in our own autorun canceling conditions: if either forwards or backwards are pressed.
                if (foundAutoRunCondition
                    && codes[i - 4].opcode == OpCodes.Ldarg_S //&& codes[i - 4].operand as string == "10"
                    && codes[i - 3].opcode == OpCodes.Or
                    && codes[i - 2].opcode == OpCodes.Ldarg_S //&& codes[i - 2].operand as string == "6"
                    && codes[i - 1].opcode == OpCodes.Or
                    && codes[i + 0].Branches(out Label? asdfasdf))
                {
                    //Debug.Log("foundit2 " + i);
                    branchEndAutoRun = code;
                    codes.InsertRange(i + 1, new List<CodeInstruction>() {
                        new CodeInstruction(OpCodes.Ldstr, "Forward"),
                        CodeInstruction.Call(typeof(ZInput), nameof(ZInput.GetButton)),
                        branchEndAutoRun.Clone(),
                        new CodeInstruction(OpCodes.Ldstr, "Backward"),
                        CodeInstruction.Call(typeof(ZInput), nameof(ZInput.GetButton)),
                        branchEndAutoRun.Clone()
                    });
                    i += 7;
                }

                // Nop out the "movedir != Vector3.zero" condition
                if (foundAutoRunCondition
                    && codes[i + 0].opcode == OpCodes.Ldarg_1
                    && codes[i + 1].opcode == OpCodes.Call)
                {
                    //Debug.Log("foundit3 " + i);
                    codes[i + 0].opcode = OpCodes.Nop; codes[i + 0].operand = null;
                    codes[i + 1].opcode = OpCodes.Nop; codes[i + 1].operand = null;
                    codes[i + 2].opcode = OpCodes.Nop; codes[i + 2].operand = null;
                    codes[i + 3].opcode = OpCodes.Nop; codes[i + 3].operand = null;

                    foundCancelAutorunCondition = true;
                }

                // Convert "else if (autoRun || blockHold)" into "else"
                if (foundCancelAutorunCondition
                    && codes[i + 0].opcode == OpCodes.Ldarg_S //&& codes[i + 0].operand as string == "10"
                    && codes[i + 1].opcode == OpCodes.Ldarg_S //&& codes[i + 1].operand as string == "6"
                    && codes[i + 2].opcode == OpCodes.Or
                    && codes[i + 3].Branches(out Label? asdfasdfsad))
                {
                    //Debug.Log("foundit4 " + i);
                    codes[i + 0].opcode = OpCodes.Nop; codes[i + 0].operand = null;
                    codes[i + 1].opcode = OpCodes.Nop; codes[i + 1].operand = null;
                    codes[i + 2].opcode = OpCodes.Nop; codes[i + 2].operand = null;
                    codes[i + 3].opcode = OpCodes.Nop; codes[i + 3].operand = null;

                    foundMoveDirCondition = true;
                }

                // Lastly, add "movedir.x * Vector3.Cross(Vector3.up, this.m_lookDir)" to the player's movedir so that they can strafe while autorunning
                if (foundMoveDirCondition
                    && codes[i - 3].opcode == OpCodes.Ldarg_0
                    && codes[i - 2].opcode == OpCodes.Ldarg_0
                    && codes[i - 1].LoadsField(AccessTools.Field(typeof(Character), nameof(Character.m_lookDir)))
                    && codes[i - 0].StoresField(AccessTools.Field(typeof(Character), nameof(Character.m_moveDir))))
                {
                    //Debug.Log("foundit5 " + i);
                    codes.InsertRange(i, new List<CodeInstruction>() { //FIX THIS NEXT
                        new CodeInstruction(OpCodes.Ldarg_1),
                        CodeInstruction.LoadField(typeof(Vector3), nameof(Vector3.x)),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.up))),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        CodeInstruction.LoadField(typeof(Player), nameof(Player.m_lookDir)),
                        CodeInstruction.Call(typeof(Vector3), nameof(Vector3.Cross)),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", new Type[] { typeof(float), typeof(Vector3) })),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Addition", new Type[] { typeof(Vector3), typeof(Vector3) }))
                });

                    foundMoveDirAssignment = true;
                    break; // we done
                }
            }
            
            //DumpIL(codes);

            //Debug.Log(string.Format("{0} {1} {2} {3} {4}", foundAutoRunCondition, branchEndAutoRun != null, foundCancelAutorunCondition, foundMoveDirCondition, foundMoveDirAssignment));
            if (!foundAutoRunCondition || branchEndAutoRun == null || !foundCancelAutorunCondition || !foundMoveDirCondition || !foundMoveDirAssignment)
                throw new Exception("BetterAutoRun injection point NOT found!! Game has most likely updated and broken this mod!");
            
            return codes.AsEnumerable();
        }

        // This prefix handles auto sprinting
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        public static void SetControlsPrefix(ref Player __instance, ref bool run, ref bool autoRun)
        {
            Player player = __instance;

            // Ignore this function if not autorunning or the config is set to 1, which disables the function
            if (!player.m_autoRun || Mathf.Approximately(Main.sprintUntil.Value, 1f))
            {
                return;
            }
            // Toggle sprinting on if we just started autorunning
            if (autoRun)
            {
                player.m_run = true;
            }
            // Check if we're above our sprinting stamina threshold
            if (player.m_stamina > Main.sprintUntil.Value * player.m_maxStamina)
            {
                // Toggle sprinting on if we hit full stamina
                if (!player.m_run && Mathf.Approximately(player.m_stamina, player.m_maxStamina))
                {
                    player.m_run = true;
                }
                // Retain the sprinting state even if the key isn't being held
                if (player.m_run)
                {
                    run = true;
                }
            }
            // Otherwise, toggle running off to regenerate stamina until full again
            else
            {
                player.m_run = false;
            }
        }

        public static void DumpIL<T>(this IEnumerable<T> enumerable)
        {
            int i = 0;
            foreach (T item in enumerable)
                Debug.Log(string.Format("{0}: {1}", (i++).ToString().PadLeft(4, '0'), item));
        }
    }
}
