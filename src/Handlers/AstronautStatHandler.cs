using UnityEngine;
using HarmonyLib;
using System.Collections;
using Peak.Afflictions;

namespace PeakClassMod.Handlers
{
    public class AstronautStatHandler : MonoBehaviour
    {
        private Character _character; private CharacterMovement _movement; private Traverse _travMove;
        private float _defJump = 400f; private float _defSprintMult = 2.0f; private bool _defaultsCaptured = false; private bool _isModified = false;
        private Traverse _jumpImpulseField; private Traverse _sprintMultiplierField; private Traverse _velocityField; private bool _fieldsInitialized = false;

        private void Start() { _character = GetComponent<Character>(); _movement = GetComponent<CharacterMovement>(); if (_movement != null) { _travMove = Traverse.Create(_movement); InitializeFields(); } }
        private void InitializeFields() { if (_fieldsInitialized || _travMove == null) return; _jumpImpulseField = _travMove.Field("jumpImpulse"); _sprintMultiplierField = _travMove.Field("sprintMultiplier"); _velocityField = _travMove.Field("velocity"); _fieldsInitialized = true; }
        private void Update() { if (_character == null || !_character.IsLocal || !ClassManager.IsAstronautActive()) return; if (_character.input.jumpWasPressed && _character.data.isGrounded) { StopAllCoroutines(); StartCoroutine(ApplyWindWalkDelayed()); } }
        private IEnumerator ApplyWindWalkDelayed() { yield return new WaitForSeconds(1f); if (ClassManager.IsAstronautActive() && _character != null && !_character.data.isGrounded) TriggerWindWalk(); }
        private void TriggerWindWalk() { try { _character.refs?.afflictions?.AddAffliction(new Affliction_LowGravity(3, ClassBalance.ASTRONAUT_LOW_GRAV_DURATION), true); } catch { } }
        private void FixedUpdate()
        {
            if (_character == null || !_character.IsLocal || !_fieldsInitialized) return;
            if (ClassManager.IsAstronautActive()) { if (!_defaultsCaptured && !_isModified) { CaptureDefaults(); } ApplyAstronautStats(); _isModified = true; }
            else if (_isModified) { RestoreDefaults(); }
        }
        private void CaptureDefaults() { float? j = _jumpImpulseField?.GetValue<float>(); if (j.HasValue && j.Value > 0) _defJump = j.Value; float? sm = _sprintMultiplierField?.GetValue<float>(); if (sm.HasValue) _defSprintMult = sm.Value; _defaultsCaptured = true; }
        private void ApplyAstronautStats()
        {
            _jumpImpulseField?.SetValue(ClassBalance.ASTRONAUT_JUMP_IMPULSE); _sprintMultiplierField?.SetValue(ClassBalance.ASTRONAUT_SPRINT_MULT);
            if (_character.data.isGrounded) { var list = _character.refs?.afflictions?.afflictionList; if (list != null) for (int i = list.Count - 1; i >= 0; i--) if (list[i] is Affliction_LowGravity) _character.refs.afflictions.RemoveAffliction(list[i]); }
            if (!_character.data.isSprinting && _velocityField != null) { Vector3 currentVel = _velocityField.GetValue<Vector3>(); Vector3 hVel = new Vector3(currentVel.x, 0, currentVel.z); if (hVel.magnitude > ClassBalance.ASTRONAUT_MAX_WALK_SPEED) { Vector3 c = hVel.normalized * ClassBalance.ASTRONAUT_MAX_WALK_SPEED; _velocityField.SetValue(new Vector3(c.x, currentVel.y, c.z)); } }
        }
        private void RestoreDefaults() { _jumpImpulseField?.SetValue(_defJump); _sprintMultiplierField?.SetValue(_defSprintMult); _isModified = false; _defaultsCaptured = false; }
    }
}
