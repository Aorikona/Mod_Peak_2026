using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Runtime.CompilerServices;

namespace PeakClassMod.Patches
{
    // Caches whether an item is a bag to avoid string operations and GC allocs in Update.
    internal static class BagCheckCache
    {
        private static ConditionalWeakTable<Item, StrongBox<bool>> _isBagCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static bool IsBag(Item item)
        {
            if (item == null) return false;
            if (_isBagCache.TryGetValue(item, out var isBag))
            {
                return isBag.Value;
            }
            bool check = item.name != null && item.name.IndexOf("bag", StringComparison.OrdinalIgnoreCase) >= 0;
            _isBagCache.Add(item, new StrongBox<bool>(check));
            return check;
        }
    }

    [HarmonyPatch(typeof(Item), "StartUsePrimary")]
    [HarmonyPriority(Priority.High)]
    public class DualNatureBagRestrictionPrimaryPatch
    {
        static bool Prefix(Item __instance)
        {
            if (__instance == null || __instance.holderCharacter == null) return true;

            if (ClassCheckUtil.IsDualNature(__instance.holderCharacter) && BagCheckCache.IsBag(__instance))
            {
                return false; // Block primary usage
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Item), "StartUseSecondary")]
    [HarmonyPriority(Priority.High)]
    public class DualNatureBagRestrictionSecondaryPatch
    {
        static bool Prefix(Item __instance)
        {
            if (__instance == null || __instance.holderCharacter == null) return true;

            if (ClassCheckUtil.IsDualNature(__instance.holderCharacter) && BagCheckCache.IsBag(__instance))
            {
                return false; // Block secondary usage
            }
            return true;
        }
    }
}