using UnityEngine;
using PeakClassMod.Utils;
using System;

namespace PeakClassMod.Handlers
{
	public class ClassWatcherHandler : MonoBehaviour
	{
		private Character _character;
		private string _activeClass = "None";
		private float _lastCheckTime = 0f;
		private float _lastBroadcastTime = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
			CheckAndApplyClass(true);
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			if (Time.time - _lastCheckTime > 0.5f)
			{
				_lastCheckTime = Time.time;
				CheckAndApplyClass(false);
			}

			// Broadcast périodique toutes les 2s pour s'assurer que tous les clients synchronisent la taille
			if (Time.time - _lastBroadcastTime > 2.0f && _activeClass != "None")
			{
				_lastBroadcastTime = Time.time;
				GetComponent<ClassModNetwork>()?.BroadcastClass(_activeClass);
			}
		}

		public void CheckAndApplyClass(bool force)
		{
			if (_character == null || !_character.IsLocal) return;

			if (HatUtils.TryGetHatName(_character, out string hatName))
			{
				string targetClass = DetermineClassFromHat(hatName);
				if (targetClass != "None" && (force || targetClass != _activeClass))
				{
					_activeClass = targetClass;
					ApplyClass(targetClass);
				}
			}
		}

		private string DetermineClassFromHat(string hatName)
		{
			if (string.IsNullOrEmpty(hatName)) return "None";

			if (hatName.Contains("Scoutmaster")) return "Scout";
			if (hatName.Contains("Medic")) return "Medic";
			if (hatName.Contains("Crab")) return "Tech-Priest";
			if (hatName.Contains("Mushroom")) return "Mushroomancer";
			if (hatName.Contains("Propeller")) return "GreatEpee";
			if (hatName.Contains("NinjaHeadband") || hatName.Contains("Ninja")) return "Rogue";
			if (hatName.Contains("Bandana") || hatName.Contains("Captain")) return "General";
			if (hatName.Contains("Astronaut")) return "Astronaut";
			if (hatName.Contains("Midsummer") || hatName.Contains("Gobelin") || hatName.Contains("Crown")) return "Gobelin";
			if (hatName.Contains("WolfEars") || hatName.Contains("Wolf")) return "Wolf";
			if (hatName.Contains("Chef") || hatName.Contains("Cook")) return "Cook";

			return "None";
		}

		private void ApplyClass(string className)
		{
			PeakClassModPlugin.Log?.LogInfo($"[ClassMod] >>> Activation de la classe : {className} <<<");
			GetComponent<ClassModNetwork>()?.BroadcastClass(className);

			switch (className)
			{
				case "Scout": ClassManager.ActivateScoutClass(); break;
				case "Medic":
					ClassManager.ActivateMedicClass();
					if (_character.refs?.afflictions?.currentStatuses != null)
					{
						int idx = (int)CharacterAfflictions.STATUSTYPE.Curse;
						if (_character.refs.afflictions.currentStatuses[idx] < ClassBalance.MEDIC_CURSE)
							_character.refs.afflictions.currentStatuses[idx] = ClassBalance.MEDIC_CURSE;
					}
					break;
				case "Tech-Priest": ClassManager.ActivateTechPriestClass(); break;
				case "Mushroomancer":
					ClassManager.ActivateMushroomancerClass();
					ClassManager.StartMushroomancerWatch(_character);
					break;
				case "GreatEpee":
					ClassManager.ActivateGreatEpeeClass();
					ClassManager.StartGreatEpeeRoutines(_character);
					break;
				case "Rogue": ClassManager.ActivateRogueClass(); break;
				case "General":
					ClassManager.ActivateGeneralClass();
					if (_character.refs?.afflictions?.currentStatuses != null)
					{
						int idx = (int)CharacterAfflictions.STATUSTYPE.Curse;
						if (_character.refs.afflictions.currentStatuses[idx] < ClassBalance.GENERAL_CURSE_MIN)
							_character.refs.afflictions.currentStatuses[idx] = ClassBalance.GENERAL_CURSE_MIN;
					}
					break;
				case "Astronaut": ClassManager.ActivateAstronautClass(); break;
				case "Gobelin": ClassManager.ActivateGobelinClass(); break;
				case "Wolf": ClassManager.ActivateWolfClass(); break;
				case "Cook": ClassManager.ActivateCookClass(); break;
				default:
					ClassManager.DeactivateAllClasses();
					break;
			}

			try { GUIManager.instance?.bar?.ChangeBar(); } catch { }
		}
	}
}