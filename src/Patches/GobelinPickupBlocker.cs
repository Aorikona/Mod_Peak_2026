using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "StartInteract")]
	public static class GobelinPickupBlocker
	{
		[HarmonyPrefix]
		public static bool Prefix(Item __instance, Character interactor)
		{
			if (__instance == null || interactor == null) return true;

			if (ClassCheckUtil.IsGobelin(interactor) || (interactor.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(__instance))
				{
					// Vérifie si le Gobelin a déjà une sarbacane dans les mains ou l'inventaire
					if (HasAnyBlowgun(interactor))
					{
						PeakClassModPlugin.Log?.LogInfo("[Gobelin] Déjà une sarbacane possédée : ramassage d'une 2ème sarbacane refusé.");
						return false;
					}
				}
			}

			return true;
		}

		[HarmonyPatch(typeof(Item), "CanInteract")]
		[HarmonyPostfix]
		public static void PostfixCanInteract(Item __instance, Character character, ref bool __result)
		{
			if (!__result || __instance == null || character == null) return;

			if (ClassCheckUtil.IsGobelin(character) || (character.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(__instance) && HasAnyBlowgun(character))
				{
					__result = false;
				}
			}
		}

		private static bool HasAnyBlowgun(Character ch)
		{
			if (ch == null) return false;

			if (ch.data != null && ch.data.currentItem != null && GobelinBlowgunEnforcer.IsBlowgun(ch.data.currentItem))
				return true;

			if (ch.player != null && ch.player.itemSlots != null)
			{
				foreach (var slot in ch.player.itemSlots)
				{
					if (slot != null && slot.prefab != null && GobelinBlowgunEnforcer.IsBlowgun(slot.prefab))
						return true;
				}
			}

			return false;
		}
	}
}