using UnityEngine;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using PeakClassMod;
using PeakClassMod.Utils;
using Peak.Afflictions;

namespace PeakClassMod.Handlers
{
    public class DualNatureHandler : MonoBehaviour
    {
        // Event for external scripts to play sounds (e.g. Wolf Howl for night)
        public static event System.Action<bool, Character> OnDualNatureStateChanged;

        public AudioClip dayTransitionSound;
        public AudioClip nightTransitionSound;
        private AudioSource _audioSource;

        private Character _character;
        private CharacterMovement _movement;
        private Traverse _travMove;
        private Traverse _jumpImpulseField;
        private Traverse _sprintMultiplierField;
        private Traverse _velocityField;
        private Traverse _climbSpeedField;
        private Traverse _wallAdhesionField;

        private float _defJump = 400f;
        private float _defSprintMult = 2.0f;
        private float _defClimbSpeed = 1f;
        private float _defWallAdhesion = 1f;
        private bool _fieldsInitialized = false;
        private bool _defaultsCaptured = false;
        private bool _isModified = false;
        private bool _lastStateWasDay = false;
        private bool _lastStateGrounded = false;

        private float _lastAuraTime = 0f;
        private float _lastBagCheckTime = 0f;
        private Collider[] _auraColliders = new Collider[20];

        private void Start()
        {
            _character = GetComponent<Character>();
            _movement = GetComponent<CharacterMovement>();

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            if (_movement != null)
            {
                _travMove = Traverse.Create(_movement);
                InitializeFields();
            }
        }

        private void InitializeFields()
        {
            if (_fieldsInitialized || _travMove == null) return;
            _jumpImpulseField = _travMove.Field("jumpImpulse");
            _sprintMultiplierField = _travMove.Field("sprintMultiplier");
            _velocityField = _travMove.Field("velocity");

            // Common Unity/Zorro fields for climbing, fallback to air control if not present
            _climbSpeedField = _travMove.Field("climbSpeed");
            if (!_climbSpeedField.FieldExists()) _climbSpeedField = _travMove.Field("wallClimbSpeed");
            if (!_climbSpeedField.FieldExists()) _climbSpeedField = _travMove.Field("airControl"); // Fallback

            _wallAdhesionField = _travMove.Field("wallAdhesion");
            if (!_wallAdhesionField.FieldExists()) _wallAdhesionField = _travMove.Field("wallJumpMultiplier"); // Fallback

            _fieldsInitialized = true;
        }

        private void CaptureDefaults()
        {
            float? j = _jumpImpulseField?.GetValue<float>();
            if (j.HasValue && j.Value > 0) _defJump = j.Value;

            float? sm = _sprintMultiplierField?.GetValue<float>();
            if (sm.HasValue) _defSprintMult = sm.Value;

            if (_climbSpeedField != null && _climbSpeedField.FieldExists())
            {
                float? cs = _climbSpeedField.GetValue<float>();
                if (cs.HasValue) _defClimbSpeed = cs.Value;
            }

            if (_wallAdhesionField != null && _wallAdhesionField.FieldExists())
            {
                float? wa = _wallAdhesionField.GetValue<float>();
                if (wa.HasValue) _defWallAdhesion = wa.Value;
            }

            _defaultsCaptured = true;
        }

        private void RestoreDefaults()
        {
            _jumpImpulseField?.SetValue(_defJump);
            _sprintMultiplierField?.SetValue(_defSprintMult);
            if (_climbSpeedField != null && _climbSpeedField.FieldExists()) _climbSpeedField.SetValue(_defClimbSpeed);
            if (_wallAdhesionField != null && _wallAdhesionField.FieldExists()) _wallAdhesionField.SetValue(_defWallAdhesion);

            _isModified = false;
            _defaultsCaptured = false;
        }

        public bool IsDayTime()
        {
            float cycleTime = Time.time % ClassBalance.DUALNATURE_DAY_CYCLE_DURATION;
            return cycleTime < (ClassBalance.DUALNATURE_DAY_CYCLE_DURATION / 2f);
        }

