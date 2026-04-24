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
                // On utilise Traverse pour lire le champ "bestCharacter" dans l'instance Interaction
                var bestCharInteractible = Traverse.Create(interact).Field("bestCharacter").GetValue<MonoBehaviour>();

                // Si on vise quelqu'un (bestCharacter n'est pas null)
                if (bestCharInteractible != null)
                {
                    // 4. On récupère le "Character" à l'intérieur de CharacterInteractible
                    // (On suppose que CharacterInteractible a un champ public 'character')
                    // Comme on n'a pas le type CharacterInteractible, on utilise aussi Traverse par sécurité
                    var targetChar = Traverse.Create(bestCharInteractible).Field("character").GetValue<Character>();

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
