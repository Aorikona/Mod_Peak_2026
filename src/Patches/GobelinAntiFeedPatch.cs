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

                if (_bestCharacterField == null)
                {
                    _bestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");
                }

                // 3. REFLECTION : On force l'accès à la variable interne 'bestCharacter'
                var bestCharInteractible = _bestCharacterField?.GetValue(interact) as MonoBehaviour;

                // Si on vise quelqu'un (bestCharacter n'est pas null)
                if (bestCharInteractible != null)
                {
                    if (_characterField == null)
                    {
                        _characterField = AccessTools.Field(bestCharInteractible.GetType(), "character");
                    }

                    // 4. On récupère le "Character" à l'intérieur de CharacterInteractible
                    var targetChar = _characterField?.GetValue(bestCharInteractible) as Character;

                    // 5. Vérification finale : Si la cible est un Gobelin
                    if (targetChar != null && ClassCheckUtil.IsGobelin(targetChar))
                    {
                        // PeakClassModPlugin.Log?.LogInfo($"[Anti-Feed] Bloqué : On ne nourrit pas le Gobelin !");
                        return false; // BLOQUE L'ACTION
                    }
                }
            }
            return true;
        }
    }
}
