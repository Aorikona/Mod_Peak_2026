using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Utils
{
    internal static class PerformanceCache
    {
        private static ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();
        public static FieldInfo DestroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;
            if (_isBlowgunCache.TryGetValue(item, out var cached)) return cached.Value;

            bool isBg = item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0;
            try { _isBlowgunCache.Add(item, new StrongBox<bool>(isBg)); } catch { }
            return isBg;
        }
    }

    internal class StrongBox<T>
    {
        public T Value;
        public StrongBox(T value) { Value = value; }
    }
}
