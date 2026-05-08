using UnityEngine;
using HarmonyLib;
using System.Collections;
using Peak.Afflictions;

namespace PeakClassMod.Handlers
{
    public class AstronautStatHandler : MonoBehaviour
    {
        private Character _character; private CharacterMovement _movement;
        private float _defJump = 400f; private float _defSprintMult = 2.0f; private bool _defaultsCaptured = false; private bool _isModified = false;
        private static System.Reflection.FieldInfo _jumpImpulseField; private static System.Reflection.FieldInfo _sprintMultiplierField; private static System.Reflection.FieldInfo _velocityField; private static bool _fieldsInitialized = false;

        private void Start() { _character = GetComponent<Character>(); _movement = GetComponent<CharacterMovement>(); if (_movement != null) { InitializeFields(); } }
        private void InitializeFields() { if (_fieldsInitialized) return; _jumpImpulseField = AccessTools.Field(typeof(CharacterMovement), "jumpImpulse"); _sprintMultiplierField = AccessTools.Field(typeof(CharacterMovement), "sprintMultiplier"); _velocityField = AccessTools.Field(typeof(CharacterMovement), "velocity"); _fieldsInitialized = true; }
        private void Update() { if (_character == null || !_character.IsLocal || !ClassManager.IsAstronautActive()) return; if (_character.input.jumpWasPressed && _character.data.isGrounded) { StopAllCoroutines(); StartCoroutine(ApplyWindWalkDelayed()); } }
        private IEnumerator ApplyWindWalkDelayed() { yield return new WaitForSeconds(1f); if (ClassManager.IsAstronautActive() && _character != null && !_character.data.isGrounded) TriggerWindWalk(); }
        private void TriggerWindWalk() { try { _character.refs?.afflictions?.AddAffliction(new Affliction_LowGravity(3, ClassBalance.ASTRONAUT_LOW_GRAV_DURATION), true); } catch (System.Exception ex) { PeakClassModPlugin.Log?.LogError(ex); } }
        private void FixedUpdate()
        {
            if (_character == null || !_character.IsLocal || !_fieldsInitialized) return;
            if (ClassManager.IsAstronautActive()) { if (!_defaultsCaptured && !_isModified) { CaptureDefaults(); } ApplyAstronautStats(); _isModified = true; }
            else if (_isModified) { RestoreDefaults(); }
        }
        private void CaptureDefaults() { if (_jumpImpulseField != null) { float j = (float)_jumpImpulseField.GetValue(_movement); if (j > 0) _defJump = j; } if (_sprintMultiplierField != null) { _defSprintMult = (float)_sprintMultiplierField.GetValue(_movement); } _defaultsCaptured = true; }
        private void ApplyAstronautStats()
        {
            _jumpImpulseField?.SetValue(_movement, ClassBalance.ASTRONAUT_JUMP_IMPULSE); _sprintMultiplierField?.SetValue(_movement, ClassBalance.ASTRONAUT_SPRINT_MULT);
            if (_character.data.isGrounded) { var list = _character.refs?.afflictions?.afflictionList; if (list != null) for (int i = list.Count - 1; i >= 0; i--) if (list[i] is Affliction_LowGravity) _character.refs.afflictions.RemoveAffliction(list[i]); }
            if (!_character.data.isSprinting && _velocityField != null) { Vector3 currentVel = (Vector3)_velocityField.GetValue(_movement); Vector3 hVel = new Vector3(currentVel.x, 0, currentVel.z); if (hVel.magnitude > ClassBalance.ASTRONAUT_MAX_WALK_SPEED) { Vector3 c = hVel.normalized * ClassBalance.ASTRONAUT_MAX_WALK_SPEED; _velocityField.SetValue(_movement, new Vector3(c.x, currentVel.y, c.z)); } }
        }
        private void RestoreDefaults() { _jumpImpulseField?.SetValue(_movement, _defJump); _sprintMultiplierField?.SetValue(_movement, _defSprintMult); _isModified = false; _defaultsCaptured = false; }
    }
}
