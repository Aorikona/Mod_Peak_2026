using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace PeakClassMod.Utils
{
    public static class PerformanceCache
    {
        public static readonly FieldInfo DestroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");
        public static readonly FieldInfo BestCharacterField = AccessTools.Field(typeof(Interaction), "bestCharacter");

        // The field "character" might be in a different class, like CharacterInteractible. We will cache it on first use or define it dynamically.
        // Assuming the generic interactible object has a 'character' field.
        private static readonly System.Collections.Generic.Dictionary<Type, FieldInfo> _characterFieldCache = new System.Collections.Generic.Dictionary<Type, FieldInfo>();

        public static FieldInfo GetCharacterField(Type type)
        {
            if (_characterFieldCache.TryGetValue(type, out FieldInfo cachedField))
            {
                return cachedField;
            }

            FieldInfo newField = AccessTools.Field(type, "character");
            _characterFieldCache[type] = newField;
            return newField;
        }

        private static readonly ConditionalWeakTable<Item, StrongBox<bool>> _isBlowgunCache = new ConditionalWeakTable<Item, StrongBox<bool>>();

        public static bool IsBlowgun(Item item)
        {
            if (item == null) return false;

            if (_isBlowgunCache.TryGetValue(item, out StrongBox<bool> cachedValue))
            {
                return cachedValue.Value;
            }

            bool isBlowgun = item.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0;
            try
            {
                _isBlowgunCache.Add(item, new StrongBox<bool>(isBlowgun));
            }
            catch (Exception ex)
            {
                PeakClassModPlugin.Log?.LogError(ex);
            }
            return isBlowgun;
        }
    }
}
