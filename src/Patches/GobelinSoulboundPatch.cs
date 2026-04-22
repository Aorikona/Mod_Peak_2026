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

            var wakeUp = AccessTools.Method(typeof(Character), "WakeUp");
            if (wakeUp != null) methods.Add(wakeUp);

            var revive = AccessTools.Method(typeof(Character), "CallRevive");
            if (revive != null) methods.Add(revive);

            var setDead = AccessTools.Method(typeof(Character), "SetDead");
            if (setDead != null) methods.Add(setDead);

            return methods;
        }

        static void Postfix(Character __instance)
        {
            if (__instance == null || !__instance.IsLocal) return;

            // If SetDead(true) was called, we probably shouldn't revive weapon.
            // The task specifically mentioned SetDead(false) which means they are revived.
            if (__instance.data != null && __instance.data.dead) return;

            if (ClassCheckUtil.IsGobelin(__instance))
            {
                EnsureBlowpipe(__instance);
            }
        }

        private static void EnsureBlowpipe(Character character)
        {
            if (character == null || character.player == null || character.player.itemSlots == null) return;

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
