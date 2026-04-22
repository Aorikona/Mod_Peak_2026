using HarmonyLib;
using PeakClassMod.Utils;
using System.Reflection;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Item), "Interact")]
    public class GobelinUniqueWeaponPatch
    {
        static bool Prefix(Item __instance, Character interactor)
        {
            if (__instance == null || interactor == null || !interactor.IsLocal) return true;

            // If the local player is a Goblin and they are trying to interact with a Blowgun
            if (ClassCheckUtil.IsGobelin(interactor) && __instance.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Check if they already have one to prevent dupes
                foreach (var item in UnityEngine.Resources.FindObjectsOfTypeAll<Item>())
                {
                    if (item != null && item.holderCharacter == interactor && item != __instance)
                    {
                        if (item.name.IndexOf(ClassBalance.BLOWGUN, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Blocked picking up an extra Blowgun.");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
