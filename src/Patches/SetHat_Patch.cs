using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using Zorro.Core;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
	public class SetHat_Patch
	{
		static void Postfix(CharacterCustomization __instance, int index)
		{
			try
			{
				var hats = Singleton<Customization>.Instance?.hats;
				string hatName = (hats != null && index >= 0 && index < hats.Length && hats[index] != null) ? hats[index].name : "Unknown";

				HatUtils.SetLastLocalHatName(hatName);

				var watcher = Character.localCharacter?.GetComponent<ClassWatcherHandler>();
				if (watcher != null)
				{
					watcher.CheckAndApplyClass(true);
				}
			}
			catch (Exception ex)
			{
				PeakClassModPlugin.Log?.LogWarning($"[SetHat_Patch] {ex.Message}");
			}
		}
	}
}