        private void FixedUpdate()
        {
            if (_character == null || !_character.IsLocal || !_fieldsInitialized) return;

            if (ClassManager.IsDualNatureActive())
            {
                if (!_defaultsCaptured)
                {
                    CaptureDefaults();
                    _lastStateWasDay = !IsDayTime(); // Force update first time
                    _lastStateGrounded = !_character.data.isGrounded;
                }

                bool isDay = IsDayTime();
                bool isGrounded = _character.data.isGrounded;

                // Only update fields via reflection if the state actually changed to avoid GC/Performance spikes
                if (isDay != _lastStateWasDay || isGrounded != _lastStateGrounded || !_isModified)
                {
                    // Movement Handling
                    if (isDay)
                    {
                        // Day: Increased wall-climbing speed/adhesion.
                        if (_climbSpeedField != null && _climbSpeedField.FieldExists()) _climbSpeedField.SetValue(_defClimbSpeed * ClassBalance.DUALNATURE_DAY_JUMP_MULT);
                        if (_wallAdhesionField != null && _wallAdhesionField.FieldExists()) _wallAdhesionField.SetValue(_defWallAdhesion * ClassBalance.DUALNATURE_DAY_JUMP_MULT);

                        _jumpImpulseField?.SetValue(_defJump); // normal jump
                        _sprintMultiplierField?.SetValue(_defSprintMult); // normal sprint
                    }
                    else
                    {
                        // Night: Movement speed buff specifically when grounded on terrain
                        _jumpImpulseField?.SetValue(_defJump); // normal jump
                        if (_climbSpeedField != null && _climbSpeedField.FieldExists()) _climbSpeedField.SetValue(_defClimbSpeed);
                        if (_wallAdhesionField != null && _wallAdhesionField.FieldExists()) _wallAdhesionField.SetValue(_defWallAdhesion);

                        if (isGrounded)
                        {
                            _sprintMultiplierField?.SetValue(_defSprintMult * ClassBalance.DUALNATURE_NIGHT_SPEED_MULT);
                        }
                        else
                        {
                            _sprintMultiplierField?.SetValue(_defSprintMult);
                        }
                    }

                    if (_lastStateWasDay != isDay)
                    {
                        OnDualNatureStateChanged?.Invoke(isDay, _character);

                        if (_audioSource != null)
                        {
                            if (isDay && dayTransitionSound != null) _audioSource.PlayOneShot(dayTransitionSound);
                            else if (!isDay && nightTransitionSound != null) _audioSource.PlayOneShot(nightTransitionSound);
                        }
                    }

                    _lastStateWasDay = isDay;
                    _lastStateGrounded = isGrounded;
                    _isModified = true;
                }
            }
            else if (_isModified)
            {
                RestoreDefaults();
            }
        }

        private void Update()
        {
            if (_character == null || !_character.IsLocal) return;

            if (ClassManager.IsDualNatureActive())
            {
                bool isDay = IsDayTime();

                // Nighttime survival: Increased hunger depletion
                if (!isDay && _character.refs?.afflictions != null)
                {
                    // We increase hunger over time manually, or we let the multiplier handle it if we patched General_AddStatus_Patch
                    // To do it continuously here:
                    _character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Hunger, Time.deltaTime * 0.05f * ClassBalance.DUALNATURE_HUNGER_MULT, false);
                }

                // Aura processing (Tick based)
                if (Time.time - _lastAuraTime >= ClassBalance.DUALNATURE_AURA_TICK_RATE)
                {
                    _lastAuraTime = Time.time;
                    ProcessAura(isDay);
                }

                // Bag carrying restriction processing (Tick based to avoid every frame)
                if (Time.time - _lastBagCheckTime >= 0.5f)
                {
                    _lastBagCheckTime = Time.time;
                    CheckAndDropBags();
                }
            }
        }

        private void CheckAndDropBags()
        {
            if (_character.player == null || _character.player.itemSlots == null) return;

            // Loop through inventory slots
            for (int i = 0; i < _character.player.itemSlots.Length; i++)
            {
                ItemSlot slot = _character.player.itemSlots[i];
                if (slot == null || slot.IsEmpty()) continue;

                Item item = slot.ItemInSlot;
                if (item != null && item.name.IndexOf("bag", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // The class cannot equip or carry bags. Drop it.
                    try
                    {
                        var playerTraverse = Traverse.Create(_character.player);
                        // Using multiple common method names from the underlying engine
                        if (playerTraverse.Method("DropItem", new object[] { item }).MethodExists())
                        {
                            playerTraverse.Method("DropItem", new object[] { item }).GetValue();
                        }
                        else if (playerTraverse.Method("TryDropItem", new object[] { item }).MethodExists())
                        {
                            playerTraverse.Method("TryDropItem", new object[] { item }).GetValue();
                        }
                    }
                    catch { }
                }
            }
        }

        private HashSet<Character> _processedAuraTargets = new HashSet<Character>();

        private void ProcessAura(bool isDay)
        {
            _processedAuraTargets.Clear();
            int count = Physics.OverlapSphereNonAlloc(_character.transform.position, ClassBalance.DUALNATURE_RADIUS, _auraColliders);
            for (int i = 0; i < count; i++)
            {
                Collider col = _auraColliders[i];
                if (col == null) continue;

                // Find Character
                Character target = col.GetComponentInParent<Character>();
                if (target == null || target == _character || target.refs?.afflictions == null) continue;

                // Deduplicate characters to prevent multiple colliders causing excessive damage/healing
                if (_processedAuraTargets.Contains(target)) continue;
                _processedAuraTargets.Add(target);

                if (isDay)
                {
                    // Healing pulse for allies
                    float currentInjury = target.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury);
                    if (currentInjury > 0)
                    {
                        // Note: If SubtractStatus doesn't work, we can just set it
                        float newInjury = Mathf.Max(0, currentInjury - ClassBalance.DUALNATURE_HEAL_TICK);
                        target.refs.afflictions.SetStatus(CharacterAfflictions.STATUSTYPE.Injury, newInjury);
                    }
                }
                else
                {
                    // Periodic damage to allies
                    target.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, ClassBalance.DUALNATURE_DAMAGE_TICK, false);
                }
            }
        }
    }
}