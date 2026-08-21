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

using System.Runtime.CompilerServices;

namespace PeakClassMod.Patches
{
    internal static class BarAfflictionCache
    {
        private static readonly ConditionalWeakTable<BarAffliction, StrongBox<Image>> _cache = new ConditionalWeakTable<BarAffliction, StrongBox<Image>>();

        public static Image GetImage(BarAffliction instance)
        {
            if (_cache.TryGetValue(instance, out StrongBox<Image> box))
            {
                return box.Value;
            }

            Image img = instance.GetComponent<Image>();
            try
            {
                _cache.Add(instance, new StrongBox<Image>(img));
            }
            catch { } // thread safety

            return img;
        }
    }

    [HarmonyPatch(typeof(BarAffliction), "ChangeAffliction")]
    public class BarAffliction_Patch
    {
        static void Postfix(BarAffliction __instance)
        {
            if (Character.localCharacter == null || __instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight) return;
            if (ClassManager.IsScoutActive()) __instance.size *= ClassBalance.SCOUT_WEIGHT_MULT;
            else if (ClassManager.IsGeneralActive()) __instance.size *= ClassBalance.GENERAL_WEIGHT_MULT;
            else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(Character.localCharacter))
            {
                __instance.size = 0f;
                var img = BarAfflictionCache.GetImage(__instance);
                if (img != null) img.enabled = false;
            }
        }
    }
}
