using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Utils
{
	public static class ItemCache
	{
		private static Dictionary<string, Item> _byName;
		private static float _lastRefreshTime = 0f;

		private static string Normalize(string input)
		{
			if (string.IsNullOrEmpty(input)) return "";
			return input.Replace(" ", "").Replace("-", "").Replace("_", "").Replace("(Clone)", "").Trim().ToLowerInvariant();
		}

		public static void Refresh()
		{
			_byName = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
			var allItems = Resources.FindObjectsOfTypeAll<Item>();
			foreach (var it in allItems)
			{
				if (it == null || string.IsNullOrEmpty(it.name)) continue;
				string raw = it.name;
				string clean = raw.Replace("(Clone)", "").Trim();
				string norm = Normalize(raw);

				if (!_byName.ContainsKey(raw)) _byName[raw] = it;
				if (!_byName.ContainsKey(clean)) _byName[clean] = it;
				if (!_byName.ContainsKey(norm)) _byName[norm] = it;

				// Log tous les items pour trouver le nom exact
				if (raw.ToLower().Contains("dart") || raw.ToLower().Contains("blow") || raw.ToLower().Contains("heal") || raw.ToLower().Contains("gun") || raw.ToLower().Contains("pipe") || raw.ToLower().Contains("sarbacane"))
				{
					PeakClassModPlugin.Log?.LogInfo($"[ItemScan] Dart/Weapon candidate: '{raw}' (ID: {it.itemID})");
				}
			}
			_lastRefreshTime = Time.time;
		}

		public static Item Get(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			if (_byName == null || Time.time - _lastRefreshTime > ClassBalance.ITEM_CACHE_DURATION)
			{
				Refresh();
			}

			if (_byName.TryGetValue(name, out var item)) return item;

			string searchNorm = Normalize(name);
			if (_byName.TryGetValue(searchNorm, out item)) return item;

			// Recherche floue
			foreach (var kvp in _byName)
			{
				string keyNorm = Normalize(kvp.Key);
				if (keyNorm.Contains(searchNorm) || searchNorm.Contains(keyNorm))
				{
					return kvp.Value;
				}
			}

			return null;
		}
	}
}