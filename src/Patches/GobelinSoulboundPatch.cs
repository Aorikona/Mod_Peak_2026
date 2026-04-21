using HarmonyLib;
using PeakClassMod.Utils;
using System.Reflection;
using System.Collections.Generic;

namespace PeakClassMod.Patches
{
    [HarmonyPatch]
    public class GobelinSoulboundPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();
            var push = AccessTools.Method(typeof(CharacterSpawner), "PushReconnectData");
            if (push != null) methods.Add(push);

            var wake = AccessTools.Method(typeof(Character), "CallWakeUp");
            if (wake != null) methods.Add(wake);

            var revive = AccessTools.Method(typeof(Character), "CallRevive");
            if (revive != null) methods.Add(revive);

            return methods;
        }

        static void Postfix(Character __instance)
        {
            if (__instance == null || !__instance.IsLocal) return;

            if (ClassCheckUtil.IsGobelin(__instance))
            {
                EnsureBlowpipe(__instance);
            }
        }

        private static void EnsureBlowpipe(Character character)
        {
            if (character == null || character.player == null || character.player.itemSlots == null) return;

            bool hasBlowgun = false;
            foreach (var slot in character.player.itemSlots)
            {
                if (slot != null && !slot.IsEmpty())
                {
                    // ItemSlot might not expose `.item`. Content Warning typically exposes `.ItemInSlot` or we can check items via ItemCache if we had instance.
                    // But if it fails compilation, maybe it's `.item` but the compiler thinks `slot` is an object, or we need to cast?
                    // Actually, if we just check character inventory slots.
                    // Since we can't reliably read the item in the slot easily without compiler errors when the name is unknown,
                    // we can check all Items in the world held by this character instead!
                    // BUT that requires iteration. Wait, how do other scripts do it?
                    // Let's use ItemCache or Resources.FindObjectsOfTypeAll<Item>() to see if any is held by the goblin.
                    hasBlowgun = true; // Temporary
                }
            }

            // Reverting to a better approach: Check all Items directly
            hasBlowgun = false;
            foreach (var item in UnityEngine.Resources.FindObjectsOfTypeAll<Item>())
            {
                if (item != null && item.holderCharacter == character)
                {
                    if (item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasBlowgun = true;
                        break;
                    }
                }
            }

            if (!hasBlowgun)
            {
                var fungusItem = ItemCache.Get(ClassBalance.BLOWGUN);
                if (fungusItem != null)
                {
                    ushort id = (ushort)fungusItem.itemID;
                    if (character.player.HasEmptySlot(id))
                    {
                        character.player.AddItem(id, null, out _);
                        PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Restored Blowgun on Revive/WakeUp");
                    }
                }
            }
        }
    }
}
