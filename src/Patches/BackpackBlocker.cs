using HarmonyLib;
using PeakClassMod.Utils;
using UnityEngine;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(Backpack), nameof(Backpack.Wear))]
	public static class BackpackBlocker
	{
		[HarmonyPrefix]
		public static bool Prefix(Backpack __instance, Character interactor)
		{
			if (interactor == null) return true;

			if (ClassCheckUtil.IsWolf(interactor) || ClassManager.IsWolfActive())
			{
				return false; // Bloque l'équipement du sac
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Backpack), "Interact")]
	public static class BackpackInteractBlocker
	{
		[HarmonyPrefix]
		public static bool Prefix(Backpack __instance, Character interactor)
		{
			if (interactor == null) return true;

			if (ClassCheckUtil.IsWolf(interactor) || ClassManager.IsWolfActive())
			{
				return false;
			}
			return true;
		}
	}
}