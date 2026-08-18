using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "FinishCastPrimary")]
	public class GobelinCurseOnShootPatch
	{
		static void Postfix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return;

			if (__instance.holderCharacter.IsLocal && (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || ClassManager.IsGobelinActive()))
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(__instance))
				{
					__instance.holderCharacter.refs?.afflictions?.AddStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.GOBELIN_CURSE_PER_SHOT,
						false);
				}
			}
		}
	}
}