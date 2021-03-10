using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace BiggerBoatMapExploreRadius.Patches
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateExplore))]
        public static void UpdateExplorePostfix(ref Minimap __instance, float dt, Player player)
        {
            if (__instance.m_exploreTimer != 0f) return;
            
            List<Player> playersInRange = new List<Player>();
            Player.GetPlayersInRange(player.transform.position, 20f, playersInRange);

            if (playersInRange.Any(p => p.GetControlledShip()))
            {
                __instance.Explore(player.transform.position, Main.boatExploreRadius.Value);
            }
        }
    }
}
