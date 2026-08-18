using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;
using System;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions))]
	public static class General_AddStatus_Patch
	{
		[HarmonyPatch(nameof(CharacterAfflictions.AddStatus), new Type[] {
			typeof(CharacterAfflictions.STATUSTYPE),
			typeof(float),
			typeof(bool)
		})]
		[HarmonyPrefix]
		public static bool Prefix3(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			return ProcessAddStatus(__instance, statusType, ref amount);
		}

		[HarmonyPatch(nameof(CharacterAfflictions.AddStatus), new Type[] {
			typeof(CharacterAfflictions.STATUSTYPE),
			typeof(float),
			typeof(bool),
			typeof(bool),
			typeof(bool)
		})]
		[HarmonyPrefix]
		public static bool Prefix5(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			return ProcessAddStatus(__instance, statusType, ref amount);
		}

		private static bool ProcessAddStatus(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			var ch = __instance?.character;
			if (ch == null) return true;

			// 1. FAIM
			if (ch.IsLocal && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
			{
				if (ClassManager.IsAstronautActive()) amount *= ClassBalance.ASTRONAUT_HUNGER_MULT;
				else if (ClassManager.IsScoutActive()) amount *= ClassBalance.SCOUT_HUNGER_MULT;
				else if (ClassManager.IsWolfActive() && !DaySegmentUtils.IsDay()) amount *= ClassBalance.WOLF_HUNGER_MULT;
				else if (ClassManager.IsTechPriestActive()) amount *= ClassBalance.TECH_PRIEST_HUNGER_MULT;
				else if (ClassManager.IsGeneralActive()) amount *= ClassBalance.GENERAL_HUNGER_MULT;
			}

			// 2. GOBELIN : Immunité Spores
			if (statusType == CharacterAfflictions.STATUSTYPE.Spores && (ClassCheckUtil.IsGobelin(ch) || ClassManager.IsGobelinActive()))
			{
				amount = 0f;
				return false;
			}

			// 3. TECH-PRIEST : Immunité Poids
			if (statusType == CharacterAfflictions.STATUSTYPE.Weight && (ClassCheckUtil.IsTechPriest(ch) || ClassManager.IsTechPriestActive()))
			{
				amount = 0f;
				return false;
			}

			// 4. MUSHROOMANCER : Immunité Poison
			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && (ClassCheckUtil.IsMushroom(ch) || ClassManager.IsMushroomancerActive()))
			{
				amount = 0f;
				return false;
			}

			// 5. GENERAL : Socle de Malédiction
			if (ch.IsLocal && ClassManager.IsGeneralActive() && statusType == CharacterAfflictions.STATUSTYPE.Curse && amount < 0f)
			{
				float current = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse);
				if (current + amount < ClassBalance.GENERAL_CURSE_MIN)
				{
					float allowed = ClassBalance.GENERAL_CURSE_MIN - current;
					if (allowed >= 0f) return false;
					amount = allowed;
				}
			}

			return true;
		}
	}
}