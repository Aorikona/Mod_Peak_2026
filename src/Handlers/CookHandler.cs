using System;
using System.Linq;
using TMPro;
using UnityEngine;
using Photon.Pun;
using PeakClassMod.Utils;
using Zorro.Core;

namespace PeakClassMod.Handlers
{
	public class CookHandler : MonoBehaviour
	{
		private Character _character;
		private Campfire _hoveredStove = null;

		private GameObject _uiContainer;
		private TextMeshProUGUI _cookPromptText;
		private static TMP_FontAsset _peakFont;

		private const float DETECT_RADIUS = 4.0f;
		private const float MAX_VIEW_ANGLE = 50.0f;

		private void Awake()
		{
			_character = GetComponent<Character>();
		}

		private void Start()
		{
			CreateUI();
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;
			bool isCook = ClassManager.IsCookActive() || ClassCheckUtil.IsCook(_character);

			if (!isCook)
			{
				if (_uiContainer != null && _uiContainer.activeSelf)
				{
					_uiContainer.SetActive(false);
				}
				_hoveredStove = null;
				return;
			}

			// Détection par rayon (distance) + cône de vision (orientation)
			_hoveredStove = FindTargetStove();

			// Mise à jour de l''affichage
			UpdateUI();

			// Touche G ou F pour ramasser le réchaud
			if (_hoveredStove != null && (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.F)))
			{
				PickupStove(_hoveredStove);
			}
		}

		public static bool IsPlacedStoveStatic(Campfire campfire)
		{
			if (campfire == null) return false;
			string name = campfire.gameObject.name.ToLowerInvariant();

			if (name.Contains("portablestovetop") || name.Contains("portablestove") || name.Contains("stove") || name.Contains("réchaud") || name.Contains("rechaud"))
			{
				return true;
			}

			if (name.Contains("placed") || name.Contains("clone") || name.Contains("instance"))
			{
				return true;
			}

			return false;
		}

		private Campfire FindTargetStove()
		{
			Camera cam = Camera.main;
			if (cam == null) return null;

			Campfire bestStove = null;
			float bestScore = float.MaxValue;

			// Détection par sphère autour du joueur (rayon 4m)
			var colliders = Physics.OverlapSphere(_character.Center, DETECT_RADIUS);
			foreach (var col in colliders)
			{
				var campfire = col.GetComponentInParent<Campfire>();
				if (campfire != null && IsPlacedStoveStatic(campfire))
				{
					Vector3 stovePos = campfire.transform.position + Vector3.up * 0.25f;
					Vector3 toStove = stovePos - cam.transform.position;
					float dist = toStove.magnitude;

					if (dist <= DETECT_RADIUS)
					{
						float angle = Vector3.Angle(cam.transform.forward, toStove);

						// Cône de vision confortable (50 degrés) : visible dès qu''on regarde dans la direction du réchaud
						if (angle <= MAX_VIEW_ANGLE)
						{
							// Vérification de ligne de vue (pas de montagne/mur qui bloque)
							if (!Physics.Linecast(cam.transform.position, stovePos, LayerMask.GetMask("Terrain", "Map")))
							{
								float score = dist * (1f + angle * 0.05f);
								if (score < bestScore)
								{
									bestScore = score;
									bestStove = campfire;
								}
							}
						}
					}
				}
			}

			return bestStove;
		}

		private void PickupStove(Campfire stove)
		{
			if (stove == null) return;

			PeakClassModPlugin.Log?.LogInfo($"[Cook] Picking up placed stove: {stove.gameObject.name}");

			Item stoveItem = ItemCache.Get("portablestovetopitem");
			if (stoveItem == null) stoveItem = ItemCache.Get("PortableStove");
			if (stoveItem == null) stoveItem = ItemCache.Get("portablestovetop");

			if (stoveItem != null && _character.player != null)
			{
				ushort id = (ushort)stoveItem.itemID;
				if (_character.player.HasEmptySlot(id))
				{
					_character.player.AddItem(id, null, out _);
				}
				else
				{
					ItemDatabase.Add(stoveItem, _character.Center + Vector3.up * 0.5f);
				}
			}

			var pv = stove.GetComponent<PhotonView>();
			if (pv != null && pv.IsMine)
			{
				PhotonNetwork.Destroy(stove.gameObject);
			}
			else if (pv != null)
			{
				pv.RPC("DestroyStoveRPC", RpcTarget.AllBuffered);
			}
			else
			{
				UnityEngine.Object.Destroy(stove.gameObject);
			}

			_hoveredStove = null;
			if (_uiContainer != null) _uiContainer.SetActive(false);
		}

		private void CreateUI()
		{
			try
			{
				if (_uiContainer != null) return;

				GameObject parent = GameObject.Find("GAME/GUIManager/Canvas_HUD");
				if (parent == null && GUIManager.instance != null) parent = GUIManager.instance.gameObject;
				if (parent == null)
				{
					var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
					if (canvas != null) parent = canvas.gameObject;
				}

				if (parent == null) return;

				// Récupération de la police officielle PEAK (Daruma)
				if (_peakFont == null)
				{
					foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
					{
						if (t.font != null && (t.font.name.Contains("Daruma") || t.font.name.Contains("Peak") || t.font.name.Contains("Font")))
						{
							_peakFont = t.font;
							break;
						}
					}
					if (_peakFont == null && GUIManager.instance?.interactPromptText?.font != null)
					{
						_peakFont = GUIManager.instance.interactPromptText.font;
					}
				}

				_uiContainer = new GameObject("CookStovePromptUI");
				_uiContainer.transform.SetParent(parent.transform, false);

				_cookPromptText = _uiContainer.AddComponent<TextMeshProUGUI>();
				RectTransform rect = _uiContainer.GetComponent<RectTransform>();
				rect.anchorMin = new Vector2(0.5f, 0.5f);
				rect.anchorMax = new Vector2(0.5f, 0.5f);
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(0, -110);
				rect.sizeDelta = new Vector2(500, 50);

				_cookPromptText.fontSize = 24;
				_cookPromptText.alignment = TextAlignmentOptions.Center;
				if (_peakFont != null) _cookPromptText.font = _peakFont;

				_uiContainer.SetActive(false);
			}
			catch (Exception ex)
			{
				PeakClassModPlugin.Log?.LogWarning($"[CookHandler] UI creation error: {ex.Message}");
			}
		}

		private void UpdateUI()
		{
			if (_uiContainer == null)
			{
				CreateUI();
			}

			if (_hoveredStove != null)
			{
				if (_uiContainer != null && !_uiContainer.activeSelf)
				{
					_uiContainer.SetActive(true);
				}

				if (_cookPromptText != null)
				{
					if (_cookPromptText.font == null && _peakFont != null)
					{
						_cookPromptText.font = _peakFont;
					}
					_cookPromptText.text = "<color=#FFCC00><b>[G]</b></color> <color=#FFFFFF>PICK UP STOVE</color>";
				}
			}
			else
			{
				if (_uiContainer != null && _uiContainer.activeSelf)
				{
					_uiContainer.SetActive(false);
				}
			}
		}

		private void OnDestroy()
		{
			if (_uiContainer != null) UnityEngine.Object.Destroy(_uiContainer);
		}
	}
}
