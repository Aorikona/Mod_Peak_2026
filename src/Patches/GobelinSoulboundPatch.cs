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

            // Use FindObjectsOfTypeAll to check for the Blowgun assigned to this character
            // since ItemSlot doesn't expose the item directly in a way the compiler recognizes without the dll
            bool hasBlowgun = false;
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
