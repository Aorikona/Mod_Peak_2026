using System;
using UnityEngine;

namespace PeakClassMod.Utils
{
	public static class GobelinDiet
	{
		private static readonly string[] PackagedKeywords = new[]
		{
			"packaged", "bar", "energybar", "granola", "can", "canned", "chips",
			"cracker", "ration", "soda", "cola", "candy", "chocolate", "cookie", "marshmallow"
		};

		private static readonly string[] NaturalKeywords = new[]
		{
			"shroom", "mushroom", "berry", "insect", "bug", "raw", "fish", "meat", "plant"
		};

		public static bool IsPackagedFood(Item item)
		{
			if (item == null) return false;

			// 1. Tag officiel
			if ((item.itemTags & Item.ItemTags.PackagedFood) != 0)
				return true;

			string name = item.name.ToLowerInvariant();

			// Ne pas bloquer la nourriture naturelle
			foreach (var nat in NaturalKeywords)
			{
				if (name.Contains(nat)) return false;
			}

			// 2. Mots-clés industriels
			foreach (var kw in PackagedKeywords)
			{
				if (name.Contains(kw)) return true;
			}

			return false;
		}
	}
}