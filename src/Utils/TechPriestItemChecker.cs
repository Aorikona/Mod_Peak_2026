using PeakClassMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

namespace PeakClassMod.Utils
{
    internal static class TechPriestItemChecker
    {
        public static bool ShouldBlockUse(Item item)
        {
            if (item == null || item.holderCharacter == null || !ClassCheckUtil.IsTechPriest(item.holderCharacter)) return false;
            if (TechPriestBlocker.ShouldBlockItem(item)) return true;
            var a = item.GetComponent<Action_ApplyAffliction>();
            return a?.affliction != null && a.affliction.GetType().Name.Contains("FasterBoi");
        }
    }
}
