using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(Peak.Afflictions.Affliction_PoisonOverTime), "OnApplied")]
    public class PoisonPatch { 
        static bool Prefix(Peak.Afflictions.Affliction_PoisonOverTime __instance) => __instance?.character == null || !ClassCheckUtil.IsMushroom(__instance.character); 
    }

}
