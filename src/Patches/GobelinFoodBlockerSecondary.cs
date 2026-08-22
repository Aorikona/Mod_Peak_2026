using PeakClassMod;
using PeakClassMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerSecondary
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || (__instance.holderCharacter.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(__instance))
				{
					return false; // Bloque l'action secondaire
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "CanUseSecondary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerSecondary_CanUse
	{
		static bool Prefix(Item __instance, ref bool __result)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || (__instance.holderCharacter.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(__instance))
				{
					__result = false;
					return false;
				}
			}
			return true;
		}
	}
}
