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
		private int _notFoundCounter = 0;

		private void Start()
		{
			_character = GetComponent<Character>();
			CheckAndApplyClass(true);
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			if (Time.time - _lastCheckTime < 0.5f) return;
			_lastCheckTime = Time.time;

			CheckAndApplyClass(false);
		}

		public void CheckAndApplyClass(bool force)
		{
			if (_character == null || !_character.IsLocal) return;

			if (HatUtils.TryGetHatName(_character, out string hatName))
			{
				_notFoundCounter = 0;
				string targetClass = DetermineClassFromHat(hatName);
				if (force || targetClass != _activeClass)
				{
					_activeClass = targetClass;
					ApplyClass(targetClass);
				}
			}
			else
			{
				_notFoundCounter++;
				if (_notFoundCounter >= 3 && _activeClass != "None")
				{
					_activeClass = "None";
					ClassManager.DeactivateAllClasses();
					GetComponent<ClassModNetwork>()?.BroadcastClass("None");
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
			if (hatName.Contains("Midsummer") || hatName.Contains("Gobelin")) return "Gobelin";
			if (hatName.Contains("WolfEars") || hatName.Contains("Wolf")) return "Wolf";

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
				default:
					ClassManager.DeactivateAllClasses();
					break;
			}

			try { GUIManager.instance?.bar?.ChangeBar(); } catch { }
		}
	}
}