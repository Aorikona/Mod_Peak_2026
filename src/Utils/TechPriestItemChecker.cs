using System;
using UnityEngine;

namespace PeakClassMod.Utils
{
	internal static class TechPriestItemChecker
	{
		public static bool ShouldBlockSelfUse(Item item)
		{
			if (item == null || item.holderCharacter == null) return false;
			var ch = item.holderCharacter;

			if ((ch.IsLocal && ClassManager.IsTechPriestActive()) || ClassCheckUtil.IsTechPriest(ch))
			{
				return TechPriestBlocker.ShouldBlockSelfUse(item);
			}

			return false;
		}
	}
}
