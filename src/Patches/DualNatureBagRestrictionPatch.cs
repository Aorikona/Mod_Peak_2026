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

    [HarmonyPatch(typeof(Item), "Update")]
    public class DualNatureBagRestrictionUpdatePatch
    {
        static void Postfix(Item __instance)
        {
            if (__instance == null) return;
            if (!BagCheckCache.IsBag(__instance)) return;

            Character holder = __instance.holderCharacter;
            bool heldByDualNature = holder != null && ClassCheckUtil.IsDualNature(holder);

            // ONLY apply if held by someone
            if (holder != null)
            {
                if (heldByDualNature)
                {
                    if (__instance.UIData != null)
                    {
                        // Safely restrict inventory movements without allocating memory via reflection in Update
                        if (__instance.UIData.canPocket) __instance.UIData.canPocket = false;
                        if (__instance.UIData.canBackpack) __instance.UIData.canBackpack = false;

                        // We do not drop it forcefully here to avoid reflection performance issues.
                        // The player cannot pocket it, and cannot use it due to the Primary/Secondary patch.
                        // They will be forced to drop it using the default G key.
                    }
                }
                else
                {
                    // Restore state only if held by someone else, avoiding global state leak on unheld items
                    if (__instance.UIData != null)
                    {
                        if (!__instance.UIData.canPocket) __instance.UIData.canPocket = true;
                        if (!__instance.UIData.canBackpack) __instance.UIData.canBackpack = true;
                    }
                }
            }
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