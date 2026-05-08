using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    // ==================================================
    // EMPÊCHER DE NOURRIR LE GOBELIN (FIXED)
    // ==================================================
    [HarmonyPatch(typeof(Item), "StartUseSecondary")]
    public class GobelinAntiFeedPatch
    {
        private static readonly System.Reflection.FieldInfo bestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");
        // We will fallback to reflection to find CharacterInteractible type dynamically
        // to cache its FieldInfo if needed, or just keep it simple with AccessTools

        // Caching CharacterInteractible field
        private static System.Reflection.FieldInfo characterInteractibleField = null;

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

                // 3. REFLECTION : On force l'accès à la variable interne 'bestCharacter'
                var bestCharInteractible = bestCharacterField?.GetValue(interact) as MonoBehaviour;

                // Si on vise quelqu'un (bestCharacter n'est pas null)
                if (bestCharInteractible != null)
                {
                    if (characterInteractibleField == null)
                    {
                        characterInteractibleField = AccessTools.Field(bestCharInteractible.GetType(), "character");
                    }

                    // 4. On récupère le "Character" à l'intérieur de CharacterInteractible
                    var targetChar = characterInteractibleField?.GetValue(bestCharInteractible) as Character;

                    // 5. Vérification finale : Si la cible est un Gobelin
                    if (targetChar != null && ClassCheckUtil.IsGobelin(targetChar))
                    {
                        return false; // BLOQUE L'ACTION
                    }
                }
            }
            return true;
        }
    }
}
