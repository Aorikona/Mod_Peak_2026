using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Handlers
{
	public class TechPriestWeightEnforcer : MonoBehaviour
	{
		private Character _character;
		private CharacterAfflictions _afflictions;
		private int _weightIndex;

		private void Start()
		{
			_character = GetComponent<Character>();
			_weightIndex = (int)CharacterAfflictions.STATUSTYPE.Weight;
			if (_character?.refs != null) _afflictions = _character.refs.afflictions;
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal || !ClassManager.IsTechPriestActive()) return;
			if (_afflictions == null && _character?.refs != null) _afflictions = _character.refs.afflictions;

			if (_afflictions != null && _afflictions.currentStatuses != null && _afflictions.currentStatuses.Length > _weightIndex)
			{
				_afflictions.currentStatuses[_weightIndex] = 0f;
			}
		}

		private void LateUpdate()
		{
			if (_character == null || !_character.IsLocal || !ClassManager.IsTechPriestActive()) return;
			if (_afflictions != null && _afflictions.currentStatuses != null && _afflictions.currentStatuses.Length > _weightIndex)
			{
				_afflictions.currentStatuses[_weightIndex] = 0f;
			}
		}
	}
}