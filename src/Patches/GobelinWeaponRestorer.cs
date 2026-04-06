using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(PlayerDeath), "Revive")]
    public class GobelinWeaponRestorer
    {
        static void Postfix(PlayerDeath __instance)
        {
            if (__instance == null) return;
            Character ch = __instance.GetComponent<Character>();
            if (ch != null && ch.IsLocal && ClassCheckUtil.IsGobelin(ch))
            {
                if (!ClassManager.IsGobelinActive()) return;

                var item = ItemCache.Get(ClassBalance.BLOWGUN);
                if (item != null && ch.player != null && ch.player.HasEmptySlot((ushort)item.itemID))
                {
                    // Check if they already have the blowgun to avoid duplicating
                    bool hasBlowgun = false;
                    foreach (var invItem in ch.player.inventory.GetAllItems())
                    {
                        if (invItem != null && invItem.name != null && invItem.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasBlowgun = true;
                            break;
                        }
                    }

                    if (!hasBlowgun)
                    {
                        ch.player.AddItem((ushort)item.itemID, null, out _);
                        PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Weapon restored after waking up.");
                    }
                }
            }
        }
    }
}
