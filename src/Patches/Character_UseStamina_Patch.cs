using PeakClassMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Character), "UseStamina")]
    public class Character_UseStamina_Patch
    {
        static void Prefix(Character __instance, ref float usage)
        {
            if (__instance == null || !__instance.IsLocal) return;
            if (ClassManager.IsScoutActive()) usage *= ClassBalance.SCOUT_STAMINA_MULT;
            else if (ClassManager.IsRogueActive()) usage *= ClassBalance.ROGUE_STAMINA_MULT;
        }
    }
}
