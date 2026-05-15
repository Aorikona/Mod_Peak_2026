using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PeakClassMod.Utils
{
    public static class PerformanceCache
    {
        private static readonly ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;

            if (_isBlowgunCache.TryGetValue(item, out StrongBox<bool> cachedValue))
            {
                return cachedValue.Value;
            }

            bool isBlowgun = false;
            if (!string.IsNullOrEmpty(item.name))
            {
                isBlowgun = item.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            try
            {
                _isBlowgunCache.Add(item, new StrongBox<bool>(isBlowgun));
            }
            catch (ArgumentException)
            {
                // Ignore if added concurrently by another thread, though unlikely in Unity main thread
            }

            return isBlowgun;
        }
    }

    // Polyfill for StrongBox if missing in target framework, though usually in System.Runtime.CompilerServices
    // It's used here specifically as requested in memory for ConditionalWeakTable caching.
}
