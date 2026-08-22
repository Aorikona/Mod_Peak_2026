using System;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	public static class CookFoodHelper
	{
		public static bool IsRawFood(Item item)
		{
			if (item == null) return false;

			// Les structures à placer, outils, sacs, réchauds ne sont jamais de la nourriture
			if (item is Backpack || item.GetComponent<Constructable>() != null || item.GetComponent<Action_Spawn>() != null)
				return false;

			// Doit posséder une action de consommation (comestible)
			bool isEdible = item.GetComponent<Action_Consume>() != null
						 || item.GetComponent<Action_ConsumeAndSpawn>() != null
						 || item.GetComponent<Action_RestoreHunger>() != null;

			if (!isEdible) return false;

			// Si déjà cuit, ce n''est plus cru
			int cookedAmount = 0;
			if (item.data != null && item.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var data))
			{
				cookedAmount = data.Value;
			}
			if (cookedAmount > 0) return false;

			// Si c''est un aliment cuisable
			var cooking = item.GetComponent<ItemCooking>();
			if (cooking != null && cooking.canBeCooked)
			{
				return true;
			}

			// Si c''est une baie ou un champignon
			if (item.itemTags.HasFlag(Item.ItemTags.Berry) || item.itemTags.HasFlag(Item.ItemTags.Mushroom))
			{
				return true;
			}

			string name = item.gameObject.name.ToLowerInvariant();
			if (name.Contains("berry") || name.Contains("shroom") || name.Contains("marshmallow") || name.Contains("root") || name.Contains("egg") || name.Contains("meat"))
			{
				return true;
			}

			return false;
		}
	}

	// Bloque uniquement l''utilisation primaire (manger) des aliments crus pour le Cook
	[HarmonyPatch(typeof(Item), "CanUsePrimary")]
	public static class Cook_ItemCanUsePrimary_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Item __instance, ref bool __result)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;
			var ch = __instance.holderCharacter;

			if (ClassCheckUtil.IsCook(ch))
			{
				if (CookFoodHelper.IsRawFood(__instance))
				{
					__result = false;
					return false;
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Action_Consume), "RunAction")]
	public static class Cook_ActionConsume_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Action_Consume __instance)
		{
			if (__instance == null) return true;
			var ch = __instance.GetComponentInParent<Character>() ?? Character.localCharacter;
			var itm = __instance.GetComponent<Item>();

			if (ch != null && ClassCheckUtil.IsCook(ch))
			{
				if (CookFoodHelper.IsRawFood(itm))
				{
					return false;
				}
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Action_ConsumeAndSpawn), "RunAction")]
	public static class Cook_ActionConsumeAndSpawn_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Action_ConsumeAndSpawn __instance)
		{
			if (__instance == null) return true;
			var ch = __instance.GetComponentInParent<Character>() ?? Character.localCharacter;
			var itm = __instance.GetComponent<Item>();

			if (ch != null && ClassCheckUtil.IsCook(ch))
			{
				if (CookFoodHelper.IsRawFood(itm))
				{
					return false;
				}
			}

			return true;
		}
	}

	// Bonus d''endurance de +25% quand le Cook mange de la nourriture cuite
	[HarmonyPatch(typeof(Item), "Consume")]
	public static class Cook_ItemConsume_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(Item __instance, int consumerID)
		{
			if (__instance == null) return;

			PhotonView pv = PhotonNetwork.GetPhotonView(consumerID);
			if (pv == null) return;
			Character ch = pv.GetComponent<Character>();
			if (ch == null) return;

			if (ClassCheckUtil.IsCook(ch))
			{
				int cookedAmount = 0;
				if (__instance.data != null && __instance.data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var data))
				{
					cookedAmount = data.Value;
				}

				if (cookedAmount >= 1)
				{
					ch.AddExtraStamina(ClassBalance.COOK_STAMINA_BONUS);
					ch.AddStamina(ClassBalance.COOK_STAMINA_BONUS);
					PeakClassModPlugin.Log?.LogInfo($"[Cook] {ch.gameObject.name} consumed cooked food! +25% Stamina bonus granted.");
				}
			}
		}
	}
}
