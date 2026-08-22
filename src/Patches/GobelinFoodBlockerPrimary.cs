using PeakClassMod;
using PeakClassMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerPrimary
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || (__instance.holderCharacter.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(__instance))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Refused packaged food: {__instance.name}");
					return false; // Bloque l'action de manger
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "CanUsePrimary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerPrimary_CanUse
	{
		static bool Prefix(Item __instance, ref bool __result)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || (__instance.holderCharacter.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(__instance))
				{
					__result = false;
					return false; // Interdit le clic
				}
			}
			return true;
		}
	}
}
