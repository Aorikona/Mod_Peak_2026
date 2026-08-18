using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions))]
	public static class UpdateStatus_TechPriest_Patch
	{
		[HarmonyPatch("GetCurrentStatus")]
		[HarmonyPostfix]
		public static void GetCurrentStatusPostfix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float __result)
		{
			if (__instance?.character == null) return;

			// Poids
			if (statusType == CharacterAfflictions.STATUSTYPE.Weight)
			{
				if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character))
					__result = 0f;
				else if (ClassManager.IsScoutActive() && ClassCheckUtil.IsScout(__instance.character))
					__result *= ClassBalance.SCOUT_WEIGHT_MULT;
				else if (ClassManager.IsGeneralActive() && ClassCheckUtil.IsGeneral(__instance.character))
					__result *= ClassBalance.GENERAL_WEIGHT_MULT;
			}

			// Malédiction
			if (statusType == CharacterAfflictions.STATUSTYPE.Curse)
			{
				if (ClassManager.IsGeneralActive() && ClassCheckUtil.IsGeneral(__instance.character))
				{
					if (__result < ClassBalance.GENERAL_CURSE_MIN) __result = ClassBalance.GENERAL_CURSE_MIN;
				}
				else if (ClassManager.IsMedicActive() && ClassCheckUtil.IsMedicHat(__instance.character))
				{
					if (__result < ClassBalance.MEDIC_CURSE) __result = ClassBalance.MEDIC_CURSE;
				}
			}

			// Immunités
			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && (ClassCheckUtil.IsMushroom(__instance.character) || ClassManager.IsMushroomancerActive()))
				__result = 0f;

			if (statusType == CharacterAfflictions.STATUSTYPE.Spores && (ClassCheckUtil.IsGobelin(__instance.character) || ClassManager.IsGobelinActive()))
				__result = 0f;
		}

		[HarmonyPatch("UpdateNormalStatuses")]
		[HarmonyPostfix]
		public static void UpdateNormalStatusesPostfix(CharacterAfflictions __instance)
		{
			if (__instance?.character == null || __instance.currentStatuses == null) return;

			var ch = __instance.character;

			// Tech-Priest : Poids = 0
			if (ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Weight] = 0f;
			}

			// Mushroomancer : Poison = 0
			if (ClassCheckUtil.IsMushroom(ch) || ClassManager.IsMushroomancerActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Poison] = 0f;
			}

			// Gobelin : Spores = 0
			if (ClassCheckUtil.IsGobelin(ch) || ClassManager.IsGobelinActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Spores] = 0f;
			}

			// Medic : Curse plancher 15%
			if (ClassManager.IsMedicActive() && ClassCheckUtil.IsMedicHat(ch))
			{
				int idx = (int)CharacterAfflictions.STATUSTYPE.Curse;
				if (__instance.currentStatuses[idx] < ClassBalance.MEDIC_CURSE)
					__instance.currentStatuses[idx] = ClassBalance.MEDIC_CURSE;
			}

			// General : Curse plancher 35%
			if (ClassManager.IsGeneralActive() && ClassCheckUtil.IsGeneral(ch))
			{
				int idx = (int)CharacterAfflictions.STATUSTYPE.Curse;
				if (__instance.currentStatuses[idx] < ClassBalance.GENERAL_CURSE_MIN)
					__instance.currentStatuses[idx] = ClassBalance.GENERAL_CURSE_MIN;
			}
		}
	}
}