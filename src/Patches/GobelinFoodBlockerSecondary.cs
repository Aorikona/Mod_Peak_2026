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
    [HarmonyPatch(typeof(Item), "StartUseSecondary")]
    [HarmonyPriority(HarmonyLib.Priority.High)]
    public class GobelinFoodBlockerSecondary
    {
        static bool Prefix(Item __instance)
        {
            if (__instance == null || __instance.holderCharacter == null) return true;

            if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
            {
                if (__instance.GetComponent<Action_RestoreHunger>() != null &&
                    GobelinDiet.IsPackagedFood(__instance)) // Nouvelle méthode
                {
                    return false; // Bloque l'action
                }
            }
            return true;
        }
    }
}
