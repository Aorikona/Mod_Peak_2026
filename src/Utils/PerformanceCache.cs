using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Utils
{
    internal static class PerformanceCache
    {
        private static readonly ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();
        public static FieldInfo _destroyOnUseField;

        static PerformanceCache()
        {
            _destroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");
        }

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;

            if (_isBlowgunCache.TryGetValue(item, out var cached))
            {
                return cached.Value;
            }

            bool isBlowgun = !string.IsNullOrEmpty(item.name) && item.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0;

            try
            {
                _isBlowgunCache.Add(item, new StrongBox<bool>(isBlowgun));
            }
            catch (ArgumentException)
            {
                // Ignore if it was added by another thread concurrently
            }

            return isBlowgun;
        }

        public static void Clear()
        {
            // ConditionalWeakTable cleans itself up when keys are GC'd,
            // but we can't manually clear it easily in .NET 4.7.2.
            // Since the keys are Unity objects, they'll be garbage collected when destroyed.
        }
    }
}
