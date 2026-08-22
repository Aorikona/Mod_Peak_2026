using System;
using UnityEngine;

namespace PeakClassMod.Utils
{
	internal static class TechPriestBlocker
	{
		private static readonly string[] HealingKeywords = new[]
		{
			"heal", "cure", "medicine", "restorehealth", "bandage", "firstaid",
			"medkit", "salve", "pill", "healingpuff", "cure-all", "cureall"
		};

		public static bool ShouldBlockSelfUse(Item item)
		{
			if (item == null) return false;

			string rawName = (item.name ?? "").ToLowerInvariant();
			string uiName = (item.UIData?.itemName ?? "").ToLowerInvariant();

			// 1. Bloque les items de soin pour soi-même
			foreach (var word in HealingKeywords)
			{
				if (rawName.Contains(word) || uiName.Contains(word))
					return true;
			}

			// 2. Bloque la nourriture organique / naturelle pour soi-même
			if (GobelinDiet.IsNaturalFood(item))
			{
				return true;
			}

			// 3. Bloque toute nourriture consommable directement
			if (GobelinDiet.IsFood(item))
			{
				return true;
			}

			// 4. Bloque les boosts d'endurance et boissons énergisantes
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