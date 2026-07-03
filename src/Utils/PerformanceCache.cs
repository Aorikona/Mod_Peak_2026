using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;

namespace PeakClassMod.Utils
{
    internal static class PerformanceCache
    {
        private static readonly ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static FieldInfo DestroyOnUseField;

        static PerformanceCache()
        {
            DestroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");
        }

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;

            if (_isBlowgunCache.TryGetValue(item, out var isBlowgun))
            {
                return isBlowgun.Value;
            }

            bool result = item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0;
            try
            {
                _isBlowgunCache.Add(item, new StrongBox<bool>(result));
            }
            catch { }
            return result;
        }
    }
}
