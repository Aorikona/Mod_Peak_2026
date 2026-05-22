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
    [HarmonyPatch(typeof(Item), "StartUsePrimary")][HarmonyPriority(HarmonyLib.Priority.Normal)]
    public class TechPriestConsumablePatch1 { 
        static bool Prefix(Item __instance) => !TechPriestItemChecker.ShouldBlockUse(__instance); 
    }

}
