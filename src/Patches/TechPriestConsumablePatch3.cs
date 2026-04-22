using PeakClassMod.Utils;
using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Item), "Consume")]
    public class TechPriestConsumableConsumePatch
    {
        static bool Prefix(Item __instance)
        {
            if (__instance != null && __instance.holderCharacter != null && ClassCheckUtil.IsTechPriest(__instance.holderCharacter))
            {
                // To fix infinite item bug for Tech Priest
                // The item needs to decrement uses. If another patch is blocking StartUsePrimary, it might not reach Consume.
                // Or maybe they CAN use it, but Consume isn't destroying it because of some other logic.
                // Wait, if StartUse returns false, it never reaches Consume.
            }
            return true;
        }
    }
}
