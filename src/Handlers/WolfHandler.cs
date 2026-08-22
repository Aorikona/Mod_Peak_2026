using HarmonyLib;
using PeakClassMod.Utils;
using Peak.Afflictions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace PeakClassMod.Handlers
{
	public class WolfHandler : MonoBehaviour
	{
		private static WolfHandler _localInstance;

		private Character _character;
		private ClassModNetwork _network;
		private CharacterClimbing _characterClimbing;
		private CharacterMovement _characterMovement;

		private float _baseClimbSpeed;
		private float _baseSprintSpeed;
		private float _nextEffectTime = 0f;

		private TextMeshProUGUI _timeTextComponent;
		private Dictionary<Character, float> _lastHitTime = new Dictionary<Character, float>();

		private const float EFFECT_RADIUS = 10f;
		private const float ORB_PULSE_SPEED = 2.5f;
		private const float ORB_PULSE_AMPLITUDE = 0.25f;
		private const float LINE_BASE_WIDTH = 0.25f;

		private GameObject _effectAnchor;
		private GameObject _orbContainer;
		private LineRenderer _ringEquator;
		private LineRenderer _ringVertical1;
		private LineRenderer _ringVertical2;

		private void Start()
		{
			_character = GetComponent<Character>();
			_network = GetComponent<ClassModNetwork>();
			_characterClimbing = GetComponent<CharacterClimbing>();
			_characterMovement = GetComponent<CharacterMovement>();

			if (_characterMovement != null) _baseSprintSpeed = _characterMovement.sprintMultiplier;
			if (_characterClimbing != null) _baseClimbSpeed = _characterClimbing.climbSpeed;

			if (_character != null && _character.IsLocal)
			{
				if (_localInstance != null && _localInstance != this)
				{
					Destroy(this);
					return;
				}
				_localInstance = this;
			}
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			bool isWolf = ClassManager.IsWolfActive() || ClassCheckUtil.IsWolf(_character);

			if (!isWolf)
			{
				SetOrbVisibility(false);
				EnsureCannibalAffliction(false);
				if (_timeTextComponent != null) _timeTextComponent.gameObject.SetActive(false);
				return;
			}

			if (_effectAnchor == null)
			{
				Create3DOrb();
				CreateUI();
			}

			if (_timeTextComponent != null && !_timeTextComponent.gameObject.activeSelf)
			{
				_timeTextComponent.gameObject.SetActive(true);
			}

			float pulse = 1f + Mathf.Sin(Time.time * ORB_PULSE_SPEED) * ORB_PULSE_AMPLITUDE;
			float currentWidth = LINE_BASE_WIDTH * pulse;

			if (_ringEquator != null) _ringEquator.widthMultiplier = currentWidth;
			if (_ringVertical1 != null) _ringVertical1.widthMultiplier = currentWidth * 0.9f;
			if (_ringVertical2 != null) _ringVertical2.widthMultiplier = currentWidth * 0.9f;

			if (_orbContainer != null)
			{
				_orbContainer.transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
			}

			bool isDay = DaySegmentUtils.IsDay();

			// VISION DE CANNIBALE LA NUIT POUR LE LOUP
			EnsureCannibalAffliction(!isDay && !_character.IsGhost);

			if (_character.IsGhost)
			{
				if (_timeTextComponent != null) _timeTextComponent.text = isDay ? "Day" : "Night";
				SetOrbVisibility(false);
				return;
			}
			else
			{
				SetOrbVisibility(true);
			}

			Color orbColor = isDay ? new Color(0.1f, 1f, 0.3f, 0.9f) : new Color(1f, 0.15f, 0.15f, 0.9f);
			SetOrbColor(orbColor);

			if (Time.time >= _nextEffectTime)
			{
				_nextEffectTime = Time.time + 1f;

				if (isDay)
				{
					if (_timeTextComponent != null) _timeTextComponent.text = "Day";
					HealFriends();
					if (_characterClimbing != null) _characterClimbing.climbSpeed = _baseClimbSpeed * ClassBalance.WOLF_CLIMB_SPEED_MULT;
					if (_characterMovement != null) _characterMovement.sprintMultiplier = _baseSprintSpeed;
				}
				else
				{
					if (_timeTextComponent != null) _timeTextComponent.text = "Night";
					DamageAndFeedOnFriends();
					if (_characterClimbing != null) _characterClimbing.climbSpeed = _baseClimbSpeed;
					if (_characterMovement != null) _characterMovement.sprintMultiplier = _baseSprintSpeed * ClassBalance.WOLF_SPRINT_SPEED_MULT;
				}
			}
		}

		private void EnsureCannibalAffliction(bool enable)
		{
			if (_character?.refs?.afflictions == null) return;
			var list = _character.refs.afflictions.afflictionList;
			if (list == null) return;

			if (enable)
			{
				bool hasCannibal = false;
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] != null && list[i].GetType().Name.IndexOf("Cannibal", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						hasCannibal = true;
						break;
					}
				}

				if (!hasCannibal)
				{
					try
					{
						Type targetType = null;
						foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
						{
							targetType = asm.GetType("Peak.Afflictions.Affliction_Cannibal")
								?? asm.GetType("Affliction_Cannibal");

							if (targetType == null)
							{
								try
								{
									targetType = asm.GetTypes().FirstOrDefault(t => t != null && t.Name.IndexOf("Cannibal", StringComparison.OrdinalIgnoreCase) >= 0 && typeof(Affliction).IsAssignableFrom(t));
								}
								catch { }
							}

							if (targetType != null) break;
						}

						if (targetType != null)
						{
							Affliction inst = null;
							var ctors = targetType.GetConstructors();
							foreach (var c in ctors)
							{
								var pars = c.GetParameters();
								if (pars.Length == 0) { inst = (Affliction)c.Invoke(null); break; }
								else if (pars.Length == 1 && pars[0].ParameterType == typeof(float)) { inst = (Affliction)c.Invoke(new object[] { 99999f }); break; }
								else if (pars.Length == 2 && pars[0].ParameterType == typeof(int) && pars[1].ParameterType == typeof(float)) { inst = (Affliction)c.Invoke(new object[] { 1, 99999f }); break; }
							}

							if (inst == null)
							{
								inst = (Affliction)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(targetType);
							}

							if (inst != null)
							{
								_character.refs.afflictions.AddAffliction(inst, false);
							}
						}
					}
					catch { }
				}
			}
			else
			{
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (list[i] != null && list[i].GetType().Name.IndexOf("Cannibal", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						_character.refs.afflictions.RemoveAffliction(list[i]);
					}
				}
			}
		}

		public void DamageAndFeedOnFriends()
		{
			if (_character?.refs?.view == null || !_character.refs.view.IsMine) return;

			Vector3 center = GetRealPlayerPosition();
			Collider[] hitColliders = Physics.OverlapSphere(center, EFFECT_RADIUS);
			HashSet<Character> alreadyHit = new HashSet<Character>();

			bool fedThisTick = false;

			foreach (var hit in hitColliders)
			{
				Character other = hit.GetComponentInParent<Character>();
				if (other == null || other == _character || alreadyHit.Contains(other)) continue;

				alreadyHit.Add(other);
				float now = Time.time;
				if (_lastHitTime.ContainsKey(other) && now - _lastHitTime[other] < 1f) continue;
				_lastHitTime[other] = now;

				var targetNet = other.GetComponent<ClassModNetwork>();
				if (targetNet != null && targetNet.photonView != null && other.photonView != null && other.photonView.Owner != null)
				{
					targetNet.photonView.RPC("RPC_ApplyWolfDamage", other.photonView.Owner, 0.05f);
				}
				else
				{
					other.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.05f, false, true, true);
				}

				fedThisTick = true;
			}

			// Quand le loup blesse un allié la nuit, il se nourrit et réduit sa faim !
			if (fedThisTick && _character.refs?.afflictions != null)
			{
				_character.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Hunger, 0.10f, false);
			}
		}

		public void HealFriends()
		{
			if (_character?.refs?.view == null || !_character.refs.view.IsMine) return;

			Vector3 center = GetRealPlayerPosition();
			Collider[] hitColliders = Physics.OverlapSphere(center, EFFECT_RADIUS);
			HashSet<Character> alreadyHit = new HashSet<Character>();

			foreach (var hit in hitColliders)
			{
				Character other = hit.GetComponentInParent<Character>();
				if (other == null || other == _character || alreadyHit.Contains(other)) continue;

				alreadyHit.Add(other);
				float now = Time.time;
				if (_lastHitTime.ContainsKey(other) && now - _lastHitTime[other] < 1f) continue;
				_lastHitTime[other] = now;

				var targetNet = other.GetComponent<ClassModNetwork>();
				if (targetNet != null && targetNet.photonView != null && other.photonView != null && other.photonView.Owner != null)
				{
					targetNet.photonView.RPC("RPC_ApplyWolfHeal", other.photonView.Owner, 0.025f);
				}
				else
				{
					other.refs?.afflictions?.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.025f, false);
				}
			}
		}

		private void LateUpdate()
		{
			if (_effectAnchor == null) return;
			_effectAnchor.transform.position = GetRealPlayerPosition();
		}

		private Vector3 GetRealPlayerPosition()
		{
			if (_character != null && _character.IsLocal && Camera.main != null)
			{
				Vector3 cam = Camera.main.transform.position;
				return new Vector3(cam.x, cam.y - 1.0f, cam.z);
			}
			if (_character != null)
			{
				var animPos = _character.transform.Find("animationPosition");
				if (animPos != null) return animPos.position;
				return _character.transform.position;
			}
			return Vector3.zero;
		}

		private void OnDestroy()
		{
			if (_localInstance == this) _localInstance = null;
			EnsureCannibalAffliction(false);
			if (_effectAnchor != null) Destroy(_effectAnchor);
		}

		private void Create3DOrb()
		{
			_effectAnchor = new GameObject("Wolf3DOrbAnchor");
			_effectAnchor.transform.position = GetRealPlayerPosition();

			_orbContainer = new GameObject("OrbRings");
			_orbContainer.transform.SetParent(_effectAnchor.transform, false);
			_orbContainer.transform.localPosition = Vector3.zero;
			_orbContainer.transform.localRotation = Quaternion.identity;

			Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Color");
			Material lineMat = new Material(shader);

			_ringEquator = CreateRing("Equator", _orbContainer.transform, lineMat);
			DrawCircleRing(_ringEquator, EFFECT_RADIUS, Vector3.right, Vector3.forward);

			_ringVertical1 = CreateRing("Meridian1", _orbContainer.transform, lineMat);
			DrawCircleRing(_ringVertical1, EFFECT_RADIUS, Vector3.right, Vector3.up);

			_ringVertical2 = CreateRing("Meridian2", _orbContainer.transform, lineMat);
			DrawCircleRing(_ringVertical2, EFFECT_RADIUS, Vector3.forward, Vector3.up);
		}

		private LineRenderer CreateRing(string name, Transform parent, Material mat)
		{
			GameObject ringObj = new GameObject(name);
			ringObj.transform.SetParent(parent, false);

			LineRenderer lr = ringObj.AddComponent<LineRenderer>();
			lr.useWorldSpace = false;
			lr.loop = true;
			lr.positionCount = 80;
			lr.startWidth = LINE_BASE_WIDTH;
			lr.endWidth = LINE_BASE_WIDTH;
			lr.material = mat;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			return lr;
		}

		private void DrawCircleRing(LineRenderer lr, float radius, Vector3 axisA, Vector3 axisB)
		{
			int count = 80;
			for (int i = 0; i < count; i++)
			{
				float angle = ((float)i / count) * Mathf.PI * 2f;
				Vector3 pos = (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius;
				lr.SetPosition(i, pos);
			}
		}

		private void SetOrbColor(Color color)
		{
			if (_ringEquator != null) { _ringEquator.startColor = color; _ringEquator.endColor = color; }
			if (_ringVertical1 != null) { _ringVertical1.startColor = color; _ringVertical1.endColor = color; }
			if (_ringVertical2 != null) { _ringVertical2.startColor = color; _ringVertical2.endColor = color; }
		}

		private void SetOrbVisibility(bool visible)
		{
			if (_ringEquator != null) _ringEquator.enabled = visible;
			if (_ringVertical1 != null) _ringVertical1.enabled = visible;
			if (_ringVertical2 != null) _ringVertical2.enabled = visible;
		}

		private void CreateUI()
		{
			try
			{
				GameObject parent = GameObject.Find("GAME/GUIManager/Canvas_HUD");
				if (parent == null && GUIManager.instance != null) parent = GUIManager.instance.gameObject;
				if (parent == null)
				{
					var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
					if (canvas != null) parent = canvas.gameObject;
				}

				if (parent == null) return;

				TMP_FontAsset fontAsset = null;
				foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					if (t.font != null && t.font.name.Contains("Daruma"))
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
				if (fontAsset != null) _timeTextComponent.font = fontAsset;
			}
			catch (Exception ex)
			{
				PeakClassModPlugin.Log?.LogWarning($"[WolfHandler] UI setup warning: {ex.Message}");
			}
		}
	}
}