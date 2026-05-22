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
    [HarmonyPatch(typeof(Item), "StartUsePrimary")]
    [HarmonyPriority(HarmonyLib.Priority.High)]
    public class GobelinFoodBlockerPrimary
    {
        static bool Prefix(Item __instance)
        {
            if (__instance == null || __instance.holderCharacter == null) return true;

            if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
            {
                // On vérifie si c'est de la nourriture ET si c'est "PackagedFood" via les tags
                if (__instance.GetComponent<Action_RestoreHunger>() != null &&
                    GobelinDiet.IsPackagedFood(__instance)) // Nouvelle méthode
                {
                    // PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Beurk ! Nourriture industrielle refusée : {__instance.name}");
                    return false; // Bloque l'action
                }
            }
            return true;
        }
    }
}
