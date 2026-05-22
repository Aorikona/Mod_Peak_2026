using PeakClassMod.Utils;
using System;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Peak.Afflictions.Affliction_FasterBoi), "OnApplied")] 
    public class TechPriestFasterBoiBlock { 
        static bool Prefix(Peak.Afflictions.Affliction_FasterBoi __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); 
    }

}
