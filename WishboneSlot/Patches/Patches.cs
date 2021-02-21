using HarmonyLib;
using UnityEngine;
using CustomSlotItemLib;

namespace WishboneSlot
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        [HarmonyPostfix]
        static void PrefabPostfix(ref ZNetScene __instance)
        {
            GameObject wishbonePrefab = __instance.GetPrefab("Wishbone");
            CustomSlotManager.ApplyCustomSlotItem(wishbonePrefab, "wishbone");
        }
    }
}
