using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using PeakClassMod.Handlers;

namespace PeakClassMod
{
	[BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "5.3.0")]
	public class PeakClassModPlugin : BaseUnityPlugin
	{
		public static PeakClassModPlugin Instance;
		internal static ManualLogSource Log;
		private static Harmony harmony;

		public static bool scoutActive = false;
		public static bool medicActive = false;
		public static bool techPriestActive = false;
		public static bool mushroomancerActive = false;
		public static bool greatEpeeActive = false;
		public static bool rogueActive = false;
		public static bool generalActive = false;
		public static bool astronautActive = false;
		public static bool gobelinActive = false;
		public static bool wolfActive = false;
		public static bool cookActive = false;
		public static string currentClass = "None";

		private GameObject _callbacksGO;

		private void Awake()
		{
			Instance = this;
			Log = Logger;
			harmony = new Harmony("com.peakmod.classmod");
			harmony.PatchAll();

			_callbacksGO = new GameObject("PeakClassModCallbacks");
			GameObject.DontDestroyOnLoad(_callbacksGO);
			_callbacksGO.AddComponent<PeakClassModCallbacks>();

			Log?.LogInfo("[ClassMod] Ready. Version 5.3.0 loaded.");
		}

		private void OnDestroy()
		{
			try { harmony?.UnpatchSelf(); } catch { }
			if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
		}

		public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
		{
			if (go != null && go.GetComponent<T>() == null) go.AddComponent<T>();
		}

		public static List<string> BuildStartItemsByHatName(string hatName)
		{
			var items = new List<string>();
			if (string.IsNullOrEmpty(hatName)) return items;

			if (hatName.IndexOf("Mushroom", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				for (int i = 0; i < 3; i++) items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
			}
			else if (hatName.IndexOf("Medic", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("FirstAidKit");
				items.Add("Cure-All");
			}
			else if (hatName.IndexOf("Crab", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("ChainShooter");
				items.Add("ChainShooter");
			}
			else if (hatName.IndexOf("Ninja", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("RopeSpool");
				items.Add("RopeSpool");
			}
			else if (hatName.IndexOf("Astronaut", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("BalloonBunch");
			}
			else if (hatName.IndexOf("Midsummer", StringComparison.OrdinalIgnoreCase) >= 0 ||
					 hatName.IndexOf("Gobelin", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("HealingDart Variant");
				items.Add("MagicBean");
			}
			else if (hatName.IndexOf("Chef", StringComparison.OrdinalIgnoreCase) >= 0 ||
					 hatName.IndexOf("Cook", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				items.Add("portablestovetopitem");
			}

			return items;
		}

		private static readonly string[] StartBerries = new[] { "Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green" };
	}
}