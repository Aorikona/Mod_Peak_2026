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

        // Changed to use the specific Character instance to prevent triggering for everyone
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
                if (slot != null && !slot.IsEmpty() && slot.item != null)
                {
                    if (slot.item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
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
