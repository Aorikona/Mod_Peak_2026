using PeakClassMod.Utils;
using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Item), "StartUseSecondary")]
    [HarmonyPriority(Priority.Normal)]
    public class TechPriestConsumablePatch2
    {
        static bool Prefix(Item __instance)
        {
            return true; // Let the game handle consumption, effects are blocked elsewhere.
        }
    }
}
