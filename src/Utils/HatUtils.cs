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

		public static void SetLastLocalHatName(string hatName)
		{
			if (!string.IsNullOrEmpty(hatName) && hatName != "Unknown" && hatName != "None")
			{
				_lastLocalHatName = hatName;
			}
		}

		public static string GetLastLocalHatName() => _lastLocalHatName;

		public static bool TryGetHatName(Character ch, out string hatName)
		{
			hatName = null;
			if (ch == null) return false;

			// --- SOURCE 1 : Inspection des GameObjects de chapeaux actifs ---
			try
			{
				if (ch.refs?.customization?.refs?.playerHats != null)
				{
					foreach (var h in ch.refs.customization.refs.playerHats)
					{
						if (h != null && h.gameObject.activeInHierarchy)
						{
							hatName = h.gameObject.name;
							if (ch.IsLocal) SetLastLocalHatName(hatName);
							return true;
						}
					}
				}

				var transforms = ch.GetComponentsInChildren<Transform>(true);
				foreach (var t in transforms)
				{
					if (t == null || !t.gameObject.activeInHierarchy) continue;
					string objName = t.name;
					if (objName.StartsWith("Hat_", StringComparison.OrdinalIgnoreCase) || objName.Contains("Crown") || objName.Contains("Helmet") || objName.Contains("Bandana"))
					{
						hatName = objName;
						if (ch.IsLocal) SetLastLocalHatName(hatName);
						return true;
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

			// --- SOURCE 3 : Mémoire locale persistée ---
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