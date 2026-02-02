// ============================================================
// PEAK Class Mod - Version: 6.0.0 (Clean Slate & Phoenix Fix)
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
	// ==================================================
	// 1. CONFIGURATION & BALANCE
	// ==================================================
	public static class ClassBalance
	{
		// --- GOBELIN ---
		public const string GOBELIN_HAT = "Hat_Midsummer"; // Nom exact (s minuscule)
		public const string GOBELIN_WEAPON = "HealingDart Variant"; // Nom exact de l'item
		public const float GOBELIN_SIZE = 0.5f;
		public const float GOBELIN_CURSE_PER_SHOT = 0.05f;

		// --- AUTRES CLASSES ---
		public const float ASTRONAUT_JUMP_IMPULSE = 1200f;
		public const float ASTRONAUT_SPRINT_MULT = 1.7f;
		public const float ASTRONAUT_HUNGER_MULT = 1.3f;
		public const float ASTRONAUT_MAX_WALK_SPEED = 1.6f;
		public const float ASTRONAUT_LOW_GRAV_DURATION = 3f;

		public const float GENERAL_CURSE_MIN = 0.35f;
		public const float GENERAL_HUNGER_MULT = 2.0f;
		public const float GENERAL_WEIGHT_MULT = 2.0f;

		public const float MEDIC_CURSE = 0.15f;
		public const int MEDIC_MAX_HEALS = 10;
		public const float MEDIC_HEAL_AMOUNT = 0.1f;
		public const float MEDIC_HEAL_COOLDOWN = 1f;

		public const float SCOUT_STAMINA_MULT = 0.80f;
		public const float SCOUT_HUNGER_MULT = 1.7f;
		public const float SCOUT_WEIGHT_MULT = 0.5f;

		public const float ROGUE_STAMINA_MULT = 2.0f;
		public const int ROGUE_EXTRA_JUMPS = 2;

		public const float GREATEPEE_SLEEP_MIN = 45f;
		public const float GREATEPEE_SLEEP_MAX = 150f;
		public const float GREATEPEE_EFFECT_DURATION = 99999f;

		public const float MUSHROOM_STAMINA_COOLDOWN = 10f;
		public const float MUSHROOM_CURSE_AMOUNT = 0.02f;

		// --- SYSTÈME ---
		public const float ITEM_REQUEST_DELAY = 0.5f;
		public const float ITEM_GIVE_COOLDOWN = 2f;
		public const float ITEM_CACHE_DURATION = 300f;
		public const float SIZE_CHECK_INTERVAL = 0.5f;
	}

	// ==================================================
	// 2. PLUGIN PRINCIPAL
	// ==================================================
	[BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "6.0.0")]
	public class PeakClassModPlugin : BaseUnityPlugin
	{
		public static PeakClassModPlugin Instance;
		internal static ManualLogSource Log;
		private static Harmony harmony;

		// Flags d'état
		public static bool scoutActive = false;
		public static bool medicActive = false;
		public static bool techPriestActive = false;
		public static bool mushroomancerActive = false;
		public static bool greatEpeeActive = false;
		public static bool rogueActive = false;
		public static bool generalActive = false;
		public static bool astronautActive = false;
		public static bool gobelinActive = false;
		public static string currentClass = "None";

		private GameObject _callbacksGO;

		private void Awake()
		{
			Instance = this;
			Log = Logger;
			harmony = new Harmony("com.peakmod.classmod");
			harmony.PatchAll();

			// Création d'un objet pour gérer les callbacks réseau
			_callbacksGO = new GameObject("PeakClassModCallbacks");
			GameObject.DontDestroyOnLoad(_callbacksGO);
			_callbacksGO.AddComponent<PeakClassModCallbacks>();

			Log?.LogInfo("[ClassMod] Ready. Version 6.0.0 (Clean Slate).");
		}

		private void OnDestroy()
		{
			try { harmony?.UnpatchSelf(); } catch { }
			if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
		}

		// Helper pour ajouter des composants en sécurité
		public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
		{
			if (go != null && go.GetComponent<T>() == null) go.AddComponent<T>();
		}

		// Liste des items de départ
		public static List<string> BuildStartItemsByHatName(string hatName)
		{
			var items = new List<string>();
			if (string.IsNullOrEmpty(hatName)) return items;

			// Utilisation de Contains pour éviter les erreurs de casse/espaces
			if (hatName.Contains("Hat_Mushroom"))
			{
				for (int i = 0; i < 3; i++) items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
			}
			else if (hatName.Contains("Hat_Medic") || hatName.Contains("MedicHelmet"))
			{
				items.Add("FirstAidKit"); items.Add("Cure-All");
			}
			else if (hatName.Contains("Hat_Crab"))
			{
				items.Add("ChainShooter"); items.Add("ChainShooter");
			}
			else if (hatName.Contains("Hat_Ninja"))
			{
				items.Add("RopeSpool"); items.Add("RopeSpool");
			}
			else if (hatName.Contains("Hat_Astronaut"))
			{
				items.Add("BalloonBunch");
			}
			else if (hatName.Contains(ClassBalance.GOBELIN_HAT)) // Detection Gobelin
			{
				items.Add(ClassBalance.GOBELIN_WEAPON);
			}

			return items;
		}

		private static readonly string[] StartBerries = new[] { "Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green" };
	}

	// ==================================================
	// 3. LOGIQUE GOBELIN (Diet & Infinite)
	// ==================================================
	internal static class GobelinLogic
	{
		private static readonly string[] ForbiddenFoodWords = new string[]
		{
			"Packaged", "Airplane", "Can", "Soda", "Candy", "Chip", "Cookie", "Snack", "Chocolate"
		};

		public static bool IsPackagedFood(string itemName)
		{
			if (string.IsNullOrEmpty(itemName)) return false;
			string cleanName = itemName.Replace("(Clone)", "").Trim();
			foreach (var word in ForbiddenFoodWords)
			{
				if (cleanName.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			}
			return false;
		}

		// --- HELPER REFLECTION POUR ITEMSLOT ---
		// Cette fonction résout l'erreur 'ItemSlot ne contient pas de définition pour item'
		// Elle cherche dynamiquement le contenu du slot.
		public static Item GetItemFromSlot(object slot)
		{
			if (slot == null) return null;
			Type type = slot.GetType();

			// 1. Chercher un champ public (ex: public Item item;)
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (field.FieldType == typeof(Item)) return field.GetValue(slot) as Item;
			}

			// 2. Chercher une propriété publique (ex: public Item Item { get; })
			foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (prop.PropertyType == typeof(Item) && prop.CanRead) return prop.GetValue(slot) as Item;
			}

			return null;
		}
	}

	// PATCH NOURRITURE
	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	public class GobelinDietPatch
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				// Si c'est de la nourriture industrielle -> BLOQUER
				if (__instance.GetComponent<Action_RestoreHunger>() != null &&
					GobelinLogic.IsPackagedFood(__instance.name))
				{
					PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Beurk ! {__instance.name} refusé.");
					return false; // Bloque l'action
				}

				// GESTION BLOWGUN (Curse + Lancement du Phénix)
				if (__instance.name.IndexOf(ClassBalance.GOBELIN_WEAPON, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// 1. Curse immédiate
					__instance.holderCharacter.refs?.afflictions?.AddStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.GOBELIN_CURSE_PER_SHOT,
						false);

					// 2. Lancer la réapparition
					ClassManager.TriggerGobelinRefill(__instance.holderCharacter);
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	public class GobelinDietPatchSecondary
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;
			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				if (__instance.GetComponent<Action_RestoreHunger>() != null &&
					GobelinLogic.IsPackagedFood(__instance.name))
				{
					return false; // Bloque l'action secondaire aussi
				}
			}
			return true;
		}
	}

	// GESTION TAILLE GOBELIN
	public class GobelinSizeHandler : MonoBehaviour
	{
		private Character _character;
		private bool _sizeApplied = false;
		private float _lastCheckTime = 0f;
		private void Start() { _character = GetComponent<Character>(); }
		private void Update()
		{
			if (Time.time - _lastCheckTime < ClassBalance.SIZE_CHECK_INTERVAL) return;
			_lastCheckTime = Time.time;
			if (_character == null || !_character.IsLocal) return;

			bool isGob = ClassManager.IsGobelinActive();
			if (isGob && !_sizeApplied)
			{
				_character.transform.localScale = Vector3.one * ClassBalance.GOBELIN_SIZE;
				_sizeApplied = true;
			}
			else if (!isGob && _sizeApplied)
			{
				_character.transform.localScale = Vector3.one;
				_sizeApplied = false;
			}
		}
	}

	// ==================================================
	// 4. GESTIONNAIRE DE CLASSES & LOGIQUE CENTRALE
	// ==================================================
	public static class ClassManager
	{
		private static readonly Dictionary<int, float> lastStaminaZeroTime = new Dictionary<int, float>();
		private static Coroutine _mushroomCoroutine;
		private static Coroutine _greatEpeeEffectsCoroutine;
		private static Coroutine _greatEpeeStaminaCoroutine;

		// --- PHOENIX METHOD (Respawn Sarbacane) ---
		public static void TriggerGobelinRefill(Character character)
		{
			if (character != null) character.StartCoroutine(RefillBlowgunRoutine(character));
		}

		private static IEnumerator RefillBlowgunRoutine(Character character)
		{
			// On attend 0.1s que le jeu détruise l'item
			yield return new WaitForSeconds(0.1f);

			if (character == null || character.player == null || character.player.itemSlots == null) yield break;

			bool hasBlowgun = false;
			foreach (var slot in character.player.itemSlots)
			{
				// Utilisation du Helper Reflection pour éviter l'erreur de compilation
				Item itemInSlot = GobelinLogic.GetItemFromSlot(slot);

				if (itemInSlot != null)
				{
					// Vérification souple du nom
					if (itemInSlot.name.IndexOf(ClassBalance.GOBELIN_WEAPON, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						hasBlowgun = true;
						break;
					}
				}
			}

			// Si on ne trouve plus l'arme, on la redonne
			if (!hasBlowgun)
			{
				PeakClassModPlugin.Log?.LogInfo("[Gobelin] Sarbacane consumée -> Respawn immédiat !");
				Item itemToAdd = ItemCache.Get(ClassBalance.GOBELIN_WEAPON);
				if (itemToAdd != null)
				{
					// AddItem renvoie true si réussi
					character.player.AddItem((ushort)itemToAdd.itemID, null, out _);
				}
				else
				{
					PeakClassModPlugin.Log?.LogError($"[Gobelin] Impossible de trouver l'item '{ClassBalance.GOBELIN_WEAPON}' dans le cache pour le respawn.");
				}
			}
		}
		// ------------------------------------------

		public static void ResetAllFlags()
		{
			var lc = Character.localCharacter;
			if (lc != null)
			{
				if (_mushroomCoroutine != null) lc.StopCoroutine(_mushroomCoroutine);
				if (_greatEpeeEffectsCoroutine != null) lc.StopCoroutine(_greatEpeeEffectsCoroutine);
				if (_greatEpeeStaminaCoroutine != null) lc.StopCoroutine(_greatEpeeStaminaCoroutine);
				lc.transform.localScale = Vector3.one;
			}
			PeakClassModPlugin.scoutActive = false; PeakClassModPlugin.medicActive = false; PeakClassModPlugin.techPriestActive = false;
			PeakClassModPlugin.mushroomancerActive = false; PeakClassModPlugin.greatEpeeActive = false; PeakClassModPlugin.rogueActive = false;
			PeakClassModPlugin.generalActive = false; PeakClassModPlugin.astronautActive = false; PeakClassModPlugin.gobelinActive = false;
			PeakClassModPlugin.currentClass = "None";
		}

		// Activators
		public static void ActivateScoutClass() { ResetAllFlags(); PeakClassModPlugin.scoutActive = true; PeakClassModPlugin.currentClass = "Scout"; }
		public static void ActivateMedicClass() { ResetAllFlags(); PeakClassModPlugin.medicActive = true; PeakClassModPlugin.currentClass = "Medic"; }
		public static void ActivateTechPriestClass() { ResetAllFlags(); PeakClassModPlugin.techPriestActive = true; PeakClassModPlugin.currentClass = "Tech-Priest"; }
		public static void ActivateMushroomancerClass() { ResetAllFlags(); PeakClassModPlugin.mushroomancerActive = true; PeakClassModPlugin.currentClass = "Mushroomancer"; }
		public static void ActivateGreatEpeeClass() { ResetAllFlags(); PeakClassModPlugin.greatEpeeActive = true; PeakClassModPlugin.currentClass = "GreatEpee"; }
		public static void ActivateRogueClass() { ResetAllFlags(); PeakClassModPlugin.rogueActive = true; PeakClassModPlugin.currentClass = "Rogue"; if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; }
		public static void ActivateGeneralClass() { ResetAllFlags(); PeakClassModPlugin.generalActive = true; PeakClassModPlugin.currentClass = "General"; }
		public static void ActivateAstronautClass() { ResetAllFlags(); PeakClassModPlugin.astronautActive = true; PeakClassModPlugin.currentClass = "Astronaut"; }
		public static void ActivateGobelinClass() { ResetAllFlags(); PeakClassModPlugin.gobelinActive = true; PeakClassModPlugin.currentClass = "Gobelin"; if (Character.localCharacter != null) Character.localCharacter.transform.localScale = Vector3.one * ClassBalance.GOBELIN_SIZE; }
		public static void DeactivateAllClasses() { ResetAllFlags(); if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = 1; }

		// Getters
		public static bool IsScoutActive() => PeakClassModPlugin.scoutActive;
		public static bool IsMedicActive() => PeakClassModPlugin.medicActive;
		public static bool IsTechPriestActive() => PeakClassModPlugin.techPriestActive;
		public static bool IsMushroomancerActive() => PeakClassModPlugin.mushroomancerActive;
		public static bool IsGreatEpeeActive() => PeakClassModPlugin.greatEpeeActive;
		public static bool IsRogueActive() => PeakClassModPlugin.rogueActive;
		public static bool IsGeneralActive() => PeakClassModPlugin.generalActive;
		public static bool IsAstronautActive() => PeakClassModPlugin.astronautActive;
		public static bool IsGobelinActive() => PeakClassModPlugin.gobelinActive;

		// Mushroomancer Logic
		public static void StartMushroomancerWatch(Character character) { if (character != null) _mushroomCoroutine = character.StartCoroutine(MushroomancerLocalStaminaWatch(character)); }
		public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
		{
			while (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
			{
				if (character.data.currentStamina <= 0.02f) TryActivateStaminaZeroMechanic(character);
				yield return new WaitForSeconds(0.1f);
			}
		}
		public static void TryActivateStaminaZeroMechanic(Character character)
		{
			if (character?.photonView?.Owner == null) return;
			int actor = character.photonView.Owner.ActorNumber;
			if (lastStaminaZeroTime.TryGetValue(actor, out var last) && Time.time - last < ClassBalance.MUSHROOM_STAMINA_COOLDOWN) return;
			lastStaminaZeroTime[actor] = Time.time;
			character.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MUSHROOM_CURSE_AMOUNT, false);
			string[] fungi = { "ShelfShroom", "BounceShroom", "CloudFungus" };
			Item fungusItem = ItemCache.Get(fungi[UnityEngine.Random.Range(0, fungi.Length)]);
			if (fungusItem != null && character.player != null) character.player.AddItem((ushort)fungusItem.itemID, null, out _);
		}

		// GreatEpee Logic
		public static void StartGreatEpeeRoutines(Character character)
		{
			if (character != null)
			{
				_greatEpeeEffectsCoroutine = character.StartCoroutine(GreatEpeePermanentEffects(character));
				_greatEpeeStaminaCoroutine = character.StartCoroutine(GreatEpeeStaminaRoutine(character));
			}
		}
		public static IEnumerator GreatEpeePermanentEffects(Character character)
		{
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				bool hasFaster = false;
				var list = character.refs?.afflictions?.afflictionList;
				if (list != null) { for (int i = 0; i < list.Count; i++) if (list[i].GetType().Name.Contains("FasterBoi")) { hasFaster = true; break; } }
				if (!hasFaster) character.refs.afflictions.AddAffliction(new Peak.Afflictions.Affliction_FasterBoi { totalTime = ClassBalance.GREATEPEE_EFFECT_DURATION }, false);
				yield return new WaitForSeconds(2f);
			}
		}
		public static IEnumerator GreatEpeeStaminaRoutine(Character character)
		{
			yield return new WaitForSeconds(5f);
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				while (character?.refs?.afflictions != null && character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0.05f) { yield return new WaitForSeconds(1f); if (!PeakClassModPlugin.greatEpeeActive) yield break; }
				if (character == null) yield break;
				yield return new WaitForSeconds(UnityEngine.Random.Range(ClassBalance.GREATEPEE_SLEEP_MIN, ClassBalance.GREATEPEE_SLEEP_MAX));
				if (character?.refs?.afflictions == null || !PeakClassModPlugin.greatEpeeActive) yield break;
				if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) < 0.1f) character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);
			}
		}
	}

	// ==================================================
	// 5. UTILS & HELPERS
	// ==================================================
	internal static class ClassCheckUtil
	{
		public static bool IsGeneral(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Bandana", "Hat_Captain");
		public static bool IsTechPriest(Character ch) => HatUtils.HasHat(ch, "Hat_Crab");
		public static bool IsMedicHat(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Medic", "MedicHelmet");
		public static bool IsMushroom(Character ch) => HatUtils.HasHat(ch, "Hat_Mushroom");
		public static bool IsRogueHat(Character ch) => HatUtils.HasHat(ch, "Hat_NinjaHeadband");
		public static bool IsGreatEpeeHat(Character ch) => HatUtils.HasHat(ch, "Hat_Propeller");
		public static bool IsAstronaut(Character ch) => HatUtils.HasHat(ch, "Hat_Astronaut");
		public static bool IsGobelin(Character ch) => HatUtils.HasHat(ch, ClassBalance.GOBELIN_HAT);
		public static bool IsScout(Character ch) => HatUtils.HasHat(ch, "Hat_Scout");
	}

	internal static class HatUtils
	{
		private static string _lastLocalHatName;
		internal static void SetLastLocalHatName(string hatName) { if (!string.IsNullOrEmpty(hatName)) _lastLocalHatName = hatName; }
		public static bool TryGetHatName(Character ch, out string hatName)
		{
			hatName = null; if (ch == null) return false;
			var ppds = GameHandler.GetService<PersistentPlayerDataService>();
			PersistentPlayerData data = null;
			if (ch.photonView?.Owner != null) data = ppds?.GetPlayerData(ch.photonView.Owner);
			if (data == null && ch.IsLocal && PhotonNetwork.LocalPlayer != null) data = ppds?.GetPlayerData(PhotonNetwork.LocalPlayer);
			if (data != null) { var hats = Singleton<Customization>.Instance?.hats; int i = data.customizationData.currentHat; if (hats != null && i >= 0 && i < hats.Length && hats[i] != null) hatName = hats[i].name; }
			if (string.IsNullOrEmpty(hatName) && ch.IsLocal) hatName = _lastLocalHatName;
			return !string.IsNullOrEmpty(hatName);
		}
		public static bool HasAnyHat(Character ch, params string[] names) { if (!TryGetHatName(ch, out var name)) return false; return names.Contains(name, StringComparer.OrdinalIgnoreCase); }
		public static bool HasHat(Character ch, string expected) => TryGetHatName(ch, out var name) && name.Contains(expected);
	}

	internal static class ItemCache
	{
		private static Dictionary<string, Item> _byName;
		private static float _lastRefreshTime = 0f;
		public static Item Get(string name)
		{
			if (_byName == null || Time.time - _lastRefreshTime > ClassBalance.ITEM_CACHE_DURATION)
			{
				_byName = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
				foreach (var it in Resources.FindObjectsOfTypeAll<Item>()) if (it != null && !string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name)) _byName[it.name] = it;
				_lastRefreshTime = Time.time;
			}
			return _byName.TryGetValue(name, out var item) ? item : null;
		}
	}

	// ==================================================
	// 6. RESEAU & COMPOSANTS
	// ==================================================
	public class ClassModNetwork : MonoBehaviourPun
	{
		private Character _character;
		private bool _hasRequestedItems = false;
		private static Dictionary<int, float> _hostGiveCooldown = new Dictionary<int, float>();
		private static Dictionary<int, Character> _characterCache = new Dictionary<int, Character>();

		private void Awake() { var c = GetComponents<ClassModNetwork>(); if (c.Length > 1) { Destroy(this); return; } _character = GetComponent<Character>(); }
		private void Start() { if (photonView != null && photonView.IsMine && !_hasRequestedItems) { _hasRequestedItems = true; StartCoroutine(RequestItemsRoutine()); } }
		private void OnDestroy() { if (_character?.photonView?.Owner != null) _characterCache.Remove(_character.photonView.Owner.ActorNumber); }
		public static void ClearPlayerCache(int actorNumber) { _characterCache.Remove(actorNumber); _hostGiveCooldown.Remove(actorNumber); }

		private IEnumerator RequestItemsRoutine()
		{
			yield return new WaitForSeconds(ClassBalance.ITEM_REQUEST_DELAY);
			while (!PhotonNetwork.InRoom) yield return null;
			if (_character == null) _character = GetComponent<Character>();
			float timeout = 5f; bool inventoryReady = false;
			while (timeout > 0) { if (_character?.player?.itemSlots != null && _character.player.itemSlots.Length > 0 && !_character.player.itemSlots[0].IsEmpty()) { inventoryReady = true; break; } timeout -= 0.1f; yield return new WaitForSeconds(0.1f); }
			string myHatName = "None"; if (HatUtils.TryGetHatName(_character, out string d)) myHatName = d;
			if (inventoryReady || timeout <= 0) photonView.RPC("RPC_HostGiveItems", RpcTarget.MasterClient, myHatName);
		}

		[PunRPC]
		public void RPC_HostGiveItems(string clientHatName, PhotonMessageInfo info)
		{
			if (!PhotonNetwork.IsMasterClient) return;
			int actorID = info.Sender.ActorNumber;
			if (_hostGiveCooldown.TryGetValue(actorID, out float lastTime)) if (Time.time - lastTime < ClassBalance.ITEM_GIVE_COOLDOWN) return;
			_hostGiveCooldown[actorID] = Time.time;
			StartCoroutine(HostGiveItemsRoutine(clientHatName, info.Sender));
		}

		private IEnumerator HostGiveItemsRoutine(string hatName, Photon.Realtime.Player requestor)
		{
			var itemsToGive = PeakClassModPlugin.BuildStartItemsByHatName(hatName);
			if (itemsToGive.Count == 0) yield break;
			Character targetChar = null;
			int actorNumber = requestor.ActorNumber;
			if (_characterCache.TryGetValue(actorNumber, out Character cachedChar) && cachedChar != null) targetChar = cachedChar;
			else foreach (var ch in Character.AllCharacters) if (ch?.photonView?.Owner == requestor) { targetChar = ch; _characterCache[actorNumber] = ch; break; }
			if (targetChar?.player == null) yield break;
			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null && targetChar.player.HasEmptySlot((ushort)item.itemID)) targetChar.player.AddItem((ushort)item.itemID, null, out _);
				yield return new WaitForSeconds(0.02f);
			}
		}
		[PunRPC] public void ShowMedicHealEffectRPC(int remainingHeals) { }
		[PunRPC] public void RPC_SyncClassActivation(string className, int actorNumber) { }
	}

	public class PeakClassModCallbacks : MonoBehaviourPunCallbacks { public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => ClassModNetwork.ClearPlayerCache(otherPlayer.ActorNumber); }

	// ==================================================
	// 7. PATCHES GLOBAUX
	// ==================================================
	[HarmonyPatch(typeof(Character), "Start")]
	public class Character_Start_AddNetComp_Patch
	{
		static void Postfix(Character __instance)
		{
			if (__instance != null)
			{
				var p = PeakClassModPlugin.Instance;
				p.EnsureComponent<ClassModNetwork>(__instance.gameObject);
				p.EnsureComponent<AstronautStatHandler>(__instance.gameObject);
				p.EnsureComponent<MedicStatHandler>(__instance.gameObject);
				p.EnsureComponent<TechPriestWeightEnforcer>(__instance.gameObject);
				p.EnsureComponent<GobelinSizeHandler>(__instance.gameObject);
			}
		}
	}

	[HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
	public class SetHat_Patch
	{
		static void Postfix(int index)
		{
			var hats = Singleton<Customization>.Instance?.hats;
			string hatName = (hats != null && index >= 0 && index < hats.Length) ? hats[index]?.name : "Unknown";
			HatUtils.SetLastLocalHatName(hatName);

			if (hatName.Contains("Hat_Scout")) ClassManager.ActivateScoutClass();
			else if (hatName.Contains("Hat_Medic") || hatName.Contains("MedicHelmet")) ClassManager.ActivateMedicClass();
			else if (hatName.Contains("Hat_Crab")) ClassManager.ActivateTechPriestClass();
			else if (hatName.Contains("Hat_Mushroom")) ClassManager.ActivateMushroomancerClass();
			else if (hatName.Contains("Hat_Propeller")) ClassManager.ActivateGreatEpeeClass();
			else if (hatName.Contains("Hat_Ninja")) ClassManager.ActivateRogueClass();
			else if (hatName.Contains("Hat_Bandana") || hatName.Contains("Captain")) ClassManager.ActivateGeneralClass();
			else if (hatName.Contains("Hat_Astronaut")) ClassManager.ActivateAstronautClass();
			else if (hatName.Contains(ClassBalance.GOBELIN_HAT)) ClassManager.ActivateGobelinClass();
			else ClassManager.DeactivateAllClasses();

			if (Character.localCharacter != null && GUIManager.instance?.bar != null) GUIManager.instance.bar.ChangeBar();
		}
	}

	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix() { PeakClassModPlugin.Instance?.StartCoroutine(AddEffectsDelayed()); }
		public static IEnumerator AddEffectsDelayed()
		{
			yield return new WaitForSeconds(1.5f); var lc = Character.localCharacter; if (lc == null) { yield return new WaitForSeconds(1f); lc = Character.localCharacter; if (lc == null) yield break; }
			if (ClassCheckUtil.IsMushroom(lc)) { ClassManager.ActivateMushroomancerClass(); ClassManager.StartMushroomancerWatch(lc); }
			else if (ClassCheckUtil.IsGeneral(lc)) { ClassManager.ActivateGeneralClass(); float c = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f; if (c < ClassBalance.GENERAL_CURSE_MIN) lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.GENERAL_CURSE_MIN); }
			else if (ClassCheckUtil.IsGreatEpeeHat(lc)) { ClassManager.ActivateGreatEpeeClass(); ClassManager.StartGreatEpeeRoutines(lc); }
			else if (ClassCheckUtil.IsMedicHat(lc)) { ClassManager.ActivateMedicClass(); float c = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f; if (c < ClassBalance.MEDIC_CURSE) lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MEDIC_CURSE); }
			else if (ClassCheckUtil.IsAstronaut(lc)) ClassManager.ActivateAstronautClass();
			else if (ClassCheckUtil.IsTechPriest(lc)) ClassManager.ActivateTechPriestClass();
			else if (ClassCheckUtil.IsRogueHat(lc)) ClassManager.ActivateRogueClass();
			else if (ClassCheckUtil.IsGobelin(lc)) ClassManager.ActivateGobelinClass();
			else if (ClassCheckUtil.IsScout(lc)) ClassManager.ActivateScoutClass();
		}
	}

	// --- AUTRES PATCHES DE STATS ---
	[HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
	public class General_AddStatus_Patch
	{
		static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			var ch = __instance?.character; if (ch == null) return true;
			if (ch.IsLocal && statusType == CharacterAfflictions.STATUSTYPE.Hunger)
			{
				if (ClassManager.IsAstronautActive()) amount *= ClassBalance.ASTRONAUT_HUNGER_MULT;
				else if (ClassManager.IsScoutActive()) amount *= ClassBalance.SCOUT_HUNGER_MULT;
				else if (ClassManager.IsGeneralActive()) amount *= ClassBalance.GENERAL_HUNGER_MULT;
			}
			if (ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
			{
				if (statusType == CharacterAfflictions.STATUSTYPE.Hunger) amount = 0f;
				else if (statusType == CharacterAfflictions.STATUSTYPE.Weight) { amount = 0f; return false; }
			}
			if (statusType == CharacterAfflictions.STATUSTYPE.Poison && ClassCheckUtil.IsMushroom(ch)) amount = 0f;
			return true;
		}
	}

	[HarmonyPatch(typeof(BarAffliction), "ChangeAffliction")]
	public class BarAffliction_Patch
	{
		static void Postfix(BarAffliction __instance)
		{
			if (Character.localCharacter == null || __instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight) return;
			if (ClassManager.IsScoutActive()) __instance.size *= ClassBalance.SCOUT_WEIGHT_MULT;
			else if (ClassManager.IsGeneralActive()) __instance.size *= ClassBalance.GENERAL_WEIGHT_MULT;
			else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(Character.localCharacter)) { __instance.size = 0f; var img = __instance.GetComponent<Image>(); if (img != null) img.enabled = false; }
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "get_statusSum")]
	public class StatusSum_Patch
	{
		static void Postfix(CharacterAfflictions __instance, ref float __result)
		{
			if (__instance?.character == null || !__instance.character.IsLocal) return;
			float w = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight);
			if (ClassManager.IsScoutActive()) __result = __result - w + (w * ClassBalance.SCOUT_WEIGHT_MULT);
			else if (ClassManager.IsGeneralActive()) __result = __result - w + (w * ClassBalance.GENERAL_WEIGHT_MULT);
			else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character)) __result -= w;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
	public class UpdateStatus_TechPriest_Patch
	{
		static void Postfix(CharacterAfflictions __instance)
		{
			if (__instance?.character != null && ClassCheckUtil.IsTechPriest(__instance.character) && ClassManager.IsTechPriestActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Hunger] = 0f;
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Weight] = 0f;
			}
		}
	}

	[HarmonyPatch(typeof(Character), "UseStamina")]
	public class Character_UseStamina_Patch
	{
		static void Prefix(Character __instance, ref float usage)
		{
			if (__instance == null || !__instance.IsLocal) return;
			if (ClassManager.IsScoutActive()) usage *= ClassBalance.SCOUT_STAMINA_MULT;
			else if (ClassManager.IsRogueActive()) usage *= ClassBalance.ROGUE_STAMINA_MULT;
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
	public class RogueJumpPatch { static void Postfix(CharacterMovement __instance) { var ch = __instance?.GetComponent<Character>(); if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive) ch.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; } }

	[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
	public class RogueJumpRPCPatch { static bool Prefix(CharacterMovement __instance) { var ch = __instance?.GetComponent<Character>(); if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive) { if (ch.data.jumpsRemaining > 0 && ch.data.currentStamina > 0.005f) { ch.data.jumpsRemaining--; ch.data.chargingJump = true; ch.refs?.view?.RPC("JumpRpc", RpcTarget.All, new object[] { false }); return false; } } return true; } }

	// --- CLASSES COMPOSANTES ---
	public class TechPriestWeightEnforcer : MonoBehaviour { private Character _c; private bool _a; private int _w; private float _l; private void Start() { _c = GetComponent<Character>(); _w = (int)CharacterAfflictions.STATUSTYPE.Weight; _a = ClassCheckUtil.IsTechPriest(_c); } private void LateUpdate() { if (!_a || !ClassManager.IsTechPriestActive()) { if (Time.time - _l > 2f) { _l = Time.time; _a = ClassCheckUtil.IsTechPriest(_c); } return; } if (_c?.refs?.afflictions != null) _c.refs.afflictions.currentStatuses[_w] = 0f; } }

	public class AstronautStatHandler : MonoBehaviour { private Character _c; private CharacterMovement _m; private Traverse _t; private float _dj, _ds; private bool _dc, _im; private Traverse _ji, _sm, _v; private bool _fi; private void Start() { _c = GetComponent<Character>(); _m = GetComponent<CharacterMovement>(); if (_m != null) { _t = Traverse.Create(_m); Init(); } } private void Init() { if (_fi || _t == null) return; _ji = _t.Field("jumpImpulse"); _sm = _t.Field("sprintMultiplier"); _v = _t.Field("velocity"); _fi = true; } private void Update() { if (_c == null || !_c.IsLocal || !ClassManager.IsAstronautActive()) return; if (_c.input.jumpWasPressed && _c.data.isGrounded) { StopAllCoroutines(); StartCoroutine(WindWalk()); } } private IEnumerator WindWalk() { yield return new WaitForSeconds(1f); if (ClassManager.IsAstronautActive() && _c != null && !_c.data.isGrounded) try { _c.refs?.afflictions?.AddAffliction(new Affliction_LowGravity(3, ClassBalance.ASTRONAUT_LOW_GRAV_DURATION), true); } catch { } } private void FixedUpdate() { if (_c == null || !_c.IsLocal || !_fi) return; if (ClassManager.IsAstronautActive()) { if (!_dc && !_im) { float? j = _ji?.GetValue<float>(); if (j.HasValue && j > 0) _dj = j.Value; float? s = _sm?.GetValue<float>(); if (s.HasValue) _ds = s.Value; _dc = true; } _ji?.SetValue(ClassBalance.ASTRONAUT_JUMP_IMPULSE); _sm?.SetValue(ClassBalance.ASTRONAUT_SPRINT_MULT); if (_c.data.isGrounded) { var l = _c.refs?.afflictions?.afflictionList; if (l != null) for (int i = l.Count - 1; i >= 0; i--) if (l[i] is Affliction_LowGravity) _c.refs.afflictions.RemoveAffliction(l[i]); } if (!_c.data.isSprinting && _v != null) { Vector3 cv = _v.GetValue<Vector3>(); Vector3 hv = new Vector3(cv.x, 0, cv.z); if (hv.magnitude > ClassBalance.ASTRONAUT_MAX_WALK_SPEED) { Vector3 c = hv.normalized * ClassBalance.ASTRONAUT_MAX_WALK_SPEED; _v.SetValue(new Vector3(c.x, cv.y, c.z)); } } _im = true; } else if (_im) { _ji?.SetValue(_dj); _sm?.SetValue(_ds); _im = false; _dc = false; } } }

	public class MedicStatHandler : MonoBehaviour { private Character _c; private ClassModNetwork _n; private int _h = ClassBalance.MEDIC_MAX_HEALS; private float _l; private void Start() { _c = GetComponent<Character>(); _n = GetComponent<ClassModNetwork>(); } private void Update() { if (_c == null || !_c.IsLocal) return; if (ClassManager.IsMedicActive()) { if (Input.GetKeyDown(KeyCode.H) && Time.time - _l > ClassBalance.MEDIC_HEAL_COOLDOWN) Heal(); } else if (_h < ClassBalance.MEDIC_MAX_HEALS) _h = ClassBalance.MEDIC_MAX_HEALS; } private void Heal() { if (_h <= 0) return; var a = _c.refs?.afflictions; if (a != null && a.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f) { _h--; _l = Time.time; a.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, ClassBalance.MEDIC_HEAL_AMOUNT, false); _n?.photonView?.RPC("ShowMedicHealEffectRPC", RpcTarget.All, _h); } } }

	internal static class TechPriestBlocker { private static HashSet<string> B = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "heal", "cure", "medicine", "bandage", "firstaid", "medkit", "marshmallow" }; public static bool Block(Item i) { if (i == null) return false; if (i.GetComponent<Action_RestoreHunger>() != null || i.GetComponent<Action_GiveExtraStamina>() != null) return true; string n = i.name.Replace("(Clone)", ""); foreach (var b in B) if (n.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; } }
	internal static class TechPriestChecker { public static bool Block(Item i) => i?.holderCharacter != null && ClassCheckUtil.IsTechPriest(i.holderCharacter) && TechPriestBlocker.Block(i); }
	[HarmonyPatch(typeof(Item), "StartUsePrimary")] public class TP1 { static bool Prefix(Item __instance) => !TechPriestChecker.Block(__instance); }
	[HarmonyPatch(typeof(Item), "StartUseSecondary")] public class TP2 { static bool Prefix(Item __instance) => !TechPriestChecker.Block(__instance); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_FasterBoi), "OnApplied")] public class TP3 { static bool Prefix(Peak.Afflictions.Affliction_FasterBoi __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
	[HarmonyPatch(typeof(Character), "AddExtraStamina")] public class TP4 { static bool Prefix(Character __instance) => __instance == null || !ClassCheckUtil.IsTechPriest(__instance); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "OnApplied")] public class TP5 { static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "UpdateEffect")] public class TP6 { static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_PoisonOverTime), "OnApplied")] public class PoisonPatch { static bool Prefix(Peak.Afflictions.Affliction_PoisonOverTime __instance) => __instance?.character == null || !ClassCheckUtil.IsMushroom(__instance.character); }
}