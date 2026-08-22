using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item))]
	public class GobelinCurseOnShootPatch
	{
		private static float _lastCurseShootTime = 0f;

		[HarmonyPatch("FinishCastPrimary")]
		[HarmonyPostfix]
		static void FinishCastPostfix(Item __instance)
		{
			TriggerCurse(__instance);
		}

		private static void TriggerCurse(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return;

			var ch = __instance.holderCharacter;

			if (ch.IsLocal && GobelinBlowgunEnforcer.IsBlowgun(__instance))
			{
				if (Time.time - _lastCurseShootTime > 0.2f)
				{
					_lastCurseShootTime = Time.time;
					GobelinBlowgunEnforcer.ApplyGobelinCurse(ch);
				}
			}
		}
	}
}