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
    [HarmonyPatch(typeof(Character), "AddExtraStamina")] 
    public class TechPriestStaminaPatch1 { 
        static bool Prefix(Character __instance) => __instance == null || !ClassCheckUtil.IsTechPriest(__instance); 
    }

}
