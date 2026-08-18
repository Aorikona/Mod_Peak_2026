using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using Photon.Pun;
using System.Collections;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix()
		{
			// Réinitialise pour permettre le spawn des objets sur l'île
			ClassModNetwork.ResetAllPlayerItemFlags();
			PeakClassModPlugin.Instance?.StartCoroutine(ReTriggerItemsOnIsland());
		}

		private static IEnumerator ReTriggerItemsOnIsland()
		{
			yield return new WaitForSeconds(2.0f);
			var lc = Character.localCharacter;
			if (lc != null)
			{
				var net = lc.GetComponent<ClassModNetwork>();
				if (net != null)
				{
					net.TriggerStartItems();
				}

				var watcher = lc.GetComponent<ClassWatcherHandler>();
				if (watcher != null)
				{
					watcher.CheckAndApplyClass(true);
				}
			}
		}
	}
}