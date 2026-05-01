using System.Runtime.CompilerServices;
using UnityEngine.UI;

namespace PeakClassMod.Utils
{
    internal static class BarAfflictionCache
    {
        private static ConditionalWeakTable<BarAffliction, StrongBox<Image>> _cache = new ConditionalWeakTable<BarAffliction, StrongBox<Image>>();

        public static Image GetImage(BarAffliction affliction)
        {
            if (affliction == null) return null;
            if (_cache.TryGetValue(affliction, out var cached))
            {
                return cached.Value;
            }

            Image image = affliction.GetComponent<Image>();
            try
            {
                _cache.Add(affliction, new StrongBox<Image>(image));
            }
            catch
            {
                // Ignore if already added by another thread.
            }
            return image;
        }
    }
}
