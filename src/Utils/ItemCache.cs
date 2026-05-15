using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Utils
{
    public static class ItemCache
    {
        private static Dictionary<string, Item> _byName;
        private static bool _fullyInitialized = false;

        public static Item Get(string name)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
                var dbItems = Singleton<ItemDatabase>.Instance?.items;
                if (dbItems != null)
                {
                    foreach (var item in dbItems)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.name) && !_byName.ContainsKey(item.name))
                        {
                            _byName[item.name] = item;
                        }
                    }
                }
            }

            if (_byName.TryGetValue(name, out var foundItem))
            {
                return foundItem;
            }

            if (!_fullyInitialized)
            {
                _fullyInitialized = true;
                foreach (var it in Resources.FindObjectsOfTypeAll<Item>())
                {
                    if (it != null && !string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name))
                    {
                        _byName[it.name] = it;
                    }
                }

                if (_byName.TryGetValue(name, out var fullyFoundItem))
                {
                    return fullyFoundItem;
                }
            }

            return null;
        }
    }
}
