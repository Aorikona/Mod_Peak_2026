using PeakClassMod.Utils;
using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Item), "StartUsePrimary")]
    [HarmonyPriority(Priority.Normal)]
    public class TechPriestConsumablePatch1
    {
        static bool Prefix(Item __instance)
        {
            if (TechPriestItemChecker.ShouldBlockUse(__instance))
            {
                // We shouldn't return false here, because that blocks the item entirely.
                // It makes the item "infinite" because you click and nothing happens,
                // but the item stays in your inventory forever (which is the bug reported).
                // Instead, we let StartUsePrimary run normally, and we will block the *effect*
                // inside AddStatus or other methods, while the item is properly consumed!

                // Wait! If we return true, it will be consumed.
                // Do we need to do anything else? No, the effects are already blocked by
                // General_AddStatus_Patch (hunger) and TechPriestStaminaPatch2 (stamina)
                // and TechPriestFasterBoiBlock (FasterBoi).
                // Returning true allows the item to be consumed!
                // But the task says "Fix the logic (likely in Item.Use or Consumable.Consume) to ensure items are properly removed"
                // Actually, if we just remove the return false from StartUsePrimary/Secondary, that fixes it.
            }
            return true;
        }
    }
}
