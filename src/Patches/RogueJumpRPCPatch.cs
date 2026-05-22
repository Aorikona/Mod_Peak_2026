using PeakClassMod;
using System;
using UnityEngine;
using Photon.Pun;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
    public class RogueJumpRPCPatch
    {
        static bool Prefix(CharacterMovement __instance)
        {
            var ch = __instance?.GetComponent<Character>();
            if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
            {
                // Remove the jumpsRemaining limit for Rogue. They just need stamina.
                if (ch.data.currentStamina > 0.005f)
                {
                    // Maintain jumpsRemaining at 1 so the game allows the jump
                    ch.data.jumpsRemaining = 1;
                    ch.data.chargingJump = true;
                    ch.refs?.view?.RPC("JumpRpc", RpcTarget.All, new object[] { false });
                    return false;
                }
            }
            return true;
        }
    }
}
