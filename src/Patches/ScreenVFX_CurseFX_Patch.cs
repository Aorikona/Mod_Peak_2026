using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(ScreenVFX), "CurseFX")]
    public class ScreenVFX_CurseFX_Patch
    {
        static bool Prefix()
        {
            if (Character.localCharacter != null && ClassCheckUtil.IsMushroom(Character.localCharacter) && ClassManager.IsMushroomancerActive())
            {
                return false; // Blocks the white flash effect
            }
            return true;
        }
    }
}
