using PeakClassMod.Utils;
using System;
using Peak.Afflictions;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterAfflictions), "AddAffliction")]
    public class GobelinSporeAfflictionPatch
    {
        static bool Prefix(CharacterAfflictions __instance, Affliction affliction)
        {
            if (__instance == null || __instance.character == null || affliction == null) return true;

            if (ClassCheckUtil.IsGobelin(__instance.character))
            {
                string name = affliction.GetType().Name;
                // Bloque la confusion et tout ce qui contient "Spore" dans le nom du script
                if (name.Contains("Confus") || name.Contains("Spore"))
                {
                    return false; // Bloque l'effet
                }
            }
            return true;
        }
    }
}
