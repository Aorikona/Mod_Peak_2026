using HarmonyLib;
using PeakClassMod.Handlers;
using PeakClassMod;
using System;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Character), "Start")]
	public class Character_Start_AddNetComp_Patch
	{
		static void Postfix(Character __instance)
		{
			if (__instance != null)
			{
				var p = PeakClassModPlugin.Instance;
				p.EnsureComponent<ClassModNetwork>(__instance.gameObject);
				p.EnsureComponent<ClassWatcherHandler>(__instance.gameObject);
				p.EnsureComponent<AstronautStatHandler>(__instance.gameObject);
				p.EnsureComponent<MedicStatHandler>(__instance.gameObject);
				p.EnsureComponent<TechPriestWeightEnforcer>(__instance.gameObject);
				p.EnsureComponent<GobelinSizeHandler>(__instance.gameObject);
				p.EnsureComponent<GobelinBlowgunHandler>(__instance.gameObject);
				p.EnsureComponent<WolfHandler>(__instance.gameObject);
				p.EnsureComponent<RogueMovementHandler>(__instance.gameObject);
				p.EnsureComponent<CookHandler>(__instance.gameObject);
			}
		}
	}
}