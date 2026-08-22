using PeakClassMod.Utils;
using Peak.Afflictions;
using HarmonyLib;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions), "AddAffliction")]
	public class MushroomancerPoisonAfflictionPatch
	{
		static bool Prefix(CharacterAfflictions __instance, Affliction affliction)
		{
			if (__instance == null || __instance.character == null || affliction == null) return true;

			Character ch = __instance.character;
			if ((ch.IsLocal && ClassManager.IsMushroomancerActive()) || ClassCheckUtil.IsMushroom(ch))
			{
				if (affliction is Affliction_PoisonOverTime || affliction.GetAfflictionType() == Affliction.AfflictionType.PoisonOverTime || affliction.GetType().Name.Contains("Poison"))
				{
					PeakClassModPlugin.Log?.LogInfo("[Mushroomancer] Poison affliction blocked!");
					return false;
				}
			}
			return true;
		}
	}
}
