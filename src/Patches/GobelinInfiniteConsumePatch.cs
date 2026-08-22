using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;
using System;
using System.Collections;
using Zorro.Core;

namespace PeakClassMod.Patches
{
	// 1. Protection au niveau de l'inventaire : Player.EmptySlot
	[HarmonyPatch(typeof(Player), "EmptySlot")]
	public static class Player_EmptySlot_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Player __instance, Optionable<byte> slot)
		{
			if (__instance == null || slot.IsNone) return true;

			ItemSlot itemSlot = __instance.GetItemSlot(slot.Value);
			if (itemSlot != null && itemSlot.prefab != null)
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(itemSlot.prefab))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Player.EmptySlot on slot {slot.Value} for {itemSlot.prefab.name}");
					return false; // NE VIDE JAMAIS LE SLOT DE LA SARBACANE DU GOBELIN
				}
			}
			return true;
		}
	}

	// 2. Protection réseau : Player.RPCRemoveItemFromSlot
	[HarmonyPatch(typeof(Player), "RPCRemoveItemFromSlot")]
	public static class Player_RPCRemoveItemFromSlot_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Player __instance, byte slotID)
		{
			if (__instance == null) return true;

			ItemSlot itemSlot = __instance.GetItemSlot(slotID);
			if (itemSlot != null && itemSlot.prefab != null && GobelinBlowgunEnforcer.IsBlowgun(itemSlot.prefab))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED RPCRemoveItemFromSlot on slot {slotID}");
				return false;
			}
			return true;
		}
	}

	// 3. Bloque Action_Consume.RunAction
	[HarmonyPatch(typeof(Action_Consume), "RunAction")]
	public static class Action_Consume_RunAction_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Action_Consume __instance)
		{
			if (__instance == null) return true;
			Item item = Traverse.Create(__instance).Field("item").GetValue<Item>() ?? __instance.GetComponent<Item>() ?? __instance.GetComponentInParent<Item>();
			if (item != null && GobelinBlowgunEnforcer.IsBlowgun(item))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Action_Consume.RunAction on {item.name}");
				return false;
			}
			return true;
		}
	}

	// 4. Bloque Action_ReduceUses.RunAction
	[HarmonyPatch(typeof(Action_ReduceUses), "RunAction")]
	public static class Action_ReduceUses_RunAction_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Action_ReduceUses __instance)
		{
			if (__instance == null) return true;
			Item item = Traverse.Create(__instance).Field("item").GetValue<Item>() ?? __instance.GetComponent<Item>() ?? __instance.GetComponentInParent<Item>();
			if (item != null && GobelinBlowgunEnforcer.IsBlowgun(item))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Action_ReduceUses.RunAction on {item.name}");
				return false;
			}
			return true;
		}
	}

	// 5. Bloque Action_ReduceUses.ReduceUsesRPC
	[HarmonyPatch(typeof(Action_ReduceUses), "ReduceUsesRPC")]
	public static class Action_ReduceUses_ReduceUsesRPC_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Action_ReduceUses __instance)
		{
			if (__instance == null) return true;
			Item item = Traverse.Create(__instance).Field("item").GetValue<Item>() ?? __instance.GetComponent<Item>() ?? __instance.GetComponentInParent<Item>();
			if (item != null && GobelinBlowgunEnforcer.IsBlowgun(item))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Action_ReduceUses.ReduceUsesRPC on {item.name}");
				return false;
			}
			return true;
		}
	}

	// 6. Bloque la coroutine Item.ConsumeDelayed
	[HarmonyPatch(typeof(Item), "ConsumeDelayed")]
	public static class Item_ConsumeDelayed_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Item __instance, ref IEnumerator __result)
		{
			if (__instance != null && GobelinBlowgunEnforcer.IsBlowgun(__instance))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Item.ConsumeDelayed on {__instance.name}");
				__result = EmptyRoutine();
				return false;
			}
			return true;
		}

		private static IEnumerator EmptyRoutine()
		{
			yield break;
		}
	}

	// 7. Bloque la méthode Item.Consume(int)
	[HarmonyPatch(typeof(Item), "Consume", new Type[] { typeof(int) })]
	public static class Item_Consume_Int_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(Item __instance, int consumerID)
		{
			if (__instance != null && GobelinBlowgunEnforcer.IsBlowgun(__instance))
			{
				PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED Item.Consume on {__instance.name}");
				return false; // Bloque totalement le RPC Consume
			}
			return true;
		}
	}

	// 8. Bloque la destruction de l'objet en main : CharacterItems.DestroyHeldItemRpc
	[HarmonyPatch(typeof(CharacterItems), "DestroyHeldItemRpc")]
	public static class CharacterItems_DestroyHeldItemRpc_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterItems __instance)
		{
			if (__instance == null) return true;
			Character ch = __instance.GetComponent<Character>();
			if (ch != null && ch.data != null && ch.data.currentItem != null)
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(ch.data.currentItem))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Blowgun] BLOCKED CharacterItems.DestroyHeldItemRpc for {ch.data.currentItem.name}");
					return false;
				}
			}
			return true;
		}
	}



	// 9. Bloque le drop uniquement pour la sarbacane du Gobelin
	[HarmonyPatch(typeof(CharacterItems), "DropItemRpc")]
	public static class CharacterItems_DropItemRpc_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterItems __instance, byte slotID)
		{
			if (__instance == null) return true;
			Character ch = __instance.GetComponent<Character>() ?? __instance.GetComponentInParent<Character>();
			if (ch == null || ch.player == null) return true;

			if (ClassCheckUtil.IsGobelin(ch) || (ch.IsLocal && ClassManager.IsGobelinActive()))
			{
				ItemSlot slot = ch.player.GetItemSlot(slotID);
				if (slot != null && slot.prefab != null && GobelinBlowgunEnforcer.IsBlowgun(slot.prefab))
				{
					PeakClassModPlugin.Log?.LogInfo("[Blowgun] Le Gobelin ne peut pas jeter sa sarbacane !");
					return false;
				}
			}
			return true;
		}
	}

	// 10. Bloque le drop depuis slot uniquement pour la sarbacane du Gobelin
	[HarmonyPatch(typeof(CharacterItems), "DropItemFromSlotRPC")]
	public static class CharacterItems_DropItemFromSlotRPC_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterItems __instance, byte slotID)
		{
			if (__instance == null) return true;
			Character ch = __instance.GetComponent<Character>() ?? __instance.GetComponentInParent<Character>();
			if (ch == null || ch.player == null) return true;

			if (ClassCheckUtil.IsGobelin(ch) || (ch.IsLocal && ClassManager.IsGobelinActive()))
			{
				ItemSlot slot = ch.player.GetItemSlot(slotID);
				if (slot != null && slot.prefab != null && GobelinBlowgunEnforcer.IsBlowgun(slot.prefab))
				{
					return false;
				}
			}
			return true;
		}
	}

	// 11. Réinitialise immédiatement la sarbacane après le tir pour le tir suivant
	[HarmonyPatch(typeof(Item), "FinishCastPrimary")]
	public static class Item_FinishCastPrimary_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(Item __instance)
		{
			if (__instance != null && GobelinBlowgunEnforcer.IsBlowgun(__instance))
			{
				__instance.finishedCast = false;
				__instance.isUsingPrimary = false;
				Traverse.Create(__instance).Property("castProgress").SetValue(0f);
			}
		}
	}

	// 13. Court-circuite la fonction DoUsing native quand on tient la sarbacane ou de la nourriture industrielle
	[HarmonyPatch(typeof(CharacterItems), "DoUsing")]
	public static class CharacterItems_DoUsing_Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(CharacterItems __instance)
		{
			if (__instance == null) return true;
			Character ch = __instance.GetComponent<Character>();
			if (ch == null || ch.data == null || ch.data.currentItem == null) return true;

			// A. Sarbacane gérée par GobelinBlowgunHandler
			if (ch.data.currentItem.itemID == 70 || GobelinBlowgunEnforcer.IsBlowgun(ch.data.currentItem))
			{
				return false;
			}

			// B. Blocage total de la nourriture industrielle pour le Gobelin
			if (ClassCheckUtil.IsGobelin(ch) || (ch.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(ch.data.currentItem))
				{
					return false; // Impossible de consommer
				}
			}
			return true;
		}
	}
}