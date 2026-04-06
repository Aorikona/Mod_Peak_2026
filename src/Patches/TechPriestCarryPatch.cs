using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterInteractible), "CanBeCarried")]
    public class TechPriestCarryPatch
    {
        static void Postfix(CharacterInteractible __instance, ref bool __result)
        {
            // If the original already says true, nothing to do
            if (__result) return;

            // If we are a Tech Priest, we can carry conscious allies
            if (ClassManager.IsTechPriestActive())
            {
                // Basic checks equivalent to original, minus the fullyPassedOut check
                if (__instance.character != null && !__instance.character.isBot && !__instance.character.data.dead && !__instance.character.data.carrier)
                {
                    __result = true;
                }
            }
        }
    }
}
