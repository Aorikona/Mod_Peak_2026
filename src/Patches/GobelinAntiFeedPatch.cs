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
        private static System.Reflection.FieldInfo _bestCharacterField;
        private static System.Reflection.FieldInfo _characterField;
        private static Type _characterInteractibleType;

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

                // 3. REFLECTION CACHED : On force l'accès à la variable interne 'bestCharacter'
                if (_bestCharacterField == null)
                    _bestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");

                if (_bestCharacterField == null) return true;

                var bestCharInteractible = _bestCharacterField.GetValue(interact) as MonoBehaviour;

                // Si on vise quelqu'un (bestCharacter n'est pas null)
                if (bestCharInteractible != null)
                {
                    // 4. On récupère le "Character" à l'intérieur de CharacterInteractible
                    if (_characterInteractibleType == null)
                        _characterInteractibleType = bestCharInteractible.GetType();

                    if (_characterField == null)
                        _characterField = AccessTools.Field(_characterInteractibleType, "character");

                    if (_characterField == null) return true;

                    var targetChar = _characterField.GetValue(bestCharInteractible) as Character;

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
