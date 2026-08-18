using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod
{
	public static class ClassManager
	{
		private static readonly Dictionary<int, float> lastStaminaZeroTime = new Dictionary<int, float>();
		private static readonly Dictionary<int, int> staminaZeroCount = new Dictionary<int, int>();
		private static Coroutine _mushroomCoroutine;
		private static Coroutine _greatEpeeEffectsCoroutine;
		private static Coroutine _greatEpeeStaminaCoroutine;
		private static bool _greatEpeeNeedsRecovery = false;

		public static void ResetAllFlags()
		{
			var lc = Character.localCharacter;
			if (lc != null)
			{
				if (_mushroomCoroutine != null) { lc.StopCoroutine(_mushroomCoroutine); _mushroomCoroutine = null; }
				if (_greatEpeeEffectsCoroutine != null) { lc.StopCoroutine(_greatEpeeEffectsCoroutine); _greatEpeeEffectsCoroutine = null; }
				if (_greatEpeeStaminaCoroutine != null) { lc.StopCoroutine(_greatEpeeStaminaCoroutine); _greatEpeeStaminaCoroutine = null; }
				RemoveFasterBoi(lc);
				lc.transform.localScale = Vector3.one;
			}
			_greatEpeeNeedsRecovery = false;
			PeakClassModPlugin.scoutActive = false; PeakClassModPlugin.medicActive = false; PeakClassModPlugin.techPriestActive = false;
			PeakClassModPlugin.mushroomancerActive = false; PeakClassModPlugin.greatEpeeActive = false; PeakClassModPlugin.rogueActive = false;
			PeakClassModPlugin.generalActive = false; PeakClassModPlugin.astronautActive = false; PeakClassModPlugin.gobelinActive = false;
			PeakClassModPlugin.wolfActive = false;
			PeakClassModPlugin.currentClass = "None";
		}

		public static void ActivateScoutClass() { ResetAllFlags(); PeakClassModPlugin.scoutActive = true; PeakClassModPlugin.currentClass = "Scout"; }
		public static void ActivateMedicClass() { ResetAllFlags(); PeakClassModPlugin.medicActive = true; PeakClassModPlugin.currentClass = "Medic"; }
		public static void ActivateTechPriestClass() { ResetAllFlags(); PeakClassModPlugin.techPriestActive = true; PeakClassModPlugin.currentClass = "Tech-Priest"; }
		public static void ActivateMushroomancerClass() { ResetAllFlags(); PeakClassModPlugin.mushroomancerActive = true; PeakClassModPlugin.currentClass = "Mushroomancer"; }
		public static void ActivateGreatEpeeClass() { ResetAllFlags(); PeakClassModPlugin.greatEpeeActive = true; PeakClassModPlugin.currentClass = "GreatEpee"; }
		public static void ActivateRogueClass() { ResetAllFlags(); PeakClassModPlugin.rogueActive = true; PeakClassModPlugin.currentClass = "Rogue"; if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; }
		public static void ActivateGeneralClass() { ResetAllFlags(); PeakClassModPlugin.generalActive = true; PeakClassModPlugin.currentClass = "General"; }
		public static void ActivateAstronautClass() { ResetAllFlags(); PeakClassModPlugin.astronautActive = true; PeakClassModPlugin.currentClass = "Astronaut"; }
		public static void ActivateGobelinClass() { ResetAllFlags(); PeakClassModPlugin.gobelinActive = true; PeakClassModPlugin.currentClass = "Gobelin"; }
		public static void ActivateWolfClass() { ResetAllFlags(); PeakClassModPlugin.wolfActive = true; PeakClassModPlugin.currentClass = "Wolf"; }

		public static void DeactivateAllClasses() { ResetAllFlags(); if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = 1; }

		public static bool IsScoutActive() => PeakClassModPlugin.scoutActive;
		public static bool IsMedicActive() => PeakClassModPlugin.medicActive;
		public static bool IsTechPriestActive() => PeakClassModPlugin.techPriestActive;
		public static bool IsMushroomancerActive() => PeakClassModPlugin.mushroomancerActive;
		public static bool IsGreatEpeeActive() => PeakClassModPlugin.greatEpeeActive;
		public static bool IsRogueActive() => PeakClassModPlugin.rogueActive;
		public static bool IsGeneralActive() => PeakClassModPlugin.generalActive;
		public static bool IsAstronautActive() => PeakClassModPlugin.astronautActive;
		public static bool IsGobelinActive() => PeakClassModPlugin.gobelinActive;
		public static bool IsWolfActive() => PeakClassModPlugin.wolfActive;

		private static readonly string[] ZeroStaminaFungi = new[] { "ShelfShroom", "BounceShroom", "CloudFungus" };

		public static void StartMushroomancerWatch(Character character)
		{
			if (character != null && _mushroomCoroutine == null)
				_mushroomCoroutine = character.StartCoroutine(MushroomancerLocalStaminaWatch(character));
		}

		public static void StartGreatEpeeRoutines(Character character)
		{
			if (character != null)
			{
				if (_greatEpeeEffectsCoroutine == null)
					_greatEpeeEffectsCoroutine = character.StartCoroutine(GreatEpeePermanentEffects(character));
				if (_greatEpeeStaminaCoroutine == null)
					_greatEpeeStaminaCoroutine = character.StartCoroutine(GreatEpeeStaminaRoutine(character));
			}
		}

		public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
		{
			// Délai initial de 4s pour laisser le joueur charger son endurance à 100%
			yield return new WaitForSeconds(4f);

			bool wasAboveThreshold = true;

			while (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
			{
				bool isKnockedOut = false;
				if (character.data != null)
				{
					if (character.data.passedOut || character.data.fullyPassedOut || character.data.dead)
						isKnockedOut = true;
				}

				float currentStam = character.data != null ? character.data.currentStamina : 1f;

				if (currentStam > 0.20f)
				{
					wasAboveThreshold = true;
				}

				// Ne donne un champignon QUE si le joueur avait de l'endurance et vient de la vider
				if (!isKnockedOut && wasAboveThreshold && currentStam <= 0.02f)
				{
					wasAboveThreshold = false;
					TryActivateStaminaZeroMechanic(character);
				}

				yield return new WaitForSeconds(0.1f);
			}
		}

		public static void TryActivateStaminaZeroMechanic(Character character)
		{
			if (character?.photonView?.Owner == null) return;
			int actor = character.photonView.Owner.ActorNumber;
			if (lastStaminaZeroTime.TryGetValue(actor, out var last) && Time.time - last < ClassBalance.MUSHROOM_STAMINA_COOLDOWN) return;
			lastStaminaZeroTime[actor] = Time.time;

			if (!staminaZeroCount.ContainsKey(actor)) staminaZeroCount[actor] = 0;
			staminaZeroCount[actor]++;

			if (staminaZeroCount[actor] % 2 == 0)
			{
				character.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MUSHROOM_CURSE_AMOUNT, false);
			}

			string randomName = ZeroStaminaFungi[UnityEngine.Random.Range(0, ZeroStaminaFungi.Length)];
			Item fungusItem = ItemCache.Get(randomName);
			if (fungusItem == null) return;
			ushort id = (ushort)fungusItem.itemID;

			if (character != null)
			{
				character.StartCoroutine(MushroomancerGiveItemDelayed(character, id));
			}
		}

		private static IEnumerator MushroomancerGiveItemDelayed(Character character, ushort itemId)
		{
			float timeout = 5f;
			while (character != null && character.data != null && (!character.data.isGrounded || character.data.isSprinting) && timeout > 0)
			{
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			if (character != null && character.player != null && character.player.itemSlots != null)
			{
				bool hasSpaceInBaseSlots = false;
				int maxSlotsToCheck = Mathf.Min(3, character.player.itemSlots.Length);

				for (int i = 0; i < maxSlotsToCheck; i++)
				{
					if (character.player.itemSlots[i] != null && character.player.itemSlots[i].IsEmpty())
					{
						hasSpaceInBaseSlots = true;
						break;
					}
				}

				if (hasSpaceInBaseSlots && character.player.HasEmptySlot(itemId))
				{
					character.player.AddItem(itemId, null, out _);
				}
			}
		}

		public static void RemoveFasterBoi(Character character)
		{
			try
			{
				var list = character.refs?.afflictions?.afflictionList;
				if (list != null)
				{
					for (int i = list.Count - 1; i >= 0; i--)
					{
						if (list[i] != null && list[i].GetType().Name.Contains("FasterBoi"))
						{
							list.RemoveAt(i);
						}
					}
				}
			}
			catch { }
		}

		public static IEnumerator GreatEpeePermanentEffects(Character character)
		{
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				float currentDrowsy = character.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) ?? 0f;

				bool isRestricted = _greatEpeeNeedsRecovery
					|| (character.data != null && (character.data.passedOut || character.data.fullyPassedOut))
					|| currentDrowsy > 0.20f;

				if (!isRestricted)
				{
					bool hasFaster = false;
					var list = character.refs?.afflictions?.afflictionList;
					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							if (list[i] != null && list[i].GetType().Name.Contains("FasterBoi"))
							{
								hasFaster = true;
								break;
							}
						}
					}

					if (!hasFaster && character.refs?.afflictions != null)
					{
						character.refs.afflictions.AddAffliction(new Peak.Afflictions.Affliction_FasterBoi
						{
							moveSpeedMod = 0.5f,
							climbSpeedMod = 1.0f,
							totalTime = ClassBalance.GREATEPEE_EFFECT_DURATION
						}, false);
					}
				}
				else
				{
					RemoveFasterBoi(character);
				}

				yield return new WaitForSeconds(0.4f);
			}
		}

		public static IEnumerator GreatEpeeStaminaRoutine(Character character)
		{
			yield return new WaitForSeconds(5f);

			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				float sleepDelay = UnityEngine.Random.Range(ClassBalance.GREATEPEE_SLEEP_MIN, ClassBalance.GREATEPEE_SLEEP_MAX);
				yield return new WaitForSeconds(sleepDelay);

				if (!PeakClassModPlugin.greatEpeeActive || character == null || character.refs?.afflictions == null)
					yield break;

				_greatEpeeNeedsRecovery = true;
				RemoveFasterBoi(character);

				character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);

				yield return new WaitForSeconds(0.5f);
				if (character != null && character.data != null && !character.data.dead)
				{
					character.data.passedOut = true;
					character.data.fullyPassedOut = true;
				}

				while (character != null && character.data != null && (character.data.passedOut || character.data.fullyPassedOut))
				{
					yield return new WaitForSeconds(0.5f);
				}

				while (character != null && character.refs?.afflictions != null && character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0.20f)
				{
					yield return new WaitForSeconds(0.2f);
				}

				_greatEpeeNeedsRecovery = false;
				yield return new WaitForSeconds(1f);
			}
		}
	}
}