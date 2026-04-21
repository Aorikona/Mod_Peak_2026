using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace PeakClassMod.Patches
{
    [HarmonyPatch]
    public class MushroomancerVFXPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();

            var play = AccessTools.Method(typeof(ScreenVFX), "Play");
            if (play != null) methods.Add(play);

            var startFX = AccessTools.Method(typeof(ScreenVFX), "StartFX");
            if (startFX != null) methods.Add(startFX);

            return methods;
        }

        static bool Prefix()
        {
            if (ClassManager.IsMushroomancerActive())
            {
                // Bypass screen VFX if local player is Mushroomancer
                return false;
            }
            return true;
        }
    }
}
