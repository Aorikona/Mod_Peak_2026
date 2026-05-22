using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mod_Peak_2026.src.Patches
{

    using PeakClassMod.Utils;
    using UnityEngine;

    [HarmonyPatch(typeof(Backpack), "Wear")]
    public class BackpackBlocker
    {
        static bool Prefix(Character interactor)
        {
            if (ClassCheckUtil.IsWolf(interactor)) {
                Debug.LogWarning("Cannot carry backpacks!");
                return false;
            }
            return true; 
        }

    }
}
