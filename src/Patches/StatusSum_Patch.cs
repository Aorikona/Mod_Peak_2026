using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterAfflictions), "get_statusSum")]
    public class StatusSum_Patch
    {
        static void Postfix(CharacterAfflictions __instance, ref float __result)
        {
            if (__instance?.character == null || !__instance.character.IsLocal) return;
            float w = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight);
            if (ClassManager.IsScoutActive()) __result = __result - w + (w * ClassBalance.SCOUT_WEIGHT_MULT);
            else if (ClassManager.IsGeneralActive()) __result = __result - w + (w * ClassBalance.GENERAL_WEIGHT_MULT);
            else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character)) __result -= w;
        }
    }
}
