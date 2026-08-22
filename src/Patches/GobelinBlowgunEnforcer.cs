using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;
using Zorro.Core;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "Update")]
	public class GobelinBlowgunEnforcer
	{
		public static bool IsBlowgun(Item item)
		{
			if (item == null) return false;
			if (item.itemID == 70) return true;
			string n = item.name.ToLowerInvariant();
			if (n.Contains("dart") || n.Contains("blowgun") || n.Contains("healing") || n.Contains("sarbacane")) return true;

			if (item.UIData != null && !string.IsNullOrEmpty(item.UIData.itemName))
			{
				string ui = item.UIData.itemName.ToLowerInvariant();
				if (ui.Contains("dart") || ui.Contains("blowgun") || ui.Contains("healing") || ui.Contains("sarbacane")) return true;
			}

			if (item.GetComponent<Action_RaycastDart>() != null || item.GetComponentInChildren<Action_RaycastDart>() != null) return true;

			return false;
		}

		static void Postfix(Item __instance)
		{
			if (__instance == null) return;

			if (IsBlowgun(__instance))
			{
				// 1. Forcer la disponibilité d'utilisation native
				var trav = Traverse.Create(__instance);
				var usField = trav.Field("overrideUsability");
				if (usField.FieldExists()) usField.SetValue(Optionable<bool>.Some(true));
				__instance.totalUses = -1;

				// 2. Désactiver et désabonner les composants de consommation
				var reduceUses = __instance.GetComponents<Action_ReduceUses>();
				foreach (var ru in reduceUses)
				{
					if (ru != null)
					{
						ru.consumeOnFullyUsed = false;
						if (ru.enabled)
						{
							ru.enabled = false;
							try
							{
								if (__instance.OnPrimaryFinishedCast != null)
								{
									__instance.OnPrimaryFinishedCast = (System.Action)System.Delegate.Remove(__instance.OnPrimaryFinishedCast, new System.Action(ru.RunAction));
								}
							}
							catch { }
						}
					}
				}

				var consumes = __instance.GetComponents<Action_Consume>();
				foreach (var c in consumes)
				{
					if (c != null)
					{
						if (c.enabled)
						{
							c.enabled = false;
							try
							{
								if (__instance.OnPrimaryFinishedCast != null)
								{
									__instance.OnPrimaryFinishedCast = (System.Action)System.Delegate.Remove(__instance.OnPrimaryFinishedCast, new System.Action(c.RunAction));
								}
								if (__instance.OnConsumed != null)
								{
									__instance.OnConsumed = (System.Action)System.Delegate.Remove(__instance.OnConsumed, new System.Action(c.RunAction));
								}
							}
							catch { }
						}
					}
				}

				// 3. Désactive destroyOnUse
				var field = trav.Field("destroyOnUse");
				if (field.FieldExists()) field.SetValue(false);

				// 4. Munitions infinies natives dans les données
				try
				{
					if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
					{
						var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
						if (usesData != null)
						{
							usesData.HasData = false;
							usesData.Value = 999;
						}
					}
				}
				catch { }
			}
		}

		public static void ApplyGobelinCurse(Character ch)
		{
			if (ch?.refs?.afflictions == null) return;

			ch.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.GOBELIN_CURSE_PER_SHOT, false, false, false);

			int curseIdx = (int)CharacterAfflictions.STATUSTYPE.Curse;
			if (ch.refs.afflictions.currentStatuses != null && ch.refs.afflictions.currentStatuses.Length > curseIdx)
			{
				ch.refs.afflictions.currentStatuses[curseIdx] += ClassBalance.GOBELIN_CURSE_PER_SHOT;
			}

			try { GUIManager.instance?.bar?.ChangeBar(); } catch { }
		}
	}
}