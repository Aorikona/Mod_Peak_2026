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

        public static void ResetAllFlags()
        {
            var lc = Character.localCharacter;
            if (lc != null)
            {
                if (_mushroomCoroutine != null) lc.StopCoroutine(_mushroomCoroutine);
                if (_greatEpeeEffectsCoroutine != null) lc.StopCoroutine(_greatEpeeEffectsCoroutine);
                if (_greatEpeeStaminaCoroutine != null) lc.StopCoroutine(_greatEpeeStaminaCoroutine);
                lc.transform.localScale = Vector3.one;
            }
            PeakClassModPlugin.scoutActive = false; PeakClassModPlugin.medicActive = false; PeakClassModPlugin.techPriestActive = false;
            PeakClassModPlugin.mushroomancerActive = false; PeakClassModPlugin.greatEpeeActive = false; PeakClassModPlugin.rogueActive = false;
            PeakClassModPlugin.generalActive = false; PeakClassModPlugin.astronautActive = false; PeakClassModPlugin.gobelinActive = false;
            PeakClassModPlugin.dualNatureActive = false;
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
        public static void ActivateDualNatureClass() { ResetAllFlags(); PeakClassModPlugin.dualNatureActive = true; PeakClassModPlugin.currentClass = "DualNature"; }

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
        public static bool IsDualNatureActive() => PeakClassModPlugin.dualNatureActive;

        private static readonly string[] ZeroStaminaFungi = new[] { "ShelfShroom", "BounceShroom", "CloudFungus" };

        public static void StartMushroomancerWatch(Character character) { if (character != null) _mushroomCoroutine = character.StartCoroutine(MushroomancerLocalStaminaWatch(character)); }

        public static void StartGreatEpeeRoutines(Character character)
        {
            if (character != null)
            {
                _greatEpeeEffectsCoroutine = character.StartCoroutine(GreatEpeePermanentEffects(character));
                _greatEpeeStaminaCoroutine = character.StartCoroutine(GreatEpeeStaminaRoutine(character));
            }
        }

        // --- CORRECTION MUSHROOMANCER ---
        public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
        {
            while (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
            {
                // Vérification basée sur le code source DnSpy :
                // On vérifie si le joueur est évanoui (PassedOut) OU mort.
                bool isKnockedOut = false;

                if (character.data != null)
                {
                    if (character.data.passedOut || character.data.fullyPassedOut || character.data.dead)
                    {
                        isKnockedOut = true;
                    }
                }

                // On applique la punition SEULEMENT si le joueur est conscient (debout)
                if (!isKnockedOut && character.data.currentStamina <= 0.02f)
                {
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

            // On incrémente le compteur
            if (!staminaZeroCount.ContainsKey(actor)) staminaZeroCount[actor] = 0;
            staminaZeroCount[actor]++;

            // "gives 2% curse every second time!"
            // Donc une fois sur deux (les nombres pairs : 2, 4, 6...)
            if (staminaZeroCount[actor] % 2 == 0)
            {
                character.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MUSHROOM_CURSE_AMOUNT, false);
            }

            string randomName = ZeroStaminaFungi[UnityEngine.Random.Range(0, ZeroStaminaFungi.Length)];
            Item fungusItem = ItemCache.Get(randomName);
            if (fungusItem == null) return;
            ushort id = (ushort)fungusItem.itemID;

            // Retarder SEULEMENT le don de l'objet via coroutine
            if (character != null)
            {
                character.StartCoroutine(MushroomancerGiveItemDelayed(character, id));
            }
        }

        private static IEnumerator MushroomancerGiveItemDelayed(Character character, ushort itemId)
        {
            // Wait maximum of 5 seconds to avoid memory leaks
            float timeout = 5f;
            while (character != null && character.data != null && (!character.data.isGrounded || character.data.isSprinting) && timeout > 0)
            {
                timeout -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (character != null && character.player != null && character.player.itemSlots != null)
            {
                // On vérifie MANUELLEMENT les 3 slots de base (0, 1, 2)
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

                // On ne donne l'objet QUE si on a de la place dans les slots principaux
                if (hasSpaceInBaseSlots && character.player.HasEmptySlot(itemId))
                {
                    character.player.AddItem(itemId, null, out _);
                }
                // Sinon, l'inventaire principal est plein. L'objet ne sera PAS forcé dans le slot 4 (les mains).
            }
        }

        public static IEnumerator GreatEpeePermanentEffects(Character character)
        {
            while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
            {
                bool hasFaster = false;
                var list = character.refs?.afflictions?.afflictionList;
                if (list != null) { for (int i = 0; i < list.Count; i++) if (list[i].GetType().Name.Contains("FasterBoi")) { hasFaster = true; break; } }
                if (!hasFaster) character.refs.afflictions.AddAffliction(new Peak.Afflictions.Affliction_FasterBoi { totalTime = ClassBalance.GREATEPEE_EFFECT_DURATION }, false);
                yield return new WaitForSeconds(2f);
            }
        }

        public static IEnumerator GreatEpeeStaminaRoutine(Character character)
        {
            yield return new WaitForSeconds(5f);
            while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
            {
                while (character?.refs?.afflictions != null && character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0.05f)
                {
                    yield return new WaitForSeconds(1f);
                    if (!PeakClassModPlugin.greatEpeeActive) yield break;
                }
                if (character == null) yield break;
                yield return new WaitForSeconds(UnityEngine.Random.Range(ClassBalance.GREATEPEE_SLEEP_MIN, ClassBalance.GREATEPEE_SLEEP_MAX));
                if (character?.refs?.afflictions == null || !PeakClassModPlugin.greatEpeeActive) yield break;
                if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) < 0.1f)
                    character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);
            }
        }
    }
}
