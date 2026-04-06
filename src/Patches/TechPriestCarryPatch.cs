using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterInteractible), "CanBeCarried")]
    public class TechPriestCarryPatch
    {
        static void Postfix(CharacterInteractible __instance, ref bool __result)
        {
            if (__instance == null) return;

            // Allow TechPriest to carry conscious teammates
            if (Character.localCharacter != null && ClassCheckUtil.IsTechPriest(Character.localCharacter) && ClassManager.IsTechPriestActive())
            {
                __result = true;
            }
        }
    }
}
