using PeakClassMod;
using PeakClassMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	public class GobelinAntiFeedPatch
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null) return true;

			if (GobelinDiet.IsPackagedFood(__instance))
			{
				var interact = Interaction.instance;
				if (interact == null) return true;

				var bestCharInteractible = Traverse.Create(interact).Field("bestCharacter").GetValue<MonoBehaviour>();
				if (bestCharInteractible != null)
				{
					var targetChar = Traverse.Create(bestCharInteractible).Field("character").GetValue<Character>();
					if (targetChar != null && ClassCheckUtil.IsGobelin(targetChar))
					{
						PeakClassModPlugin.Log?.LogInfo($"[Anti-Feed] Blocked feeding industrial food to Gobelin!");
						return false;
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Character), "FeedItem")]
	public class GobelinAntiFeed_Character_Patch
	{
		static bool Prefix(Character __instance, Item item)
		{
			if (__instance == null || item == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance) || (__instance.IsLocal && ClassManager.IsGobelinActive()))
			{
				if (GobelinDiet.IsPackagedFood(item))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Anti-Feed] Gobelin refused to be fed {item.name}!");
					return false;
				}
			}
			return true;
		}
	}
}
