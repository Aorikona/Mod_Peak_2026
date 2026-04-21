using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(ScreenVFX), "CurseFX")]
    public class ScreenVFX_CurseFX_Patch
    {
        static bool Prefix()
        {
            if (Character.localCharacter != null && ClassCheckUtil.IsMushroom(Character.localCharacter))
            {
                // Block the curse screen effect for Mushroomancer
                return false;
            }
            return true;
        }
    }
}
