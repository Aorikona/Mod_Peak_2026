using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(ScreenVFX), "CurseFX")]
    public class ScreenVFX_CurseFX_Patch
    {
        static bool Prefix()
        {
            if (ClassManager.IsMushroomancerActive())
            {
                // Cancel the standard white flash screen VFX if we are a Mushroomancer
                return false;
            }
            return true;
        }
    }
}
