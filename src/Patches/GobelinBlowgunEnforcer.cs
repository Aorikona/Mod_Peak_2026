using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "Update")]
	public class GobelinBlowgunEnforcer
	{
		public static bool IsBlowgun(Item item)
		{
			if (item == null) return false;
			string n = item.name.ToLowerInvariant();
			return n.Contains("healingdart") || n.Contains("blowgun") || n.Contains("dart");
		}

		static void Postfix(Item __instance)
		{
			if (__instance == null) return;

			if (IsBlowgun(__instance))
			{
				Character holder = __instance.holderCharacter;
				bool heldByGobelin = holder != null && (ClassCheckUtil.IsGobelin(holder) || ClassManager.IsGobelinActive());

				if (heldByGobelin)
				{
					if (holder.IsLocal)
					{
						var trav = Traverse.Create(__instance);
						var field = trav.Field("destroyOnUse");
						if (field.FieldExists() && field.GetValue<bool>() == true)
						{
							field.SetValue(false);
						}

						try
						{
							if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
							{
								var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
								if (usesData != null && usesData.Value != -1) usesData.Value = -1;
							}
						}
						catch { }
					}

					if (__instance.UIData != null)
					{
						__instance.UIData.canDrop = false;
						__instance.UIData.canBackpack = false;
						__instance.UIData.canThrow = false;
						__instance.UIData.canPocket = true;
					}
				}
				else
				{
					if (__instance.UIData != null)
					{
						__instance.UIData.canDrop = true;
						__instance.UIData.canBackpack = true;
						__instance.UIData.canThrow = true;
						__instance.UIData.canPocket = true;
					}

					var trav = Traverse.Create(__instance);
					var field = trav.Field("destroyOnUse");
					if (field.FieldExists())
					{
						field.SetValue(true);
					}
				}
			}
		}
	}
}