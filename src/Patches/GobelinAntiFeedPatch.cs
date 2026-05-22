using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using System.Reflection;

namespace PeakClassMod.Patches
{

    public static class GobelinAntiFeedCache
    {
        public static FieldInfo bestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");
        // We assume bestCharacter is of type CharacterInteractible, which inherits MonoBehaviour.
        // The type name might not be accessible directly without the namespace, but we can look it up by name or just use a generic approach.
        // Actually, let's look up the "character" field lazily or by knowing the type.
        public static FieldInfo characterInteractibleCharacterField = null;
    }
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
                var bestCharInteractible = GobelinAntiFeedCache.bestCharacterField?.GetValue(interact) as MonoBehaviour;

                // Si on vise quelqu'un (bestCharacter n'est pas null)
                if (bestCharInteractible != null)
                {
                    // 4. On récupère le "Character" à l'intérieur de CharacterInteractible
                    // (On suppose que CharacterInteractible a un champ public 'character')
                    // Comme on n'a pas le type CharacterInteractible, on utilise aussi Traverse par sécurité
                    if (GobelinAntiFeedCache.characterInteractibleCharacterField == null) { GobelinAntiFeedCache.characterInteractibleCharacterField = AccessTools.Field(bestCharInteractible.GetType(), "character"); }
                    var targetChar = GobelinAntiFeedCache.characterInteractibleCharacterField?.GetValue(bestCharInteractible) as Character;

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
