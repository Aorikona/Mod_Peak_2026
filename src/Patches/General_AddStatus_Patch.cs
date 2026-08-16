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
    [HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
    public class General_AddStatus_Patch
    {
        static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
        {
            var ch = __instance?.character; if (ch == null) return true;
            var daySegmentUtils = new DaySegmentUtils();
            if (ch.IsLocal && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
            {
                if (ClassManager.IsAstronautActive()) amount *= ClassBalance.ASTRONAUT_HUNGER_MULT;
                else if (ClassManager.IsScoutActive()) amount *= ClassBalance.SCOUT_HUNGER_MULT;
                else if (ClassManager.IsWolfActive() && daySegmentUtils.IsDay() == false) amount *= ClassBalance.WOLF_HUNGER_MULT;
                else if (ClassManager.IsTechPriestActive()) amount *= ClassBalance.TECH_PRIEST_HUNGER_MULT;
                else if (ClassManager.IsGeneralActive()) amount *= ClassBalance.GENERAL_HUNGER_MULT;     
            }
            // --- GOBELIN IMMUNITY (NOUVEAU) ---
            if (ClassCheckUtil.IsGobelin(ch))
            {
                // Si on essaie d'ajouter des Spores au Gobelin
                if (statusType == CharacterAfflictions.STATUSTYPE.Spores)
                {
                    amount = 0f; // On annule le montant
                    return false; // On bloque l'exécution
                }
            }
            if (ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
            {
                if (statusType == CharacterAfflictions.STATUSTYPE.Hunger) amount *= ClassBalance.TECH_PRIEST_HUNGER_MULT;
                else if (statusType == CharacterAfflictions.STATUSTYPE.Weight) { amount = 0f; return false; }
            }
            if (statusType == CharacterAfflictions.STATUSTYPE.Poison && ClassCheckUtil.IsMushroom(ch)) amount = 0f;
            return true;


        }
    }
}
