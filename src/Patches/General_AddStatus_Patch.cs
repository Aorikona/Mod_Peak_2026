using HarmonyLib;
using PeakClassMod.Utils;
using Peak.Afflictions;
using UnityEngine;
using System;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
	public static class General_AddStatus_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount, ref bool __result)
		{
			var ch = __instance?.character;
			if (ch == null) return true;

			bool isLocal = ch.IsLocal;

			// GOBELIN : Immunité totale Spores
			if (statusType == CharacterAfflictions.STATUSTYPE.Spores && ((isLocal && ClassManager.IsGobelinActive()) || ClassCheckUtil.IsGobelin(ch)))
			{
				amount = 0f;
				__result = false;
				return false;
			}

			// MUSHROOMANCER : Immunité totale Poison
			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && ((isLocal && ClassManager.IsMushroomancerActive()) || ClassCheckUtil.IsMushroom(ch)))
			{
				amount = 0f;
				__result = false;
				return false;
			}

			// TECH-PRIEST : Immunité totale Poids
			if (statusType == CharacterAfflictions.STATUSTYPE.Weight && ((isLocal && ClassManager.IsTechPriestActive()) || ClassCheckUtil.IsTechPriest(ch)))
			{
				amount = 0f;
				__result = false;
				return false;
			}

			// FAIM
			if (isLocal && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
			{
				if (ClassManager.IsAstronautActive()) amount *= ClassBalance.ASTRONAUT_HUNGER_MULT;
				else if (ClassManager.IsScoutActive()) amount *= ClassBalance.SCOUT_HUNGER_MULT;
				else if (ClassManager.IsWolfActive() && !DaySegmentUtils.IsDay()) amount *= ClassBalance.WOLF_HUNGER_MULT;
				else if (ClassManager.IsTechPriestActive()) amount *= ClassBalance.TECH_PRIEST_HUNGER_MULT;
				else if (ClassManager.IsGeneralActive()) amount *= ClassBalance.GENERAL_HUNGER_MULT;
				else if (ClassManager.IsCookActive()) amount *= ClassBalance.COOK_HUNGER_MULT;
			}

			// GENERAL : Socle de Malédiction
			if (isLocal && ClassManager.IsGeneralActive() && statusType == CharacterAfflictions.STATUSTYPE.Curse && amount < 0f)
			{
				float current = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse);
				if (current + amount < ClassBalance.GENERAL_CURSE_MIN)
				{
					float allowed = ClassBalance.GENERAL_CURSE_MIN - current;
					if (allowed >= 0f) { __result = false; return false; }
					amount = allowed;
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "AdjustStatus")]
	public static class General_AdjustStatus_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			var ch = __instance?.character;
			if (ch == null) return true;

			bool isLocal = ch.IsLocal;

			if (statusType == CharacterAfflictions.STATUSTYPE.Spores && ((isLocal && ClassManager.IsGobelinActive()) || ClassCheckUtil.IsGobelin(ch)))
			{
				amount = 0f;
				return false;
			}

			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && ((isLocal && ClassManager.IsMushroomancerActive()) || ClassCheckUtil.IsMushroom(ch)))
			{
				amount = 0f;
				return false;
			}

			if (statusType == CharacterAfflictions.STATUSTYPE.Weight && ((isLocal && ClassManager.IsTechPriestActive()) || ClassCheckUtil.IsTechPriest(ch)))
			{
				amount = 0f;
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "AddPoisonOverTime")]
	public static class General_AddPoisonOverTime_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterAfflictions __instance)
		{
			var ch = __instance?.character;
			if (ch == null) return true;

			if ((ch.IsLocal && ClassManager.IsMushroomancerActive()) || ClassCheckUtil.IsMushroom(ch))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "Update")]
	public static class General_Afflictions_Update_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(CharacterAfflictions __instance)
		{
			if (__instance == null || __instance.character == null) return;
			var ch = __instance.character;
			bool isLocal = ch.IsLocal;

			// GOBELIN : Purge Spores & Confusion chaque frame
			if ((isLocal && ClassManager.IsGobelinActive()) || ClassCheckUtil.IsGobelin(ch))
			{
				if (__instance.currentStatuses != null)
				{
					__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Spores] = 0f;
				}
				if (__instance.afflictionList != null)
				{
					for (int i = __instance.afflictionList.Count - 1; i >= 0; i--)
					{
						var aff = __instance.afflictionList[i];
						if (aff != null)
						{
							string name = aff.GetType().Name;
							if (aff is Affliction_Chaos || aff.GetAfflictionType() == Affliction.AfflictionType.Chaos || name.Contains("Chaos") || name.Contains("Confus") || name.Contains("Spore") || name.Contains("ZombieBite"))
							{
								__instance.afflictionList.RemoveAt(i);
							}
						}
					}
				}
			}

			// MUSHROOMANCER : Purge Poison chaque frame
			if ((isLocal && ClassManager.IsMushroomancerActive()) || ClassCheckUtil.IsMushroom(ch))
			{
				if (__instance.currentStatuses != null)
				{
					__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Poison] = 0f;
				}
				if (__instance.afflictionList != null)
				{
					for (int i = __instance.afflictionList.Count - 1; i >= 0; i--)
					{
						var aff = __instance.afflictionList[i];
						if (aff != null)
						{
							string name = aff.GetType().Name;
							if (aff is Affliction_PoisonOverTime || aff.GetAfflictionType() == Affliction.AfflictionType.PoisonOverTime || name.Contains("Poison"))
							{
								__instance.afflictionList.RemoveAt(i);
							}
						}
					}
				}
			}
		}
	}
}