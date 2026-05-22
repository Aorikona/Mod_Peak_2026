using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Handlers
{
    public class TechPriestWeightEnforcer : MonoBehaviour
    {
        private Character _character; private Transform _transform; private CharacterAfflictions _afflictions; private bool _isActive; private int _weightIndex; private float _lastCheck = 0f;
        private void Start() { _character = GetComponent<Character>(); _transform = transform; _weightIndex = (int)CharacterAfflictions.STATUSTYPE.Weight; RefreshState(); }
        private void RefreshState() { _isActive = _character != null && ClassCheckUtil.IsTechPriest(_character); if (_isActive) _afflictions = _character.refs.afflictions; }
        private void LateUpdate()
        {
            if (!_isActive || !ClassManager.IsTechPriestActive()) { if (Time.time - _lastCheck > 2f) { _lastCheck = Time.time; RefreshState(); } return; }
            if (_afflictions != null && _afflictions.currentStatuses[_weightIndex] > 0f) _afflictions.currentStatuses[_weightIndex] = 0f;
        }
    }
}
