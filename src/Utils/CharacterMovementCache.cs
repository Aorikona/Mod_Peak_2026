using System.Runtime.CompilerServices;
using UnityEngine;

namespace PeakClassMod.Utils
{
    internal static class CharacterMovementCache
    {
        private static ConditionalWeakTable<CharacterMovement, StrongBox<Character>> _cache = new ConditionalWeakTable<CharacterMovement, StrongBox<Character>>();

        public static Character GetCharacter(CharacterMovement movement)
        {
            if (movement == null) return null;
            if (_cache.TryGetValue(movement, out var cached))
            {
                return cached.Value;
            }

            Character character = movement.GetComponent<Character>();
            try
            {
                _cache.Add(movement, new StrongBox<Character>(character));
            }
            catch
            {
                // Ignore if already added by another thread.
            }
            return character;
        }
    }
}
