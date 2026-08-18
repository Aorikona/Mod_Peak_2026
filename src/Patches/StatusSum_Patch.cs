using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions), "statusSum", MethodType.Getter)]
	public static class StatusSum_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(CharacterAfflictions __instance, ref float __result)
		{
			if (__instance?.character == null || !__instance.character.IsLocal) return;

			float w = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight);
			if (ClassManager.IsScoutActive())
			{
				__result = __result - w + (w * ClassBalance.SCOUT_WEIGHT_MULT);
			}
			else if (ClassManager.IsGeneralActive())
			{
				__result = __result - w + (w * ClassBalance.GENERAL_WEIGHT_MULT);
			}
			else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character))
			{
				__result -= w;
			}
		}
	}
}