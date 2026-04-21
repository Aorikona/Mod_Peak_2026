using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;
using Photon.Pun;
using System.Linq;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Character))]
    public class GobelinWeaponRestorer
    {
        [HarmonyPatch("Revive")]
        [HarmonyPostfix]
        static void Postfix_Revive(Character __instance)
        {
            EnsureBlowpipe(__instance);
        }

        [HarmonyPatch("WakeUp")]
        [HarmonyPostfix]
        static void Postfix_WakeUp(Character __instance)
        {
            EnsureBlowpipe(__instance);
        }

        private static void EnsureBlowpipe(Character character)
        {
            if (character == null || !character.IsLocal) return;
            if (character.player == null || character.player.itemSlots == null) return;

            if (ClassCheckUtil.IsGobelin(character))
            {
                bool hasBlowpipe = false;
                foreach (var slot in character.player.itemSlots)
                {
                    if (slot != null && slot.ItemInSlot != null)
                    {
                        if (slot.ItemInSlot.item != null && slot.ItemInSlot.item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasBlowpipe = true;
                            break;
                        }
                    }
                }

                if (!hasBlowpipe)
                {
                    var blowpipePrefab = ItemCache.Get(ClassBalance.BLOWGUN);
                    if (blowpipePrefab != null)
                    {
                        if (character.player.HasEmptySlot((ushort)blowpipePrefab.itemID))
                        {
                            character.player.AddItem((ushort)blowpipePrefab.itemID, null, out _);
                        }
                    }
                }
            }
        }
    }
}
