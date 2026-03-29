using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Utils
{
    public static class ItemCache
    {
        private static Dictionary<string, Item> _byName;
        private static float _lastRefreshTime = 0f;
        public static Item Get(string name)
        {
            if (_byName == null || Time.time - _lastRefreshTime > ClassBalance.ITEM_CACHE_DURATION)
            {
                _byName = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in Resources.FindObjectsOfTypeAll<Item>()) if (it != null && !string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name)) _byName[it.name] = it;
                _lastRefreshTime = Time.time;
            }
            return _byName.TryGetValue(name, out var item) ? item : null;
        }
    }
}
