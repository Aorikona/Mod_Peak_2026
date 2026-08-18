using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "Consume")]
	public class GobelinInfiniteConsumePatch
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter) || ClassManager.IsGobelinActive())
			{
				if (GobelinBlowgunEnforcer.IsBlowgun(__instance))
				{
					__instance.holderCharacter.data.lastConsumedItem = Time.time;

					if (__instance.holderCharacter.refs?.animator != null)
					{
						__instance.holderCharacter.refs.animator.SetBool("Consumed Item", true);
					}

					return false;
				}
			}

			return true;
		}
	}
}