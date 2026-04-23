using HarmonyLib;
using PeakClassMod.Utils;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

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

            var postRevive = AccessTools.Method(typeof(Character), "ApplyPostReviveStatus");
            if (postRevive != null) methods.Add(postRevive);

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
            // If the hooked method is PushReconnectData, it might not have __instance.
            Character target = __instance ?? Character.localCharacter;
            if (target == null || !target.IsLocal) return;

            if (target.data != null && target.data.dead) return;

            if (ClassCheckUtil.IsGobelin(target))
            {
                EnsureBlowpipe(target);
            }
        }

        private static void EnsureBlowpipe(Character character)
        {
            if (character == null || character.player == null || character.player.itemSlots == null) return;

            bool hasBlowpipe = false;

            foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
            {
                if (item != null && item.holderCharacter == character)
                {
                    if (item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasBlowpipe = true;
                        break;
                    }
                }
            }

            if (!hasBlowpipe)
            {
                var fungusItem = ItemCache.Get(ClassBalance.BLOWGUN);
                if (fungusItem != null)
                {
                    ushort id = (ushort)fungusItem.itemID;
                    if (character.player.HasEmptySlot(id))
                    {
                        character.player.AddItem(id, null, out _);
                        PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Restored Blowpipe on Revive/WakeUp");
                    }
                }
            }
        }
    }
}
