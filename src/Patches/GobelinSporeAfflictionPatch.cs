using PeakClassMod.Utils;
using Peak.Afflictions;
using HarmonyLib;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions), "AddAffliction")]
	public class GobelinSporeAfflictionPatch
	{
		static bool Prefix(CharacterAfflictions __instance, Affliction affliction)
		{
			if (__instance == null || __instance.character == null || affliction == null) return true;

			Character ch = __instance.character;
			if ((ch.IsLocal && ClassManager.IsGobelinActive()) || ClassCheckUtil.IsGobelin(ch))
			{
				// 1. Bloque la Confusion (Affliction_Chaos)
				if (affliction is Affliction_Chaos || affliction.GetAfflictionType() == Affliction.AfflictionType.Chaos)
				{
					PeakClassModPlugin.Log?.LogInfo("[Gobelin] Confusion (Chaos) blocked!");
					return false;
				}

				// 2. Bloque les Spores et morsures de zombie
				string name = affliction.GetType().Name;
				if (name.Contains("Chaos") || name.Contains("Confus") || name.Contains("Spore") || name.Contains("ZombieBite"))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Affliction {name} blocked!");
					return false;
				}
			}
			return true;
		}
	}
}
