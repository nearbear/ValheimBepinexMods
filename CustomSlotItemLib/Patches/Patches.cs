using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static CustomSlotItemLib.CustomSlotManager;

namespace CustomSlotItemLib
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), "IsEquipable")]
        [HarmonyPostfix]
        static void IsEquipablePostfix(ref bool __result, ref ItemDrop.ItemData __instance)
        {
            __result = __result || IsCustomSlotItem(__instance);
        }

        [HarmonyPatch(typeof(Humanoid), "Awake")]
        [HarmonyPostfix]
        static void HumanoidEntryPostfix(ref Humanoid __instance)
        {
            customSlotItemData[__instance] = new Dictionary<string, ItemDrop.ItemData>();
        }

        [HarmonyPatch(typeof(Player), "Load")]
        [HarmonyPostfix]
        static void InventoryLoadPostfix(ref Player __instance)
        {
            foreach (ItemDrop.ItemData itemData in __instance.m_inventory.GetEquipedtems())
            {
                if (IsCustomSlotItem(itemData))
                {
                    string slotName = GetCustomSlotName(itemData);
                    SetSlotItem(__instance, slotName, itemData);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        [HarmonyPostfix]
        static void EquipItemPostfix(ref bool __result, ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            if (!__result)
            {
                return;
            }
            if (IsCustomSlotItem(item))
            {
                string slotName = GetCustomSlotName(item);

                if (IsSlotOccupied(__instance, slotName))
                {
                    
                    __instance.UnequipItem(GetSlotItem(__instance, slotName), triggerEquipEffects);
                }
                SetSlotItem(__instance, slotName, item);
                if (__instance.IsItemEquiped(item))
                {
                    item.m_equiped = true;
                }
                __instance.SetupEquipment();
                if (triggerEquipEffects)
                {
                    __instance.TriggerEquipEffect(item);
                }
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
        [HarmonyPostfix]
        static void UnequipItemPostfix(ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            if (item == null || !IsCustomSlotItem(item))
            {
                return;
            }
            string slotName = GetCustomSlotName(item);
            if (item == GetSlotItem(__instance, slotName))
            {
                SetSlotItem(__instance, slotName, null);
            }
            __instance.UpdateEquipmentStatusEffects();
        }

        [HarmonyPatch(typeof(Humanoid), "IsItemEquiped")]
        [HarmonyPostfix]
        static void IsItemEquipedPostfix(ref bool __result, ref Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item == null || !IsCustomSlotItem(item))
            {
                return;
            }
            string slotName = GetCustomSlotName(item);
            bool isEquipped = DoesSlotExist(__instance, slotName) && GetSlotItem(__instance, slotName) == item;
            __result = __result || isEquipped;
        }

        [HarmonyPatch(typeof(Humanoid), "GetEquipmentWeight")]
        [HarmonyPostfix]
        static void GetEquipmentWeightPostfix(ref float __result, ref Humanoid __instance)
        {
            foreach (string slotName in customSlotItemData[__instance].Keys)
            {
                if (IsSlotOccupied(__instance, slotName))
                {
                    __result += GetSlotItem(__instance, slotName).m_shared.m_weight;
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "UnequipAllItems")]
        [HarmonyPostfix]
        static void UnequipAllItemsPostfix(ref Humanoid __instance)
        {
            foreach (string slotName in customSlotItemData[__instance].Keys.ToList())
            {
                if (IsSlotOccupied(__instance, slotName))
                {
                    __instance.UnequipItem(GetSlotItem(__instance, slotName), false);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "GetSetCount")]
        [HarmonyPostfix]
        static void GetSetCountPostfix(ref int __result, ref Humanoid __instance, string setName)
        {
            foreach (string slotName in customSlotItemData[__instance].Keys)
            {
                if (IsSlotOccupied(__instance, slotName) && GetSlotItem(__instance, slotName).m_shared.m_setName == setName)
                {
                    __result++;
                }
            }
        }

        public static HashSet<StatusEffect> GetStatusEffectsFromCustomSlotItems(Humanoid __instance)
        {
            HashSet<StatusEffect> hashSet = new HashSet<StatusEffect>();
            foreach (string slotName in customSlotItemData[__instance].Keys)
            {
                if (IsSlotOccupied(__instance, slotName))
                {
                    if (GetSlotItem(__instance, slotName).m_shared.m_equipStatusEffect)
                    {
                        StatusEffect statusEffect = GetSlotItem(__instance, slotName).m_shared.m_equipStatusEffect;
                        hashSet.Add(statusEffect);
                    }

                    if (__instance.HaveSetEffect(GetSlotItem(__instance, slotName)))
                    {
                        StatusEffect statusEffect = GetSlotItem(__instance, slotName).m_shared.m_setStatusEffect;
                        hashSet.Add(statusEffect);
                    }
                }
            }
            return hashSet;
        }

        [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UpdateEquipmentStatusEffectsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            // Sanity check the current assembly
            if (codes[0].opcode != OpCodes.Newobj || codes[1].opcode != OpCodes.Stloc_0)
            {
                throw new System.Exception("CustomSlotItemLib Transpiler injection point NOT found!! Game has most likely updated and broken this mod!");
            }

            yield return codes[0];
            yield return codes[1];

            // We are trying to Union the result of the GetStatusEffectsFromCustomSlotItems function with the freshly created union
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return CodeInstruction.Call(typeof(Patches), nameof(Patches.GetStatusEffectsFromCustomSlotItems));
            yield return CodeInstruction.Call(typeof(HashSet<StatusEffect>), nameof(HashSet<StatusEffect>.UnionWith));

            for (int i = 2; i < codes.Count; i++)
            {
                CodeInstruction instruction = codes[i];
                yield return instruction;
            }
        }
    }
}
