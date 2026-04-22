using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterInteractible), "GetInteractText")]
    public class TechPriestInteractTextPatch
    {
        static void Postfix(CharacterInteractible __instance, ref string __result)
        {
            if (ClassManager.IsTechPriestActive() && __instance.character != null)
            {
                // Only change if not dead.
                if (!__instance.character.data.dead && !__instance.character.data.fullyPassedOut)
                {
                    __result = "Press [E] to Carry";
                }
            }
        }
    }

    [HarmonyPatch(typeof(CharacterInteractible), "CanBeInteractedWith")]
    public class TechPriestCanInteractPatch
    {
        static void Postfix(CharacterInteractible __instance, ref bool __result)
        {
            if (ClassManager.IsTechPriestActive() && __instance.character != null)
            {
                if (!__instance.character.data.dead)
                {
                    __result = true;
                }
            }
        }
    }
}
