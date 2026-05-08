using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace PeakClassMod.Utils
{
    internal static class PerformanceCache
    {
        private static ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static readonly FieldInfo destroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;

            if (_isBlowgunCache.TryGetValue(item, out StrongBox<bool> cachedValue))
            {
                return cachedValue.Value;
            }

            bool isBlowgun = item.name != null && item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0;

            try
            {
                _isBlowgunCache.Add(item, new StrongBox<bool>(isBlowgun));
            }
            catch
            {
                // Ignore race conditions on Add
            }

            return isBlowgun;
        }
    }
}
