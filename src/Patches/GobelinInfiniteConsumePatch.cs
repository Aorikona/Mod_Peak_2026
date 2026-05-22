
using UnityEngine;
using PeakClassMod.Utils;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    // ==================================================
    // GOBELIN INFINITE ITEM FIX (CORRIGÉ)
    // ==================================================

    [HarmonyPatch(typeof(Item), "Consume")]
    public class GobelinInfiniteConsumePatch
    {
        static bool Prefix(Item __instance)
        {
            // Sécurité de base
            if (__instance == null || __instance.holderCharacter == null) return true;

            // 1. Est-ce un Gobelin ?
            if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
            {
                // 2. Est-ce la Sarbacane ?
                if (__instance.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // --- MANUELLEMENT FINIR L'ACTION ---

                    // A. Mettre à jour le timer (pour dire "c'est bon, il a tiré")
                    __instance.holderCharacter.data.lastConsumedItem = Time.time;

                    // B. DÉBLOQUER L'ANIMATION (C'est ça qui bloquait !)
                    // On dit à l'animateur : "L'objet est consommé, reviens en position normale"
                    if (__instance.holderCharacter.refs != null && __instance.holderCharacter.refs.animator != null)
                    {
                        __instance.holderCharacter.refs.animator.SetBool("Consumed Item", true);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
