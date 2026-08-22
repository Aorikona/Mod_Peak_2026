using PeakClassMod;
using PeakClassMod.Utils;
using UnityEngine;
using HarmonyLib;

namespace PeakClassMod.Patches
{
	[HarmonyPatch(typeof(CharacterAfflictions), "UpdateExtraJumps")]
	public static class RogueUpdateExtraJumpsPatch
	{
		[HarmonyPostfix]
		public static void Postfix(CharacterAfflictions __instance)
		{
			if (__instance == null || __instance.character == null) return;
			var ch = __instance.character;
			if (ch.IsLocal && (PeakClassModPlugin.rogueActive || ClassCheckUtil.IsRogueHat(ch)))
			{
				__instance.totalExtraJumps = 99;
				if (ch.data != null) ch.data.extraJumps = 99;
			}
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
	public static class RogueTryToJumpPatch
	{
		[HarmonyPrefix]
		public static void Prefix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>() 
				?? __instance?.GetComponentInParent<Character>() 
				?? Character.localCharacter;

			if (ch != null && ch.IsLocal && (PeakClassModPlugin.rogueActive || ClassCheckUtil.IsRogueHat(ch)))
			{
				if (ch.data != null && ch.data.currentStamina > 0.005f && !ch.data.passedOut && !ch.data.fullyPassedOut)
				{
					ch.data.extraJumps = 99;
					ch.data.chargingJump = false;
					if (ch.data.sinceJump < 0.1f) ch.data.sinceJump = 0.12f;
					if (ch.refs?.afflictions != null) ch.refs.afflictions.totalExtraJumps = 99;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "UpdateMovement")]
	public static class RogueUpdateMovementPatch
	{
		[HarmonyPrefix]
		public static void Prefix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>() 
				?? __instance?.GetComponentInParent<Character>() 
				?? Character.localCharacter;

			if (ch != null && ch.IsLocal && (PeakClassModPlugin.rogueActive || ClassCheckUtil.IsRogueHat(ch)))
			{
				if (ch.data != null && ch.data.currentStamina > 0.005f && !ch.data.passedOut && !ch.data.fullyPassedOut)
				{
					ch.data.extraJumps = 99;
					if (ch.refs?.afflictions != null) ch.refs.afflictions.totalExtraJumps = 99;
				}
			}
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
	public static class RogueGroundCheckPatch
	{
		[HarmonyPostfix]
		public static void Postfix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>() 
				?? __instance?.GetComponentInParent<Character>() 
				?? Character.localCharacter;

			if (ch != null && ch.IsLocal && (PeakClassModPlugin.rogueActive || ClassCheckUtil.IsRogueHat(ch)))
			{
				if (ch.data != null && ch.data.currentStamina > 0.005f && !ch.data.passedOut && !ch.data.fullyPassedOut)
				{
					ch.data.extraJumps = 99;
					if (ch.refs?.afflictions != null) ch.refs.afflictions.totalExtraJumps = 99;
				}
			}
		}
	}
}
