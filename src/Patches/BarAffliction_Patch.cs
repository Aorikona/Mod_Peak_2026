using PeakClassMod;
using PeakClassMod.Utils;
using System;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(BarAffliction), "ChangeAffliction")]
    public class BarAffliction_Patch
    {
        static void Postfix(BarAffliction __instance)
        {
            if (Character.localCharacter == null || __instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight) return;
            if (ClassManager.IsScoutActive()) __instance.size *= ClassBalance.SCOUT_WEIGHT_MULT;
            else if (ClassManager.IsGeneralActive()) __instance.size *= ClassBalance.GENERAL_WEIGHT_MULT;
            else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(Character.localCharacter)) { __instance.size = 0f; var img = __instance.GetComponent<Image>(); if (img != null) img.enabled = false; }
        }
    }
}
