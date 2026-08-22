using PeakClassMod;
using PeakClassMod.Utils;
using Photon.Pun;
using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PeakClassMod.Handlers
{
	public class MedicStatHandler : MonoBehaviour
	{
		private Character _character;
		private ClassModNetwork _network;
		private CharacterAfflictions _afflictions;
		private int _healsRemaining = ClassBalance.MEDIC_MAX_HEALS;
		private float _lastHealTime = -10f;
		private const float HEAL_COOLDOWN = 2.0f;

		private TextMeshProUGUI _medicUIText;
		private GameObject _uiContainer;

		private void Start()
		{
			_character = GetComponent<Character>();
			_network = GetComponent<ClassModNetwork>();
			if (_character?.refs != null)
			{
				_afflictions = _character.refs.afflictions;
			}
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			bool isMedic = ClassManager.IsMedicActive() || ClassCheckUtil.IsMedicHat(_character);

			if (!isMedic)
			{
				if (_uiContainer != null && _uiContainer.activeSelf)
				{
					_uiContainer.SetActive(false);
				}
				if (_healsRemaining < ClassBalance.MEDIC_MAX_HEALS)
				{
					_healsRemaining = ClassBalance.MEDIC_MAX_HEALS;
				}
				return;
			}

			if (_uiContainer == null)
			{
				CreateUI();
			}

			if (_uiContainer != null && !_uiContainer.activeSelf)
			{
				_uiContainer.SetActive(true);
			}

			UpdateUI();

			float timeSinceLast = Time.time - _lastHealTime;
			if (Input.GetKeyDown(KeyCode.H) && timeSinceLast >= HEAL_COOLDOWN)
			{
				TrySelfHeal();
			}
		}

		private void TrySelfHeal()
		{
			if (_healsRemaining <= 0) return;

			if (_afflictions == null && _character?.refs != null)
			{
				_afflictions = _character.refs.afflictions;
			}

			if (_afflictions != null && _afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f)
			{
				_healsRemaining--;
				_lastHealTime = Time.time;
				_afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, ClassBalance.MEDIC_HEAL_AMOUNT, false);

				// VFX & SFX
				PlayLocalHealEffects();

				if (_network?.photonView != null)
				{
					_network.photonView.RPC("ShowMedicHealEffectRPC", RpcTarget.All, _healsRemaining);
				}
			}
		}

		private void PlayLocalHealEffects()
		{
			// 1. VFX : Aura verte éclatante sur le corps
			if (_character?.refs?.customization != null)
			{
				_character.refs.customization.PulseStatus(new Color(0.2f, 1f, 0.4f, 1f), 1.8f);
			}

			// 2. SFX : Son natif de soin / bandage / kit de soin
			try
			{
				var medkit = ItemCache.Get("FirstAidKit") ?? ItemCache.Get("Bandage");
				var feedback = medkit?.GetComponent<ItemUseFeedback>();
				if (feedback != null && feedback.sfxUsed != null)
				{
					feedback.sfxUsed.Play(_character.Center);
				}
				else if (_character?.refs?.afflictions != null)
				{
					_character.refs.afflictions.injurySmall?.Play(_character.Center);
				}
			}
			catch { }
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

				_uiContainer = new GameObject("MedicHealUI");
				_uiContainer.transform.SetParent(parent.transform, false);

				_medicUIText = _uiContainer.AddComponent<TextMeshProUGUI>();
				RectTransform rect = _uiContainer.GetComponent<RectTransform>();
				rect.anchorMin = new Vector2(0.5f, 0f);
				rect.anchorMax = new Vector2(0.5f, 0f);
				rect.pivot = new Vector2(0.5f, 0f);
				rect.anchoredPosition = new Vector2(0, 85);
				rect.sizeDelta = new Vector2(450, 50);

				_medicUIText.fontSize = 24;
				_medicUIText.alignment = TextAlignmentOptions.Center;
				if (fontAsset != null) _medicUIText.font = fontAsset;
			}
			catch (Exception ex)
			{
				PeakClassModPlugin.Log?.LogWarning($"[MedicStatHandler] UI setup error: {ex.Message}");
			}
		}

		private void UpdateUI()
		{
			if (_medicUIText == null) return;

			float timeRemaining = HEAL_COOLDOWN - (Time.time - _lastHealTime);

			if (_healsRemaining <= 0)
			{
				_medicUIText.text = "<color=#888888>[H] Heal: 0/10 <color=#FF4444>[EMPTY]</color></color>";
			}
			else if (timeRemaining > 0f)
			{
				_medicUIText.text = $"<color=#00E5FF>[H]</color> Heal: <b>{_healsRemaining}/{ClassBalance.MEDIC_MAX_HEALS}</b> <color=#FFCC00>({timeRemaining:F1}s)</color>";
			}
			else
			{
				_medicUIText.text = $"<color=#00FF66>[H]</color> Heal: <b>{_healsRemaining}/{ClassBalance.MEDIC_MAX_HEALS}</b> <color=#00FF66>[READY]</color>";
			}
		}

		private void OnDestroy()
		{
			if (_uiContainer != null) Destroy(_uiContainer);
		}
	}
}