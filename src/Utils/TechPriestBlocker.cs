using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Utils
{
	internal static class TechPriestBlocker
	{
		private static readonly string[] BlockedWords = new[]
		{
			"heal", "cure", "medicine", "restorehealth", "bandage", "firstaid",
			"medkit", "marshmallow", "energy", "drink", "soda", "coffee", "potion", "salve", "pill"
		};

		public static bool ShouldBlockItem(Item item)
		{
			if (item == null) return false;

			// 1. Bloque toute nourriture et endurance
			if (item.GetComponent<Action_RestoreHunger>() != null || item.GetComponent<Action_GiveExtraStamina>() != null)
				return true;

			// 2. Bloque les soins et boosts par mots-clés
			string name = item.name.ToLowerInvariant();
			foreach (var word in BlockedWords)
			{
				if (name.Contains(word)) return true;
			}

			// 3. Bloque les afflictions FasterBoi (boissons énergisantes)
			var affAction = item.GetComponent<Action_ApplyAffliction>();
			if (affAction != null && affAction.affliction != null)
			{
				string affName = affAction.affliction.GetType().Name.ToLowerInvariant();
				if (affName.Contains("fasterboi") || affName.Contains("stamina") || affName.Contains("energy"))
					return true;
			}

			return false;
		}
	}
}