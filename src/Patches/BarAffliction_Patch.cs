using PeakClassMod;
using PeakClassMod.Utils;
using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    internal static class BarAfflictionCache
    {
        public static System.Runtime.CompilerServices.ConditionalWeakTable<BarAffliction, System.Runtime.CompilerServices.StrongBox<Image>> Images = new System.Runtime.CompilerServices.ConditionalWeakTable<BarAffliction, System.Runtime.CompilerServices.StrongBox<Image>>();
    }

    [HarmonyPatch(typeof(BarAffliction), "ChangeAffliction")]
    public class BarAffliction_Patch
    {
        static void Postfix(BarAffliction __instance)
        {
            if (Character.localCharacter == null || __instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight) return;
            if (ClassManager.IsScoutActive()) __instance.size *= ClassBalance.SCOUT_WEIGHT_MULT;
            else if (ClassManager.IsGeneralActive()) __instance.size *= ClassBalance.GENERAL_WEIGHT_MULT;
            else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(Character.localCharacter)) {
                __instance.size = 0f;

                Image img = null;
                if (BarAfflictionCache.Images.TryGetValue(__instance, out var cachedImg)) {
                    img = cachedImg.Value;
                } else {
                    img = __instance.GetComponent<Image>();
                    try {
                        BarAfflictionCache.Images.Add(__instance, new System.Runtime.CompilerServices.StrongBox<Image>(img));
                    } catch { }
                }

                if (img != null) img.enabled = false;
            }
        }
    }
}
