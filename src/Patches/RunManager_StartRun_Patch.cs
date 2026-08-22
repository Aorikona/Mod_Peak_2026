using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Collections;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix()
		{
			PeakClassModPlugin.Instance?.StartCoroutine(ReApplyClassOnIsland());
		}

		private static IEnumerator ReApplyClassOnIsland()
		{
			yield return new WaitForSeconds(1.5f);
			var lc = Character.localCharacter;
			if (lc != null)
			{
				var watcher = lc.GetComponent<ClassWatcherHandler>();
				if (watcher != null)
				{
					watcher.CheckAndApplyClass(true);
				}
			}
		}
	}
}