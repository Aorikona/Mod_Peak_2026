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
    [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
    public class UpdateStatus_TechPriest_Patch
    {
        static void Postfix(CharacterAfflictions __instance)
        {
            if (__instance?.character != null && ClassCheckUtil.IsTechPriest(__instance.character) && ClassManager.IsTechPriestActive())
            {
                __instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Hunger] = 0f;
                __instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Weight] = 0f;
            }
        }
    }
}
