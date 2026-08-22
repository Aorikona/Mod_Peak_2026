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
			if (TechPriestItemChecker.ShouldBlockSelfUse(__instance))
			{
				PeakClassModPlugin.Log?.LogInfo($"[TechPriest] Self-use of {__instance.name} blocked!");
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "CanUsePrimary")]
	[HarmonyPriority(Priority.Normal)]
	public class TechPriestConsumablePatch1_CanUse
	{
		static bool Prefix(Item __instance, ref bool __result)
		{
			if (TechPriestItemChecker.ShouldBlockSelfUse(__instance))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
