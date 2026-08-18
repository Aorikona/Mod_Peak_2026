using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zorro.Core;
using Photon.Pun;

namespace PeakClassMod.Utils
{
	public static class HatUtils
	{
		private static string _lastLocalHatName = "None";

		private static readonly string[] KnownHats = new[]
		{
			"Hat_Scoutmaster", "Hat_Medic", "MedicHelmet", "Hat_Crab",
			"Hat_Mushroom", "Hat_Propeller", "Hat_NinjaHeadband",
			"Hat_Bandana", "Hat_Captain", "Hat_Astronaut", "Hat_Midsummer", "Hat_WolfEars"
		};

		public static void SetLastLocalHatName(string hatName)
		{
			if (!string.IsNullOrEmpty(hatName) && hatName != "Unknown" && hatName != "None")
			{
				_lastLocalHatName = hatName;
			}
		}

		public static bool TryGetHatName(Character ch, out string hatName)
		{
			hatName = null;
			if (ch == null) return false;

			// --- SOURCE 1 : Inspection directe des GameObjects attachés sur le personnage ---
			try
			{
				var transforms = ch.GetComponentsInChildren<Transform>(true);
				foreach (var t in transforms)
				{
					if (t == null || !t.gameObject.activeInHierarchy) continue;
					string objName = t.name;
					foreach (var known in KnownHats)
					{
						if (objName.IndexOf(known, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							hatName = known;
							if (ch.IsLocal) SetLastLocalHatName(hatName);
							return true;
						}
					}
				}
			}
			catch { }

			// --- SOURCE 2 : PersistentPlayerDataService & Customization ---
			try
			{
				var ppds = GameHandler.GetService<PersistentPlayerDataService>();
				PersistentPlayerData data = null;
				if (ch.photonView?.Owner != null) data = ppds?.GetPlayerData(ch.photonView.Owner);
				if (data == null && ch.IsLocal && PhotonNetwork.LocalPlayer != null) data = ppds?.GetPlayerData(PhotonNetwork.LocalPlayer);

				if (data != null && Singleton<Customization>.Instance != null)
				{
					var hats = Singleton<Customization>.Instance.hats;
					int i = data.customizationData.currentHat;
					if (hats != null && i >= 0 && i < hats.Length && hats[i] != null)
					{
						hatName = hats[i].name;
						if (ch.IsLocal) SetLastLocalHatName(hatName);
						return true;
					}
				}
			}
			catch { }

			// --- SOURCE 3 : Mémoire locale ---
			if (ch.IsLocal && !string.IsNullOrEmpty(_lastLocalHatName) && _lastLocalHatName != "None")
			{
				hatName = _lastLocalHatName;
				return true;
			}

			return false;
		}

		public static bool HasAnyHat(Character ch, params string[] names)
		{
			if (!TryGetHatName(ch, out var name)) return false;
			return names.Any(n => name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		public static bool HasHat(Character ch, string expected)
		{
			return TryGetHatName(ch, out var name) && name.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}