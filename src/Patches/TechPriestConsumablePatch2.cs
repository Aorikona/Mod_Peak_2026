using PeakClassMod.Utils;
using HarmonyLib;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	[HarmonyPriority(Priority.Normal)]
	public class TechPriestConsumablePatch2
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;
			var ch = __instance.holderCharacter;

			if ((ch.IsLocal && ClassManager.IsTechPriestActive()) || ClassCheckUtil.IsTechPriest(ch))
			{
				// Bloque uniquement les boosters de stamina secondaire
				var affAction = __instance.GetComponent<Action_ApplyAffliction>();
				if (affAction != null && affAction.affliction != null && affAction.affliction.GetType().Name.Contains("FasterBoi"))
				{
					return false;
				}
			}
			return true;
		}
	}
}
