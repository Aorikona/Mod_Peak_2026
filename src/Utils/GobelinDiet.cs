using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Utils
{
	public static class GobelinDiet
	{
		// Whitelist explicite d'aliments naturels autorisés pour le Gobelin
		private static readonly HashSet<string> NaturalItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green",
			"ShelfShroom", "BounceShroom", "CloudFungus", "HealingPuffShroom", "PoisonShroom",
			"MagicBean", "HealingDart Variant", "HealingDart", "BalloonBunch", "RopeSpool", "ChainShooter"
		};

		private static readonly string[] NaturalKeywords = new[]
		{
			"shroom", "mushroom", "berry", "shroomberry", "insect", "bug", "raw",
			"fish", "meat", "plant", "herb", "puff", "spore", "healingpuff", "bean", "magicbean",
			"worm", "fly", "centipede", "beetle", "grub", "root", "seed", "fern", "flower", "cactus", "aloe", "bird", "egg"
		};

		// Blacklist explicite de nourriture industrielle / transformée
		private static readonly string[] PackagedKeywords = new[]
		{
			"packaged", "bar", "energybar", "granola", "can", "canned", "chips",
			"cracker", "ration", "soda", "cola", "candy", "chocolate", "cookie",
			"marshmallow", "pie", "drink", "snack", "soup", "tuna", "sardine",
			"noodles", "mre", "bread", "energydrink", "popcorn", "crisps", "pastry",
			"meatpie", "peanuts", "nuts", "jerky", "beef", "cheese", "sausage", "bacon",
			"sandwich", "burger", "hotdog", "pizza", "pasta", "ramen", "cereal", "gum", "cake"
		};

		public static bool IsNaturalFood(Item item)
		{
			if (item == null) return false;

			string rawName = (item.name ?? "").Replace("(Clone)", "").Trim().ToLowerInvariant();
			string uiName = (item.UIData?.itemName ?? "").ToLowerInvariant();

			if (NaturalItemNames.Contains(item.name) || NaturalItemNames.Contains(rawName))
				return true;

			if ((item.itemTags & Item.ItemTags.Berry) != 0 || (item.itemTags & Item.ItemTags.Mushroom) != 0 || (item.itemTags & Item.ItemTags.Bird) != 0)
				return true;

			foreach (var nat in NaturalKeywords)
			{
				if (rawName.Contains(nat) || uiName.Contains(nat))
					return true;
			}

			return false;
		}

		public static bool IsFood(Item item)
		{
			if (item == null) return false;

			if ((item.itemTags & Item.ItemTags.PackagedFood) != 0)
				return true;

			var playAnim = item.GetComponent<Action_PlayAnimation>();
			if (playAnim != null && !string.IsNullOrEmpty(playAnim.animationName))
			{
				string a = playAnim.animationName.ToLowerInvariant();
				if (a.Contains("eat") || a.Contains("drink") || a.Contains("consume")) return true;
			}

			var feedback = item.GetComponent<ItemUseFeedback>();
			if (feedback != null && !string.IsNullOrEmpty(feedback.useAnimation))
			{
				string a = feedback.useAnimation.ToLowerInvariant();
				if (a.Contains("eat") || a.Contains("drink") || a.Contains("consume")) return true;
			}

			var modStatuses = item.GetComponents<Action_ModifyStatus>();
			foreach (var ms in modStatuses)
			{
				if (ms.statusType == CharacterAfflictions.STATUSTYPE.Hunger) return true;
			}

			if (item.GetComponent<Action_RestoreHunger>() != null)
				return true;

			return false;
		}

		public static bool IsPackagedFood(Item item)
		{
			if (item == null) return false;

			// 1. Si c'est un aliment naturel, JAMAIS interdit
			if (IsNaturalFood(item))
				return false;

			// 2. Si c'est de la nourriture consommable et pas naturelle, INTERDIT
			if (IsFood(item))
				return true;

			// 3. Mots-clés industriels / emballés
			string rawName = (item.name ?? "").Replace("(Clone)", "").Trim().ToLowerInvariant();
			string uiName = (item.UIData?.itemName ?? "").ToLowerInvariant();

			foreach (var kw in PackagedKeywords)
			{
				if (rawName.Contains(kw) || uiName.Contains(kw))
					return true;
			}

			return false;
		}
	}
}