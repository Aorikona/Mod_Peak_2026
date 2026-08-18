using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Character), "UseStamina")]
	public static class Character_UseStamina_Patch
	{
		[HarmonyPrefix]
		public static void Prefix(Character __instance, [HarmonyArgument(0)] ref float stamina)
		{
			if (__instance == null || !__instance.IsLocal) return;

			if (ClassManager.IsScoutActive())
			{
				stamina *= ClassBalance.SCOUT_STAMINA_MULT;
			}
			else if (ClassManager.IsRogueActive())
			{
				stamina *= ClassBalance.ROGUE_STAMINA_MULT;
			}
		}
	}
}