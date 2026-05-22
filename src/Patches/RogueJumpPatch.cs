using PeakClassMod;
using System;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
    public class RogueJumpPatch { 
        static void Postfix(CharacterMovement __instance) { 
            var ch = __instance?.GetComponent<Character>(); 
            if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive) ch.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; 
        } 
    }

}
