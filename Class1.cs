// ============================================================
// PEAK Class Mod - Version: 3.6.1 (Stamina Fix)
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Peak.Afflictions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Core;

namespace PeakClassMod
{
	[BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "3.6.1")]
	public class PeakClassModPlugin : BaseUnityPlugin
	{
		public static PeakClassModPlugin Instance;
		internal static ManualLogSource Log;
		private static Harmony harmony;

		public static bool scoutActive = false;
		public static bool medicActive = false;
		public static bool techPriestActive = false;
		public static bool mushroomancerActive = false;
		public static bool greatEpeeActive = false;
		public static bool rogueActive = false;
		public static bool generalActive = false;
		public static bool astronautActive = false;
		public static string currentClass = "None";

		private GameObject _callbacksGO;

		private void Awake()
		{
			Instance = this;
			Log = Logger;
			harmony = new Harmony("com.peakmod.classmod");
			harmony.PatchAll();

			_callbacksGO = new GameObject("PeakClassModCallbacks");
			GameObject.DontDestroyOnLoad(_callbacksGO);
			_callbacksGO.AddComponent<PeakClassModCallbacks>();

			Log?.LogInfo("[ClassMod] Ready. Version 3.6.1 loaded (Stamina Fix).");
		}

		private void OnDestroy()
		{
			try { harmony?.UnpatchSelf(); } catch { }
			if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
		}

		public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
		{
			if (go.GetComponent<T>() == null) go.AddComponent<T>();
		}

		public static List<string> BuildStartItemsByHatName(string hatName)
		{
			var items = new List<string>();
			if (string.IsNullOrEmpty(hatName)) return items;

			if (hatName == "Hat_Mushroom")
			{
				for (int i = 0; i < 3; i++) items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
			}
			else if (hatName == "Hat_Medic")
			{
				items.Add("FirstAidKit");
				items.Add("Cure-All");
			}
			else if (hatName == "Hat_Crab")
			{
				items.Add("ChainShooter");
				items.Add("ChainShooter");
			}
			else if (hatName == "Hat_NinjaHeadband")
			{
				items.Add("RopeSpool");
				items.Add("RopeSpool");
			}
			else if (hatName == "Hat_Astronaut")
			{
				items.Add("BalloonBunch");
			}
			return items;
		}

		private static readonly string[] StartBerries = new[] {
			"Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green"
		};
	}

	// ==================================================
	// NETWORK HANDLER
	// ==================================================
	public class ClassModNetwork : MonoBehaviourPun
	{
		private Character _character;
		private bool _hasRequestedItems = false;
		private static Dictionary<int, float> _hostGiveCooldown = new Dictionary<int, float>();
		private static Dictionary<int, Character> _characterCache = new Dictionary<int, Character>();

		private void Awake()
		{
			var components = GetComponents<ClassModNetwork>();
			if (components.Length > 1) { UnityEngine.Object.Destroy(this); return; }
			_character = GetComponent<Character>();
		}

		private void Start()
		{
			if (photonView.IsMine && !_hasRequestedItems)
			{
				_hasRequestedItems = true;
				StartCoroutine(RequestItemsRoutine());
			}
		}

		private void OnDestroy()
		{
			if (_character != null && _character.photonView != null && _character.photonView.Owner != null)
			{
				_characterCache.Remove(_character.photonView.Owner.ActorNumber);
			}
		}

		private IEnumerator RequestItemsRoutine()
		{
			yield return new WaitForSeconds(0.3f);

			while (!PhotonNetwork.InRoom)
			{
				yield return null;
			}

			if (_character == null) _character = GetComponent<Character>();

			float timeout = 5f;
			bool inventoryReady = false;

			while (timeout > 0)
			{
				if (_character != null && _character.player != null && _character.player.itemSlots != null)
				{
					if (_character.player.itemSlots.Length > 0 && !_character.player.itemSlots[0].IsEmpty())
					{
						inventoryReady = true;
						break;
					}
				}
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			if (inventoryReady)
			{
				string myHatName = "None";
				if (HatUtils.TryGetHatName(_character, out string detectedHat)) myHatName = detectedHat;
				PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Requesting items for Hat: {myHatName}");
				photonView.RPC("RPC_HostGiveItems", RpcTarget.MasterClient, myHatName);
			}
			else
			{
				yield return new WaitForSeconds(1f);
				if (_character != null && _character.player != null)
				{
					string myHatName = "None";
					if (HatUtils.TryGetHatName(_character, out string detectedHat)) myHatName = detectedHat;
					PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Retry requesting items for Hat: {myHatName}");
					photonView.RPC("RPC_HostGiveItems", RpcTarget.MasterClient, myHatName);
				}
			}
		}

		[PunRPC]
		public void RPC_HostGiveItems(string clientHatName, PhotonMessageInfo info)
		{
			if (!PhotonNetwork.IsMasterClient) return;

			Photon.Realtime.Player requestor = info.Sender;
			int actorID = requestor.ActorNumber;

			if (_hostGiveCooldown.TryGetValue(actorID, out float lastTime))
			{
				if (Time.time - lastTime < 2f) return;
			}
			_hostGiveCooldown[actorID] = Time.time;

			StartCoroutine(HostGiveItemsRoutine(clientHatName, requestor));
		}

		private IEnumerator HostGiveItemsRoutine(string hatName, Photon.Realtime.Player requestor)
		{
			var itemsToGive = PeakClassModPlugin.BuildStartItemsByHatName(hatName);
			if (itemsToGive.Count == 0) yield break;

			Character targetChar = null;

			int actorNumber = requestor.ActorNumber;
			if (_characterCache.TryGetValue(actorNumber, out Character cachedChar) && cachedChar != null)
			{
				targetChar = cachedChar;
			}
			else
			{
				foreach (var ch in Character.AllCharacters)
				{
					if (ch.photonView.Owner == requestor)
					{
						targetChar = ch;
						_characterCache[actorNumber] = ch;
						break;
					}
				}
			}

			if (targetChar == null || targetChar.player == null) yield break;

			PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Host giving {itemsToGive.Count} items to ActorNumber {requestor.ActorNumber}...");

			int itemsGiven = 0;
			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null)
				{
					if (targetChar.player.HasEmptySlot((ushort)item.itemID))
					{
						targetChar.player.AddItem((ushort)item.itemID, null, out _);
						itemsGiven++;
						PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Host gave {itemName}");
					}
				}
				yield return new WaitForSeconds(0.02f);
			}

			if (itemsGiven > 0)
			{
				yield return new WaitForSeconds(0.1f);
			}
		}

		[PunRPC]
		public void ShowMedicHealEffectRPC(int remainingHeals)
		{
			PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Medic heal used! Remaining: {remainingHeals}");
		}

		[PunRPC]
		public void RPC_SyncClassActivation(string className, int actorNumber)
		{
			PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Player {actorNumber} activated class: {className}");
		}
	}

	// ==================================================
	// UTILITIES
	// ==================================================
	public class PeakClassModCallbacks : MonoBehaviourPunCallbacks { }

	internal static class HatUtils
	{
		private static string _lastLocalHatName;
		internal static void SetLastLocalHatName(string hatName) { if (!string.IsNullOrEmpty(hatName)) _lastLocalHatName = hatName; }

		public static bool TryGetHatName(Character ch, out string hatName)
		{
			hatName = null;
			if (ch == null) return false;
			var ppds = GameHandler.GetService<PersistentPlayerDataService>();
			PersistentPlayerData data = null;
			if (ch.photonView != null && ch.photonView.Owner != null) data = ppds?.GetPlayerData(ch.photonView.Owner);
			if (data == null && ch.IsLocal && PhotonNetwork.LocalPlayer != null) data = ppds?.GetPlayerData(PhotonNetwork.LocalPlayer);

			if (data != null)
			{
				var hats = Singleton<Customization>.Instance?.hats;
				int i = data.customizationData.currentHat;
				if (hats != null && i >= 0 && i < hats.Length && hats[i] != null)
				{
					hatName = hats[i].name;
					return !string.IsNullOrEmpty(hatName);
				}
			}
			if (ch.IsLocal && !string.IsNullOrEmpty(_lastLocalHatName)) { hatName = _lastLocalHatName; return true; }
			return false;
		}

		public static bool HasHat(Character ch, string expected) => TryGetHatName(ch, out var name) && string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
		public static bool HasAnyHat(Character ch, params string[] names) { if (!TryGetHatName(ch, out var name)) return false; return names.Contains(name, StringComparer.OrdinalIgnoreCase); }
	}

	internal static class ClassCheckUtil
	{
		public static bool IsGeneral(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Bandana");
		public static bool IsTechPriest(Character ch) => HatUtils.HasHat(ch, "Hat_Crab");
		public static bool IsMedicHat(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Medic");
		public static bool IsMushroom(Character ch) => HatUtils.HasHat(ch, "Hat_Mushroom");
		public static bool IsRogueHat(Character ch) => HatUtils.HasHat(ch, "Hat_NinjaHeadband");
		public static bool IsGreatEpeeHat(Character ch) => HatUtils.HasHat(ch, "Hat_Propeller");
		public static bool IsAstronaut(Character ch) => HatUtils.HasHat(ch, "Hat_Astronaut");
	}

	internal static class ItemCache
	{
		private static Dictionary<string, Item> _byName;
		private static float _lastRefreshTime = 0f;

		public static void Refresh() { _byName = null; EnsureCache(); }

		private static void EnsureCache()
		{
			if (_byName != null && Time.time - _lastRefreshTime < 60f) return;

			_byName = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
			foreach (var it in Resources.FindObjectsOfTypeAll<Item>())
				if (it != null && !string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name))
					_byName[it.name] = it;

			_lastRefreshTime = Time.time;
		}

		public static Item Get(string name)
		{
			EnsureCache();
			_byName.TryGetValue(name, out var item);
			return item;
		}
	}

	// ==================================================
	// COMPONENT MANAGEMENT & PATCHES
	// ==================================================
	[HarmonyPatch(typeof(Character), "Start")]
	public class Character_Start_AddNetComp_Patch
	{
		static void Postfix(Character __instance)
		{
			if (__instance != null)
			{
				PeakClassModPlugin.Instance.EnsureComponent<ClassModNetwork>(__instance.gameObject);
				PeakClassModPlugin.Instance.EnsureComponent<AstronautStatHandler>(__instance.gameObject);
				PeakClassModPlugin.Instance.EnsureComponent<MedicStatHandler>(__instance.gameObject);
				PeakClassModPlugin.Instance.EnsureComponent<TechPriestWeightEnforcer>(__instance.gameObject);
			}
		}
	}

	// ==================================================
	// CLASS MANAGER & LOGIC
	// ==================================================
	public static class ClassManager
	{
		private static readonly Dictionary<int, float> lastStaminaZeroTime = new Dictionary<int, float>();

		public static void ResetAllFlags()
		{
			PeakClassModPlugin.scoutActive = false;
			PeakClassModPlugin.medicActive = false;
			PeakClassModPlugin.techPriestActive = false;
			PeakClassModPlugin.mushroomancerActive = false;
			PeakClassModPlugin.greatEpeeActive = false;
			PeakClassModPlugin.rogueActive = false;
			PeakClassModPlugin.generalActive = false;
			PeakClassModPlugin.astronautActive = false;
			PeakClassModPlugin.currentClass = "None";
		}

		public static void ActivateScoutClass() { ResetAllFlags(); PeakClassModPlugin.scoutActive = true; PeakClassModPlugin.currentClass = "Scout"; }
		public static void ActivateMedicClass() { ResetAllFlags(); PeakClassModPlugin.medicActive = true; PeakClassModPlugin.currentClass = "Medic"; }
		public static void ActivateTechPriestClass() { ResetAllFlags(); PeakClassModPlugin.techPriestActive = true; PeakClassModPlugin.currentClass = "Tech-Priest"; }
		public static void ActivateMushroomancerClass() { ResetAllFlags(); PeakClassModPlugin.mushroomancerActive = true; PeakClassModPlugin.currentClass = "Mushroomancer"; }
		public static void ActivateGreatEpeeClass() { ResetAllFlags(); PeakClassModPlugin.greatEpeeActive = true; PeakClassModPlugin.currentClass = "GreatEpee"; }
		public static void ActivateRogueClass() { ResetAllFlags(); PeakClassModPlugin.rogueActive = true; PeakClassModPlugin.currentClass = "Rogue"; if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = 2; }
		public static void ActivateGeneralClass() { ResetAllFlags(); PeakClassModPlugin.generalActive = true; PeakClassModPlugin.currentClass = "General"; }
		public static void ActivateAstronautClass() { ResetAllFlags(); PeakClassModPlugin.astronautActive = true; PeakClassModPlugin.currentClass = "Astronaut"; }
		public static void DeactivateAllClasses() { ResetAllFlags(); if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = 1; }

		public static bool IsScoutActive() => PeakClassModPlugin.scoutActive;
		public static bool IsMedicActive() => PeakClassModPlugin.medicActive;
		public static bool IsTechPriestActive() => PeakClassModPlugin.techPriestActive;
		public static bool IsMushroomancerActive() => PeakClassModPlugin.mushroomancerActive;
		public static bool IsGreatEpeeActive() => PeakClassModPlugin.greatEpeeActive;
		public static bool IsRogueActive() => PeakClassModPlugin.rogueActive;
		public static bool IsGeneralActive() => PeakClassModPlugin.generalActive;
		public static bool IsAstronautActive() => PeakClassModPlugin.astronautActive;

		private static readonly string[] ZeroStaminaFungi = new[] { "ShelfShroom", "BounceShroom", "CloudFungus" };

		public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
		{
			while (true)
			{
				if (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
				{
					// CORRECTION: Seuil à 2% (0.02) car la stamina va de 0 à 1.
					if (character.data.currentStamina <= 0.02f)
						TryActivateStaminaZeroMechanic(character);
				}
				yield return new WaitForSeconds(0.1f);
			}
		}

		public static void TryActivateStaminaZeroMechanic(Character character)
		{
			if (character == null || character.photonView?.Owner == null) return;
			int actor = character.photonView.Owner.ActorNumber;

			// CORRECTION: Cooldown de 10 secondes
			if (lastStaminaZeroTime.TryGetValue(actor, out var last) && Time.time - last < 2f) return;

			// On met à jour le temps MAINTENANT, car l'action se déclenche
			lastStaminaZeroTime[actor] = Time.time;

			// 1. APPLIQUER LA CURSE (Toujours, si le cooldown est bon)
			character.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.02f, false);

			// 2. TENTER DE SPAWN L'ITEM (Seulement si place dans l'inventaire)
			string randomName = ZeroStaminaFungi[UnityEngine.Random.Range(0, ZeroStaminaFungi.Length)];
			Item fungusItem = ItemCache.Get(randomName);

			if (fungusItem == null)
			{
				PeakClassModPlugin.Log?.LogWarning($"[Mushroomancer] Erreur: Item '{randomName}' introuvable.");
				return;
			}

			ushort id = (ushort)fungusItem.itemID;

			// La vérification se fait ici: Si inventaire plein, on ne rentre pas, donc pas d'item.
			if (character.player != null && character.player.HasEmptySlot(id))
			{
				character.player.AddItem(id, null, out _);
				PeakClassModPlugin.Log?.LogInfo($"[Mushroomancer] Stamina vide ! Curse + Spawn de {randomName}");
			}
			else
			{
				PeakClassModPlugin.Log?.LogInfo($"[Mushroomancer] Stamina vide ! Curse appliquée mais INVENTAIRE PLEIN (pas de spawn).");
			}
		}

		public static IEnumerator GreatEpeePermanentEffects(Character character)
		{
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				bool hasFaster = false;
				var afflictionList = character.refs.afflictions.afflictionList;
				for (int i = 0; i < afflictionList.Count; i++)
				{
					if (afflictionList[i].GetType().Name.Contains("FasterBoi"))
					{
						hasFaster = true;
						break;
					}
				}
				if (!hasFaster)
				{
					var fb = new Peak.Afflictions.Affliction_FasterBoi { totalTime = 99999f };
					character.refs.afflictions.AddAffliction(fb, false);
				}
				yield return new WaitForSeconds(2f);
			}
		}

		public static IEnumerator GreatEpeeStaminaRoutine(Character character)
		{
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				yield return new WaitForSeconds(UnityEngine.Random.Range(45f, 150f));
				character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);
			}
		}
	}

	// ==================================================
	// CLASS IMPLEMENTATIONS
	// ==================================================
	public class TechPriestWeightEnforcer : MonoBehaviour
	{
		private Character _character;
		private CharacterAfflictions _afflictions;
		private bool _isActive;
		private int _weightIndex;

		private void Start()
		{
			_character = GetComponent<Character>();
			_isActive = ClassCheckUtil.IsTechPriest(_character);
			_weightIndex = (int)CharacterAfflictions.STATUSTYPE.Weight;

			if (_character != null && _character.refs != null)
			{
				_afflictions = _character.refs.afflictions;
			}
		}

		private void LateUpdate()
		{
			if (!_isActive || !ClassManager.IsTechPriestActive())
			{
				if (Time.frameCount % 120 == 0 && _character != null)
				{
					_isActive = ClassCheckUtil.IsTechPriest(_character);
					if (_isActive && _character.refs != null)
					{
						_afflictions = _character.refs.afflictions;
					}
				}
				return;
			}

			if (_afflictions != null)
			{
				if (_afflictions.currentStatuses[_weightIndex] > 0f)
					_afflictions.currentStatuses[_weightIndex] = 0f;
			}
		}
	}

	public class AstronautStatHandler : MonoBehaviour
	{
		private Character _character;
		private CharacterMovement _movement;
		private Traverse _travMove;
		private float _defJump = 400f;
		private float _defSprintMult = 2.0f;
		private bool _defaultsCaptured = false;
		private bool _isModified = false;

		private Traverse _jumpImpulseField;
		private Traverse _sprintMultiplierField;
		private Traverse _velocityField;
		private bool _fieldsInitialized = false;

		private void Start()
		{
			_character = GetComponent<Character>();
			_movement = GetComponent<CharacterMovement>();
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal || !ClassManager.IsAstronautActive()) return;
			if (_character.input.jumpWasPressed && _character.data.isGrounded)
			{
				StopAllCoroutines();
				StartCoroutine(ApplyWindWalkDelayed());
			}
		}

		private IEnumerator ApplyWindWalkDelayed()
		{
			yield return new WaitForSeconds(1f);
			if (ClassManager.IsAstronautActive() && _character != null)
				if (!_character.data.isGrounded) TriggerWindWalk();
		}

		private void TriggerWindWalk()
		{
			try
			{
				_character.refs.afflictions.AddAffliction(new Affliction_LowGravity(3, 3f), true);
			}
			catch (Exception e)
			{
				PeakClassModPlugin.Log?.LogError($"[Astronaut] WindWalk Error: {e.Message}");
			}
		}

		private void InitializeFields()
		{
			if (_fieldsInitialized || _travMove == null) return;

			_jumpImpulseField = _travMove.Field("jumpImpulse");
			_sprintMultiplierField = _travMove.Field("sprintMultiplier");
			_velocityField = _travMove.Field("velocity");
			_fieldsInitialized = true;
		}

		private void FixedUpdate()
		{
			if (_character == null || !_character.IsLocal || _movement == null) return;

			if (_travMove == null)
			{
				_travMove = Traverse.Create(_movement);
				_fieldsInitialized = false;
			}

			if (!_fieldsInitialized)
			{
				InitializeFields();
			}

			if (ClassManager.IsAstronautActive())
			{
				if (!_defaultsCaptured && !_isModified)
				{
					float? j = _jumpImpulseField?.GetValue<float>();
					if (j.HasValue && j.Value > 0) _defJump = j.Value;
					float? sm = _sprintMultiplierField?.GetValue<float>();
					if (sm.HasValue) _defSprintMult = sm.Value;
					_defaultsCaptured = true;
				}

				_jumpImpulseField?.SetValue(1200f);
				_sprintMultiplierField?.SetValue(1.7f);

				if (_character.data.isGrounded)
				{
					var afflictions = _character.refs.afflictions.afflictionList;
					bool hasLowGrav = false;

					for (int i = 0; i < afflictions.Count; i++)
					{
						if (afflictions[i] is Affliction_LowGravity)
						{
							hasLowGrav = true;
							break;
						}
					}

					if (hasLowGrav)
					{
						for (int i = afflictions.Count - 1; i >= 0; i--)
							if (afflictions[i] is Affliction_LowGravity)
								_character.refs.afflictions.RemoveAffliction(afflictions[i]);
					}
				}

				if (!_character.data.isSprinting && _velocityField != null && _velocityField.FieldExists())
				{
					Vector3 currentVel = _velocityField.GetValue<Vector3>();
					Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
					if (horizontalVel.magnitude > 1.6f)
					{
						Vector3 clamped = horizontalVel.normalized * 1.6f;
						_velocityField.SetValue(new Vector3(clamped.x, currentVel.y, clamped.z));
					}
				}
				_isModified = true;
			}
			else if (_isModified)
			{
				_jumpImpulseField?.SetValue(_defJump);
				_sprintMultiplierField?.SetValue(_defSprintMult);
				_isModified = false;
				_defaultsCaptured = false;
			}
		}
	}

	public class MedicStatHandler : MonoBehaviour
	{
		private Character _character;
		private ClassModNetwork _network;
		private CharacterAfflictions _afflictions;
		private int _healsRemaining = 10;
		private float _lastHealTime = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
			_network = GetComponent<ClassModNetwork>();
			_healsRemaining = 10;

			if (_character != null && _character.refs != null)
			{
				_afflictions = _character.refs.afflictions;
			}
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			if (ClassManager.IsMedicActive())
			{
				if (Input.GetKeyDown(KeyCode.H) && Time.time - _lastHealTime > 1f)
					TrySelfHeal();
			}
			else if (_healsRemaining < 10)
			{
				_healsRemaining = 10;
			}
		}

		private void TrySelfHeal()
		{
			if (_healsRemaining <= 0) return;

			if (_afflictions == null && _character.refs != null)
			{
				_afflictions = _character.refs.afflictions;
			}

			if (_afflictions != null && _afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f)
			{
				_healsRemaining--;
				_lastHealTime = Time.time;
				_afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.1f, false);

				if (_network != null && _network.photonView != null)
					_network.photonView.RPC("ShowMedicHealEffectRPC", RpcTarget.All, _healsRemaining);
			}
		}
	}

	// ==================================================
	// HARMONY PATCHES
	// ==================================================

	[HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
	public class SetHat_Patch
	{
		static void Postfix(int index)
		{
			var hats = Singleton<Customization>.Instance?.hats;
			string hatName = hats != null && index >= 0 && index < hats.Length ? hats[index]?.name : "Unknown";
			HatUtils.SetLastLocalHatName(hatName);

			if (index == 12) ClassManager.ActivateScoutClass();
			else if (hatName == "Hat_Medic" || hatName == "MedicHelmet") ClassManager.ActivateMedicClass();
			else if (hatName == "Hat_Crab") ClassManager.ActivateTechPriestClass();
			else if (hatName == "Hat_Mushroom") ClassManager.ActivateMushroomancerClass();
			else if (hatName == "Hat_Propeller") ClassManager.ActivateGreatEpeeClass();
			else if (hatName == "Hat_NinjaHeadband") ClassManager.ActivateRogueClass();
			else if (hatName == "Hat_Bandana" || hatName == "Hat_Captain") ClassManager.ActivateGeneralClass();
			else if (hatName == "Hat_Astronaut") ClassManager.ActivateAstronautClass();
			else ClassManager.DeactivateAllClasses();

			if (Character.localCharacter != null && GUIManager.instance?.bar != null)
				GUIManager.instance.bar.ChangeBar();
		}
	}

	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix()
		{
			PeakClassModPlugin.Instance.StartCoroutine(AddEffectsDelayed());
		}

		public static IEnumerator AddEffectsDelayed()
		{
			yield return new WaitForSeconds(1.5f);

			var lc = Character.localCharacter;
			if (lc == null)
			{
				yield return new WaitForSeconds(1f);
				lc = Character.localCharacter;
				if (lc == null) yield break;
			}

			if (ClassCheckUtil.IsMushroom(lc))
			{
				ClassManager.ActivateMushroomancerClass();
				lc.StartCoroutine(ClassManager.MushroomancerLocalStaminaWatch(lc));
			}
			else if (ClassCheckUtil.IsGeneral(lc))
			{
				ClassManager.ActivateGeneralClass();
				float currentCurse = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f;
				if (currentCurse < 0.3f)
					lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.35f);
			}
			else if (ClassCheckUtil.IsGreatEpeeHat(lc))
			{
				ClassManager.ActivateGreatEpeeClass();
				lc.StartCoroutine(ClassManager.GreatEpeePermanentEffects(lc));
				lc.StartCoroutine(ClassManager.GreatEpeeStaminaRoutine(lc));
			}
			else if (ClassCheckUtil.IsMedicHat(lc))
			{
				ClassManager.ActivateMedicClass();
				float currentCurse = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f;
				if (currentCurse < 0.3f)
					lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.15f);
			}
			else if (ClassCheckUtil.IsAstronaut(lc))
			{
				ClassManager.ActivateAstronautClass();
			}
			else if (ClassCheckUtil.IsTechPriest(lc))
			{
				ClassManager.ActivateTechPriestClass();
			}
			else if (ClassCheckUtil.IsRogueHat(lc))
			{
				ClassManager.ActivateRogueClass();
			}
			else if (HatUtils.HasHat(lc, "Hat_Scout"))
			{
				ClassManager.ActivateScoutClass();
			}
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
	public class General_AddStatus_Patch
	{
		static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			var ch = __instance.character;
			if (ch == null) return true;

			if (ch.IsLocal)
			{
				if (ClassManager.IsAstronautActive() && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
					amount *= 1.3f;
				if (ClassManager.IsScoutActive() && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
					amount *= 1.7f;
				if (ClassManager.IsGeneralActive() && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
					amount *= 2f;
			}

			if (statusType == CharacterAfflictions.STATUSTYPE.Hunger && ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
				amount = 0f;
			if (statusType == CharacterAfflictions.STATUSTYPE.Weight && ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
			{
				amount = 0f;
				return false;
			}
			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && ClassCheckUtil.IsMushroom(ch))
				amount = 0f;

			return true;
		}
	}

	[HarmonyPatch(typeof(BarAffliction), "ChangeAffliction")]
	public class BarAffliction_Patch
	{
		static void Postfix(BarAffliction __instance)
		{
			if (Character.localCharacter == null || __instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight) return;
			if (ClassManager.IsScoutActive()) __instance.size *= 0.5f;
			if (ClassManager.IsGeneralActive()) __instance.size *= 2f;
			if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(Character.localCharacter))
			{
				__instance.size = 0f;
				var img = __instance.GetComponent<Image>();
				if (img != null) img.enabled = false;
			}
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "get_statusSum")]
	public class StatusSum_Patch
	{
		static void Postfix(CharacterAfflictions __instance, ref float __result)
		{
			if (__instance.character == null || !__instance.character.IsLocal) return;
			float weight = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight);
			if (ClassManager.IsScoutActive()) __result = __result - weight + (weight * 0.5f);
			if (ClassManager.IsGeneralActive()) __result = __result - weight + (weight * 2f);
			if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character)) __result -= weight;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
	public class UpdateStatus_TechPriest_Patch
	{
		static void Postfix(CharacterAfflictions __instance)
		{
			if (ClassCheckUtil.IsTechPriest(__instance.character) && ClassManager.IsTechPriestActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Hunger] = 0f;
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Weight] = 0f;
			}
		}
	}

	internal static class TechPriestBlocker
	{
		private static Dictionary<int, bool> _blockCache = new Dictionary<int, bool>();

		public static bool ShouldBlockItem(Item item)
		{
			if (item == null) return false;

			int instanceId = item.GetInstanceID();
			if (_blockCache.TryGetValue(instanceId, out bool cached))
				return cached;

			bool shouldBlock = false;

			if (item.GetComponent<Action_RestoreHunger>() != null || item.GetComponent<Action_GiveExtraStamina>() != null)
				shouldBlock = true;

			if (!shouldBlock)
			{
				string name = item.name.Replace("(Clone)", "").Trim().ToLower();
				if (name.Contains("bandage") || name.Contains("firstaid") || name.Contains("medkit") ||
					name.Contains("cure") || name.Contains("marshmallow"))
					shouldBlock = true;
			}

			if (!shouldBlock)
			{
				foreach (var comp in item.GetComponents<MonoBehaviour>())
				{
					if (comp == null) continue;
					string typeName = comp.GetType().Name.ToLower();
					if (typeName.Contains("heal") || typeName.Contains("cure") ||
						typeName.Contains("medicine") || typeName.Contains("restorehealth"))
					{
						shouldBlock = true;
						break;
					}
				}
			}

			_blockCache[instanceId] = shouldBlock;
			return shouldBlock;
		}

		public static void ClearCache()
		{
			_blockCache.Clear();
		}
	}

	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	public class TechPriestConsumablePatch1
	{
		static bool Prefix(Item __instance)
		{
			if (ClassCheckUtil.IsTechPriest(__instance?.holderCharacter))
			{
				if (TechPriestBlocker.ShouldBlockItem(__instance)) return false;
				var affAction = __instance.GetComponent<Action_ApplyAffliction>();
				if (affAction != null && affAction.affliction != null && affAction.affliction.GetType().Name.Contains("FasterBoi"))
					return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	public class TechPriestConsumablePatch2
	{
		static bool Prefix(Item __instance)
		{
			if (ClassCheckUtil.IsTechPriest(__instance?.holderCharacter))
			{
				if (TechPriestBlocker.ShouldBlockItem(__instance)) return false;
				var affAction = __instance.GetComponent<Action_ApplyAffliction>();
				if (affAction != null && affAction.affliction != null && affAction.affliction.GetType().Name.Contains("FasterBoi"))
					return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_FasterBoi), "OnApplied")]
	public class TechPriestFasterBoiBlock
	{
		static bool Prefix(Peak.Afflictions.Affliction_FasterBoi __instance)
		{
			return !ClassCheckUtil.IsTechPriest(__instance.character);
		}
	}

	[HarmonyPatch(typeof(Character), "AddExtraStamina")]
	public class TechPriestStaminaPatch1
	{
		static bool Prefix(Character __instance) => !ClassCheckUtil.IsTechPriest(__instance);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "OnApplied")]
	public class TechPriestStaminaPatch2
	{
		static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => !ClassCheckUtil.IsTechPriest(__instance.character);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "UpdateEffect")]
	public class TechPriestStaminaPatch2_Update
	{
		static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => !ClassCheckUtil.IsTechPriest(__instance.character);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_PoisonOverTime), "OnApplied")]
	public class PoisonPatch
	{
		static bool Prefix(Peak.Afflictions.Affliction_PoisonOverTime __instance) => !ClassCheckUtil.IsMushroom(__instance.character);
	}

	[HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
	public class RogueJumpPatch
	{
		static void Postfix(CharacterMovement __instance)
		{
			var ch = __instance.GetComponent<Character>();
			if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
				ch.data.jumpsRemaining = 2;
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
	public class RogueJumpRPCPatch
	{
		static bool Prefix(CharacterMovement __instance)
		{
			var ch = __instance.GetComponent<Character>();
			if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
			{
				if (ch.data.jumpsRemaining > 0 && ch.data.currentStamina > 0.005f)
				{
					ch.data.jumpsRemaining--;
					ch.data.chargingJump = true;
					ch.refs.view.RPC("JumpRpc", RpcTarget.All, new object[] { false });
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Character), "UseStamina")]
	public class Character_UseStamina_Patch
	{
		static void Prefix(Character __instance, ref float usage)
		{
			if (ClassManager.IsScoutActive()) usage *= 0.80f;
			if (ClassManager.IsRogueActive()) usage *= 2.0f;
		}
	}

	[HarmonyPatch(typeof(Character), "ShowMedicHealEffectRPC")]
	public class MedicRPC_Patch
	{
		static void Postfix() { }
	}
}