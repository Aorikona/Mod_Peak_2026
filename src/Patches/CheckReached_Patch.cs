using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(MountainProgressHandler), "CheckReached")]
    static class CheckReached_Patch
    {
        public static event Action<Biome.BiomeType> OnBiomeReached;
        static void Postfix(MountainProgressHandler.ProgressPoint point, bool __result)
        {
            if (!__result) return;

            OnBiomeReached?.Invoke(point.biome);
        }
    }
}
