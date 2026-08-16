using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
    // ==================================================
    // APPLICATION DE LA CURSE AU TIR (FIABLE)
    // ==================================================
    [HarmonyPatch(typeof(Item), "FinishCastPrimary")]
    public class GobelinCurseOnShootPatch
    {
        static void Postfix(Item __instance)
        {
            // Sécurité
            if (__instance == null || __instance.holderCharacter == null) return;

            // On ne l'applique que pour le joueur Local (pour ne pas curse les autres sur mon écran)
            if (__instance.holderCharacter.IsLocal && ClassCheckUtil.IsGobelin(__instance.holderCharacter))
            {
                // Vérifier si c'est la Sarbacane
                if (__instance.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Appliquer la Curse
                    __instance.holderCharacter.refs?.afflictions?.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Curse,
                        ClassBalance.GOBELIN_CURSE_PER_SHOT,
                        false);
                }
            }
        }
    }
}
