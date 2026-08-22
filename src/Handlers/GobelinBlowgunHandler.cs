using UnityEngine;
using HarmonyLib;
using PeakClassMod.Utils;
using PeakClassMod.Patches;
using Zorro.Core;

namespace PeakClassMod.Handlers
{
	public class GobelinBlowgunHandler : MonoBehaviour
	{
		private Character _character;
		private bool _isCharging = false;
		private float _chargeTimer = 0f;
		private string _currentAnim = null;

		private void Start()
		{
			_character = GetComponent<Character>();
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;
			if (!ClassManager.IsGobelinActive() && !ClassCheckUtil.IsGobelin(_character)) return;

			Item currentItem = _character.data?.currentItem;
			bool holdingBlowgun = currentItem != null && (currentItem.itemID == 70 || GobelinBlowgunEnforcer.IsBlowgun(currentItem));

			if (holdingBlowgun)
			{
				// Verrouille l'usage infini sans bloquer le drop de slot
				currentItem.totalUses = -1;
				Traverse.Create(currentItem).Field("overrideUsability").SetValue(Optionable<bool>.Some(true));

				// Désactive tous les composants de consommation
				var consumes = currentItem.GetComponents<Action_Consume>();
				foreach (var c in consumes) { if (c.enabled) c.enabled = false; }
				var reduces = currentItem.GetComponents<Action_ReduceUses>();
				foreach (var r in reduces) { if (r.enabled) r.enabled = false; }

				// Récupère les feedbacks d'animation et de temps de tir
				float usingTime = currentItem.usingTimePrimary > 0f ? currentItem.usingTimePrimary : 0.8f;
				ItemUseFeedback feedback = currentItem.GetComponent<ItemUseFeedback>();
				string anim = feedback != null && !string.IsNullOrEmpty(feedback.useAnimation) ? feedback.useAnimation : "PlayerEat";

				// A. Début de la charge du tir
				if (_character.input != null && _character.input.usePrimaryWasPressed && !_isCharging)
				{
					_isCharging = true;
					_chargeTimer = 0f;
					_currentAnim = anim;
					if (_character.refs?.animator != null && !string.IsNullOrEmpty(_currentAnim))
					{
						_character.refs.animator.SetBool(_currentAnim, true);
					}
				}

				// B. Maintien de la charge
				if (_isCharging && _character.input != null && _character.input.usePrimaryIsPressed)
				{
					_chargeTimer += Time.deltaTime;
					float prog = Mathf.Clamp01(_chargeTimer / usingTime);
					Traverse.Create(currentItem).Field("overrideProgress").SetValue(prog);
					Traverse.Create(currentItem).Field("overrideForceProgress").SetValue(true);

					if (_chargeTimer >= usingTime)
					{
						// Charge complète : TIR !
						_isCharging = false;
						_chargeTimer = 0f;
						Traverse.Create(currentItem).Field("overrideProgress").SetValue(0f);
						Traverse.Create(currentItem).Field("overrideForceProgress").SetValue(false);

						if (_character.refs?.animator != null && !string.IsNullOrEmpty(_currentAnim))
						{
							_character.refs.animator.SetBool(_currentAnim, false);
						}

						if (feedback != null && feedback.sfxUsed != null && _character.refs != null)
						{
							feedback.sfxUsed.Play(currentItem.transform.position, _character.refs.carryPosRef);
						}

						ShootBlowgun(currentItem);
					}
				}

				// C. Annulation si on relâche le clic avant la fin
				if (_isCharging && (_character.input == null || _character.input.usePrimaryWasReleased || _character.data.passedOut || _character.data.fullyPassedOut))
				{
					_isCharging = false;
					_chargeTimer = 0f;
					Traverse.Create(currentItem).Field("overrideProgress").SetValue(0f);
					Traverse.Create(currentItem).Field("overrideForceProgress").SetValue(false);

					if (_character.refs?.animator != null && !string.IsNullOrEmpty(_currentAnim))
					{
						_character.refs.animator.SetBool(_currentAnim, false);
					}
				}
			}
			else
			{
				if (_isCharging)
				{
					_isCharging = false;
					_chargeTimer = 0f;
					if (_character.refs?.animator != null && !string.IsNullOrEmpty(_currentAnim))
					{
						_character.refs.animator.SetBool(_currentAnim, false);
					}
				}

				// 3. Verrouille immédiatement la nourriture industrielle pour le Gobelin
				if (currentItem != null && GobelinDiet.IsPackagedFood(currentItem))
				{
					Traverse.Create(currentItem).Field("overrideUsability").SetValue(Optionable<bool>.Some(false));
				}
			}
		}

		private void ShootBlowgun(Item item)
		{
			var dartAction = item.GetComponent<Action_RaycastDart>() ?? item.GetComponentInChildren<Action_RaycastDart>();
			if (dartAction != null)
			{
				dartAction.RunAction();
				PeakClassModPlugin.Log?.LogInfo("[Blowgun] Fired dart successfully!");
			}

			GobelinBlowgunEnforcer.ApplyGobelinCurse(_character);

			item.finishedCast = false;
			item.isUsingPrimary = false;
		}
	}
}