using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

namespace PeakClassMod.Utils
{
    internal static class TechPriestBlocker
    {
        private static readonly HashSet<string> BlockedTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "heal", "cure", "medicine", "restorehealth" };
        private static readonly HashSet<string> BlockedItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bandage", "firstaid", "medkit", "cure", "marshmallow" };
        private static Dictionary<int, bool> _blockCache = new Dictionary<int, bool>();
        private static Dictionary<Type, bool> _typeBlockCache = new Dictionary<Type, bool>();

        public static bool ShouldBlockItem(Item item)
        {
            if (item == null) return false;
            int id = item.GetInstanceID();
            if (_blockCache.TryGetValue(id, out bool cached)) return cached;

            bool block = false;
            if (item.GetComponent<Action_RestoreHunger>() != null || item.GetComponent<Action_GiveExtraStamina>() != null) block = true;

            if (!block)
            {
                string n = item.name ?? "";
                foreach (var b in BlockedItemNames)
                {
                    if (n.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        block = true;
                        break;
                    }
                }
            }

            if (!block)
            {
                foreach (var c in item.GetComponents<MonoBehaviour>())
                {
                    if (c == null) continue;
                    Type type = c.GetType();

                    if (_typeBlockCache.TryGetValue(type, out bool isTypeBlocked))
                    {
                        if (isTypeBlocked)
                        {
                            block = true;
                            break;
                        }
                        continue;
                    }

                    string t = type.Name;
                    bool blockedType = false;
                    foreach (var b in BlockedTypeNames)
                    {
                        if (t.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            blockedType = true;
                            break;
                        }
                    }
                    _typeBlockCache[type] = blockedType;
                    if (blockedType)
                    {
                        block = true;
                        break;
                    }
                }
            }
            _blockCache[id] = block;
            return block;
        }
        public static void ClearCache() { _blockCache.Clear(); _typeBlockCache.Clear(); }
    }
}
