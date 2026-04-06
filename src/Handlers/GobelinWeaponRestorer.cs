using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Handlers
{
    public class GobelinWeaponRestorer : MonoBehaviour
    {
        private Character _character;
        private bool _wasUnconscious = false;

        private void Start()
        {
            _character = GetComponent<Character>();
        }

        private void Update()
        {
            if (_character == null || !_character.IsLocal || !ClassManager.IsGobelinActive()) return;

            bool isUnconscious = _character.data != null && (_character.data.passedOut || _character.data.fullyPassedOut || _character.data.dead);

            // Detect transition from unconscious to conscious
            if (_wasUnconscious && !isUnconscious)
            {
                EnsureBlowpipe();
            }

            _wasUnconscious = isUnconscious;
        }

        private void EnsureBlowpipe()
        {
            if (_character.player == null || _character.player.itemSlots == null) return;

            // Check if player already has a blowgun
            bool hasBlowgun = false;
            foreach (var slot in _character.player.itemSlots)
            {
                if (slot != null && !slot.IsEmpty() && slot.prefab != null)
                {
                    if (slot.prefab.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasBlowgun = true;
                        break;
                    }
                }
            }

            if (!hasBlowgun)
            {
                Item blowpipe = ItemCache.Get(ClassBalance.BLOWGUN);
                if (blowpipe != null)
                {
                    ushort id = (ushort)blowpipe.itemID;
                    if (_character.player.HasEmptySlot(id))
                    {
                        _character.player.AddItem(id, null, out _);
                        // Optional: we can force equip it, but usually standard interaction picks it up into hand
                    }
                }
            }
        }
    }
}
