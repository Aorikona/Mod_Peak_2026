using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace PeakClassMod.Utils
{
    public static class PerformanceCache
    {
        private static readonly ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static readonly FieldInfo destroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");

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
                // Ignored - another thread might have added it
            }

            return isBlowgun;
        }
    }
}
