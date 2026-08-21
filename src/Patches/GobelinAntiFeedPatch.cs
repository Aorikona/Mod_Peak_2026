using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace PeakClassMod.Patches
{
    // ==================================================
    // EMPÊCHER DE NOURRIR LE GOBELIN (FIXED)
    // ==================================================
    [HarmonyPatch(typeof(Item), "StartUseSecondary")]
    public class GobelinAntiFeedPatch
    {
        private static readonly FieldInfo bestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");
        private static FieldInfo characterField = null;

        static bool Prefix(Item __instance)
        {
            if (__instance == null) return true;

            // 1. Est-ce de la nourriture industrielle ?
            if (__instance.GetComponent<Action_RestoreHunger>() != null &&
                GobelinDiet.IsPackagedFood(__instance))
            {
                // 2. Accéder au système d'interaction
                var interact = Interaction.instance;
                if (interact == null) return true;

                if (bestCharacterField != null)
                {
                    var bestCharInteractible = bestCharacterField.GetValue(interact) as MonoBehaviour;

                    // Si on vise quelqu'un (bestCharacter n'est pas null)
                    if (bestCharInteractible != null)
                    {
                        if (characterField == null)
                        {
                            characterField = AccessTools.Field(bestCharInteractible.GetType(), "character");
                        }

                        if (characterField != null)
                        {
                            var targetChar = characterField.GetValue(bestCharInteractible) as Character;

                            // 5. Vérification finale : Si la cible est un Gobelin
                            if (targetChar != null && ClassCheckUtil.IsGobelin(targetChar))
                            {
                                // PeakClassModPlugin.Log?.LogInfo($"[Anti-Feed] Bloqué : On ne nourrit pas le Gobelin !");
                                return false; // BLOQUE L'ACTION
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
