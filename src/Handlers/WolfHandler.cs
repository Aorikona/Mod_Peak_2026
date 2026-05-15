using HarmonyLib;
using Peak.Afflictions;
using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace PeakClassMod.Handlers
{
    public class WolfHandler : MonoBehaviour
    {
        private static WolfHandler _localInstance;

        private Character _character;
        private ClassModNetwork _network;
        private CharacterClimbing _characterClimbing;
        private CharacterMovement _characterMovement;
        private Transform _followTarget;


        private float _baseClimbSpeed;
        private float _baseSprintSpeed;
        private float _nextEffectTime = 0f;
        private CharacterController _cc;

        private TextMeshProUGUI _timeTextComponent;

        private Dictionary<Character, float> _lastHitTime = new Dictionary<Character, float>();

        private const float EFFECT_RADIUS = 10f;
        private const float CIRCLE_PULSE_SPEED = 3f;
        private const float CIRCLE_PULSE_AMPLITUDE = 0.3f;
        private const float CIRCLE_BASE_WIDTH = 0.30f;

        private GameObject _effectAnchor;
        private LineRenderer _circleRenderer;

        private DaySegmentUtils _daySegmentUtils;
        private string _lastTimeText = "";
        private Camera _mainCamera;


        private void Start()
        {
            _character = GetComponent<Character>();

            _followTarget = _character.transform.Find("animationPosition");

            if (_followTarget == null)
                _followTarget = _character.transform.Find("Scout");

            if (_followTarget == null)
                _followTarget = _character.transform.Find("MainMesh");

            if (_followTarget == null)
                _followTarget = _character.transform;

            if (!ClassCheckUtil.IsWolf(_character) || !_character.IsLocal)
                return;

            

            _cc = GetComponent<CharacterController>();

            if (_localInstance != null && _localInstance != this)
            {
                Destroy(this);
                return;
            }

            _localInstance = this;
            _daySegmentUtils = new DaySegmentUtils();
            _mainCamera = Camera.main;

            CreateUI();
            CreateEffectAnchor();
            CreateVisibleCircle();

            _network = GetComponent<ClassModNetwork>();
            _characterClimbing = GetComponent<CharacterClimbing>();
            _characterMovement = GetComponent<CharacterMovement>();

            _baseSprintSpeed = _characterMovement.sprintMultiplier;
            _baseClimbSpeed = _characterClimbing.climbSpeed;
        }

        private void SetTimeText(string text)
        {
            if (_timeTextComponent != null && _lastTimeText != text)
            {
                _timeTextComponent.text = text;
                _lastTimeText = text;
            }
        }

        private void Update()
        {
            if (_character == null || !_character.IsLocal || !ClassCheckUtil.IsWolf(_character))
                return;

            float pulse = 1f + Mathf.Sin(Time.time * CIRCLE_PULSE_SPEED) * CIRCLE_PULSE_AMPLITUDE;
            float widthMultiplier = CIRCLE_BASE_WIDTH * pulse;
            _circleRenderer.widthMultiplier = widthMultiplier;

            if (_character.IsGhost)
            {
                if (_daySegmentUtils.IsDay())
                {
                    SetTimeText("Day");
                }
                else
                {
                    SetTimeText("Night");
                }

                if (_circleRenderer != null)
                    _circleRenderer.enabled = false;

                return;
            }
            else
            {
                if (_circleRenderer != null)
                    _circleRenderer.enabled = true;
            }
            if (Time.time >= _nextEffectTime)
            {
                _nextEffectTime = Time.time + 1f;

                if (_daySegmentUtils.IsDay())
                {
                    SetTimeText("Day");

                    HealFriends();
                    _circleRenderer.startColor = Color.green;
                    _circleRenderer.endColor = Color.green;
                    _characterClimbing.climbSpeed =
                        _baseClimbSpeed * ClassBalance.WOLF_CLIMB_SPEED_MULT;

                    _characterMovement.sprintMultiplier =
                        _baseSprintSpeed;
                }
                else
                {
                    SetTimeText("Night");

                    DamageFriends();
                    _circleRenderer.startColor = Color.red;
                    _circleRenderer.endColor = Color.red;
                    _characterClimbing.climbSpeed =
                        _baseClimbSpeed;

                    _characterMovement.sprintMultiplier =
                        _baseSprintSpeed * ClassBalance.WOLF_SPRINT_SPEED_MULT;
                }
            }
        }
        private void LateUpdate()
        {
            if (_effectAnchor == null)
                return;

            Vector3 pos = GetPlayerRealPosition();

            _effectAnchor.transform.position = pos;

            if (_circleRenderer != null)
            {
                _circleRenderer.transform.position = new Vector3(pos.x, pos.y + 0.05f, pos.z);
            }
        }

        private void OnDestroy()
        {
            if (_localInstance == this)
                _localInstance = null;
        }

        private void CreateEffectAnchor()
        {
            _effectAnchor = new GameObject("WolfEffectAnchor");
            _effectAnchor.transform.position = GetPlayerRealPosition();
        }

        private void UpdateEffectAnchor()
        {
            if (_effectAnchor == null)
                return;

            Vector3 pos = GetPlayerRealPosition();

            _effectAnchor.transform.position = new Vector3(pos.x, pos.y + 0.1f, pos.z);
        }

        private Vector3 GetPlayerRealPosition()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_mainCamera != null)
            {
                Vector3 cam = _mainCamera.transform.position;
                return new Vector3(cam.x, cam.y - 2f, cam.z);
            }

            return Vector3.zero;
        }

        private Vector3 GetEffectCenterPosition()
        {
            if (_effectAnchor != null)
                return _effectAnchor.transform.position;

            return GetPlayerRealPosition();
        }

        public void HealFriends()
        {
            if (!_character.refs.view.IsMine)
                return;

            Collider[] hitColliders =
                Physics.OverlapSphere(GetEffectCenterPosition(), EFFECT_RADIUS);

            HashSet<Character> alreadyHit = new HashSet<Character>();

            foreach (var hit in hitColliders)
            {
                Character other = hit.GetComponentInParent<Character>();

                if (other == null)
                    continue;

                if (other == _character)
                    continue;

                if (alreadyHit.Contains(other))
                    continue;

                alreadyHit.Add(other);

                float now = Time.time;

                if (_lastHitTime.ContainsKey(other))
                {
                    if (now - _lastHitTime[other] < 1f)
                        continue;
                }

                _lastHitTime[other] = now;

                var targetNet = other.GetComponent<ClassModNetwork>();
                if (targetNet != null && targetNet.photonView != null && other.photonView != null && other.photonView.Owner != null)
                {
                    targetNet.photonView.RPC("RPC_ApplyWolfHeal", other.photonView.Owner, 0.025f);
                }
                else
                {
                    other.refs?.afflictions?.SubtractStatus(
                        CharacterAfflictions.STATUSTYPE.Injury,
                        0.025f,
                        false
                    );
                }
            }
        }

        public void DamageFriends()
        {
            if (!_character.refs.view.IsMine)
                return;

            Collider[] hitColliders =
                Physics.OverlapSphere(GetEffectCenterPosition(), EFFECT_RADIUS);

            HashSet<Character> alreadyHit = new HashSet<Character>();

            foreach (var hit in hitColliders)
            {
                Character other = hit.GetComponentInParent<Character>();

                if (other == null)
                    continue;

                if (other == _character)
                    continue;

                if (alreadyHit.Contains(other))
                    continue;

                alreadyHit.Add(other);

                float now = Time.time;

                if (_lastHitTime.ContainsKey(other))
                {
                    if (now - _lastHitTime[other] < 1f)
                        continue;
                }

                _lastHitTime[other] = now;

                var targetNet = other.GetComponent<ClassModNetwork>();
                if (targetNet != null && targetNet.photonView != null && other.photonView != null && other.photonView.Owner != null)
                {
                    targetNet.photonView.RPC("RPC_ApplyWolfDamage", other.photonView.Owner, 0.05f);
                }
                else
                {
                    other.refs?.afflictions?.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Injury,
                        0.05f,
                        false,
                        true,
                        true
                    );
                }
            }
        }

        private void CreateVisibleCircle()
        {
            GameObject circleObj = new GameObject("WolfRangeCircle");

            _circleRenderer = circleObj.AddComponent<LineRenderer>();

            _circleRenderer.useWorldSpace = false;
            _circleRenderer.loop = true;

            _circleRenderer.positionCount = 100;

            _circleRenderer.startWidth = CIRCLE_BASE_WIDTH;
            _circleRenderer.endWidth = CIRCLE_BASE_WIDTH;

            _circleRenderer.material =
                new Material(Shader.Find("Sprites/Default"));

            _circleRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            _circleRenderer.receiveShadows = false;

            DrawCircle(EFFECT_RADIUS);
        }

        private void DrawCircle(float radius)
        {
            int segments = 100;

            for (int i = 0; i < segments; i++)
            {
                float angle =
                    ((float)i / segments) * Mathf.PI * 2f;

                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                _circleRenderer.SetPosition(
                    i,
                    new Vector3(x, 0f, z)
                );
            }
        }

        private void CreateUI()
        {
            GameObject parent =
                GameObject.Find("GAME/GUIManager/Canvas_HUD");

            TMP_FontAsset fontAsset = null;

            foreach (var t in GameObject.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t.font != null &&
                    t.font.name == "DarumaDropOne-Regular SDF")
                {
                    fontAsset = t.font;
                    break;
                }
            }

            GameObject obj = new GameObject("TimeText");
            obj.transform.SetParent(parent.transform, false);

            _timeTextComponent = obj.AddComponent<TextMeshProUGUI>();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(930, 500);

            _timeTextComponent.fontSize = 40;
            _timeTextComponent.color = Color.white;
            _timeTextComponent.font = fontAsset;
        }
    }
}
