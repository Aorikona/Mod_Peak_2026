// ============================================================
// PEAK Class Mod - Version: 4.0.0 (Optimized & Bug-Fixed)
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
	// CONSTANTES DE BALANCE - Plus facile à modifier
	// ==================================================
	public static class ClassBalance
	{
		// Astronaut
		public const float ASTRONAUT_JUMP_IMPULSE = 1200f;
		public const float ASTRONAUT_SPRINT_MULT = 1.7f;
		public const float ASTRONAUT_HUNGER_MULT = 1.3f;
		public const float ASTRONAUT_MAX_WALK_SPEED = 1.6f;
		public const float ASTRONAUT_LOW_GRAV_DURATION = 3f;

		// Gobelin
		public const float GOBELIN_SIZE = 0.5f;
		public const float GOBELIN_CURSE_PER_BLOWGUN = 0.05f;

		// General
		public const float GENERAL_CURSE_MIN = 0.35f;
		public const float GENERAL_HUNGER_MULT = 2.0f;
		public const float GENERAL_WEIGHT_MULT = 2.0f;

		// Medic
		public const float MEDIC_CURSE = 0.15f;
		public const int MEDIC_MAX_HEALS = 10;
		public const float MEDIC_HEAL_AMOUNT = 0.1f;
		public const float MEDIC_HEAL_COOLDOWN = 1f;

		// Scout
		public const float SCOUT_STAMINA_MULT = 0.80f;
		public const float SCOUT_HUNGER_MULT = 1.7f;
		public const float SCOUT_WEIGHT_MULT = 0.5f;

		// Rogue
		public const float ROGUE_STAMINA_MULT = 2.0f;
		public const int ROGUE_EXTRA_JUMPS = 2;

		// GreatEpee
		public const float GREATEPEE_SLEEP_MIN = 45f;
		public const float GREATEPEE_SLEEP_MAX = 150f;
		public const float GREATEPEE_EFFECT_DURATION = 99999f;

		// Mushroomancer
		public const float MUSHROOM_STAMINA_COOLDOWN = 10f;
		public const float MUSHROOM_CURSE_AMOUNT = 0.02f;

		// Network
		public const float ITEM_REQUEST_DELAY = 0.3f;
		public const float ITEM_GIVE_COOLDOWN = 2f;

		// Cache
		public const float ITEM_CACHE_DURATION = 300f;
		public const float SIZE_CHECK_INTERVAL = 0.5f;
	}

	// ==================================================
	// PLUGIN PRINCIPAL
	// ==================================================
	[BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "4.0.0")]
	public class PeakClassModPlugin : BaseUnityPlugin
	{
		public static PeakClassModPlugin Instance;
		internal static ManualLogSource Log;
		private static Harmony harmony;

		// Flags des classes
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

			_callbacksGO = new GameObject("PeakClassModCallbacks");
			GameObject.DontDestroyOnLoad(_callbacksGO);
			_callbacksGO.AddComponent<PeakClassModCallbacks>();

			Log?.LogInfo("[ClassMod] Ready. Version 4.0.0 loaded (Optimized & Bug-Fixed).");
		}

		private void OnDestroy()
		{
			try { harmony?.UnpatchSelf(); } catch { }
			if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
		}

		public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
		{
			if (go != null && go.GetComponent<T>() == null)
				go.AddComponent<T>();
		}

		public static List<string> BuildStartItemsByHatName(string hatName)
		{
			var items = new List<string>();
			if (string.IsNullOrEmpty(hatName)) return items;

			switch (hatName)
			{
				case "Hat_Mushroom":
					for (int i = 0; i < 3; i++)
						items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
					break;
				case "Hat_Medic":
					items.Add("FirstAidKit");
					items.Add("Cure-All");
					break;
				case "Hat_Crab":
					items.Add("ChainShooter");
					items.Add("ChainShooter");
					break;
				case "Hat_NinjaHeadband":
					items.Add("RopeSpool");
					items.Add("RopeSpool");
					break;
				case "Hat_Astronaut":
					items.Add("BalloonBunch");
					break;
				case "MidSummerHat":
				case "Hat_MidSummer":
					items.Add("Blowgun");
					break;
			}
			return items;
		}

		private static readonly string[] StartBerries = new[] {
			"Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple",
			"Shroomberry_Blue", "Shroomberry_Green"
		};
	}

	// ==================================================
	// NETWORK HANDLER (Optimisé)
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
			if (components.Length > 1)
			{
				UnityEngine.Object.Destroy(this);
				return;
			}
			_character = GetComponent<Character>();
		}

		private void Start()
		{
			if (photonView != null && photonView.IsMine && !_hasRequestedItems)
			{
				_hasRequestedItems = true;
				StartCoroutine(RequestItemsRoutine());
			}
		}

		private void OnDestroy()
		{
			if (_character?.photonView?.Owner != null)
			{
				_characterCache.Remove(_character.photonView.Owner.ActorNumber);
			}
		}

		// Méthode publique pour nettoyer le cache quand un joueur part
		public static void ClearPlayerCache(int actorNumber)
		{
			_characterCache.Remove(actorNumber);
			_hostGiveCooldown.Remove(actorNumber);
		}

		private IEnumerator RequestItemsRoutine()
		{
			yield return new WaitForSeconds(ClassBalance.ITEM_REQUEST_DELAY);
			while (!PhotonNetwork.InRoom) yield return null;

			if (_character == null) _character = GetComponent<Character>();

			float timeout = 5f;
			bool inventoryReady = false;

			while (timeout > 0)
			{
				if (_character?.player?.itemSlots != null &&
					_character.player.itemSlots.Length > 0 &&
					!_character.player.itemSlots[0].IsEmpty())
				{
					inventoryReady = true;
					break;
				}
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			string myHatName = "None";
			if (HatUtils.TryGetHatName(_character, out string detectedHat))
				myHatName = detectedHat;

			if (inventoryReady || timeout <= 0)
			{
				PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Requesting items for Hat: {myHatName}");
				photonView.RPC("RPC_HostGiveItems", RpcTarget.MasterClient, myHatName);
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
				if (Time.time - lastTime < ClassBalance.ITEM_GIVE_COOLDOWN) return;
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
				targetChar = cachedChar;
			else
			{
				foreach (var ch in Character.AllCharacters)
				{
					if (ch?.photonView?.Owner == requestor)
					{
						targetChar = ch;
						_characterCache[actorNumber] = ch;
						break;
					}
				}
			}

			if (targetChar?.player == null) yield break;

			PeakClassModPlugin.Log?.LogInfo(
				$"[ClassMod] Host giving {itemsToGive.Count} items to ActorNumber {requestor.ActorNumber}...");

			int itemsGiven = 0;
			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null)
				{
					ushort itemId = (ushort)item.itemID;
					if (targetChar.player.HasEmptySlot(itemId))
					{
						targetChar.player.AddItem(itemId, null, out _);
						itemsGiven++;
						PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Host gave {itemName}");
					}
				}
				yield return new WaitForSeconds(0.02f);
			}

			if (itemsGiven > 0)
				yield return new WaitForSeconds(0.1f);
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
	// CALLBACKS PHOTON (Ajout nettoyage cache)
	// ==================================================
	public class PeakClassModCallbacks : MonoBehaviourPunCallbacks
	{
		public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
		{
			ClassModNetwork.ClearPlayerCache(otherPlayer.ActorNumber);
			PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Cleaned cache for player {otherPlayer.ActorNumber}");
		}
	}

	// ==================================================
	// UTILITIES (Optimisées)
	// ==================================================
	internal static class HatUtils
	{
		private static string _lastLocalHatName;

		internal static void SetLastLocalHatName(string hatName)
		{
			if (!string.IsNullOrEmpty(hatName))
				_lastLocalHatName = hatName;
		}

		public static bool TryGetHatName(Character ch, out string hatName)
		{
			hatName = null;
			if (ch == null) return false;

			var ppds = GameHandler.GetService<PersistentPlayerDataService>();
			PersistentPlayerData data = null;

			if (ch.photonView?.Owner != null)
				data = ppds?.GetPlayerData(ch.photonView.Owner);
			if (data == null && ch.IsLocal && PhotonNetwork.LocalPlayer != null)
				data = ppds?.GetPlayerData(PhotonNetwork.LocalPlayer);

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

			if (ch.IsLocal && !string.IsNullOrEmpty(_lastLocalHatName))
			{
				hatName = _lastLocalHatName;
				return true;
			}
			return false;
		}

		public static bool HasHat(Character ch, string expected)
			=> TryGetHatName(ch, out var name) &&
			   string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

		public static bool HasAnyHat(Character ch, params string[] names)
		{
			if (!TryGetHatName(ch, out var name)) return false;
			return names.Contains(name, StringComparer.OrdinalIgnoreCase);
		}
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
		public static bool IsGobelin(Character ch) => HatUtils.HasAnyHat(ch, "MidSummerHat", "Hat_MidSummer");
		public static bool IsScout(Character ch) => HatUtils.HasHat(ch, "Hat_Scout");
	}

	// ==================================================
	// GOBELIN DIET (Amélioré)
	// ==================================================
	internal static class GobelinDiet
	{
		private static readonly HashSet<string> AllowedCategories = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase)
		{
			"Food", "Berry", "NaturalFood", "Fruit", "Vegetable", "Mushroom"
		};

		private static readonly HashSet<string> BlockedCategories = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase)
		{
			"PackagedFood", "ProcessedFood", "CannedFood"
		};

		public static bool IsAllowedFood(Item item)
		{
			if (item?.data == null) return true;

			string category = item.data.category.ToString();

			// Priorité à la liste noire
			foreach (var blocked in BlockedCategories)
			{
				if (category.IndexOf(blocked, StringComparison.OrdinalIgnoreCase) >= 0)
					return false;
			}

			// Si dans la liste blanche, OK
			foreach (var allowed in AllowedCategories)
			{
				if (category.IndexOf(allowed, StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			// Autre chose (potions, etc.) = autorisé
			return true;
		}
	}

	// ==================================================
	// ITEM CACHE (Optimisé avec protection thread-safe)
	// ==================================================
	internal static class ItemCache
	{
		private static Dictionary<string, Item> _byName;
		private static bool _isRefreshing = false;
		private static readonly object _lock = new object();

		public static void Refresh()
		{
			lock (_lock)
			{
				_byName = null;
			}
		}

		private static void EnsureCache()
		{
			if (_byName != null)
				return;
			if (_isRefreshing) return;

			lock (_lock)
			{
				if (_byName != null)
					return;

				_isRefreshing = true;
				try
				{
					var newCache = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
					foreach (var it in Resources.FindObjectsOfTypeAll<Item>())
					{
						if (it != null && !string.IsNullOrEmpty(it.name) && !newCache.ContainsKey(it.name))
							newCache[it.name] = it;
					}
					_byName = newCache;
				}
				finally
				{
					_isRefreshing = false;
				}
			}
		}

		public static Item Get(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			EnsureCache();

			if (_byName == null) return null;

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
			if (__instance == null) return;

			var plugin = PeakClassModPlugin.Instance;
			if (plugin == null) return;

			plugin.EnsureComponent<ClassModNetwork>(__instance.gameObject);
			plugin.EnsureComponent<AstronautStatHandler>(__instance.gameObject);
			plugin.EnsureComponent<MedicStatHandler>(__instance.gameObject);
			plugin.EnsureComponent<TechPriestWeightEnforcer>(__instance.gameObject);
			plugin.EnsureComponent<GobelinSizeHandler>(__instance.gameObject);
		}
	}

	// ==================================================
	// CLASS MANAGER (Avec gestion coroutines)
	// ==================================================
	public static class ClassManager
	{
		private static readonly Dictionary<int, float> lastStaminaZeroTime = new Dictionary<int, float>();

		// Références des coroutines actives
		private static Coroutine _mushroomCoroutine;
		private static Coroutine _greatEpeeEffectsCoroutine;
		private static Coroutine _greatEpeeStaminaCoroutine;

		public static void ResetAllFlags()
		{
			// Arrêter toutes les coroutines actives
			StopAllClassCoroutines();

			PeakClassModPlugin.scoutActive = false;
			PeakClassModPlugin.medicActive = false;
			PeakClassModPlugin.techPriestActive = false;
			PeakClassModPlugin.mushroomancerActive = false;
			PeakClassModPlugin.greatEpeeActive = false;
			PeakClassModPlugin.rogueActive = false;
			PeakClassModPlugin.generalActive = false;
			PeakClassModPlugin.astronautActive = false;
			PeakClassModPlugin.gobelinActive = false;
			PeakClassModPlugin.currentClass = "None";
		}

		private static void StopAllClassCoroutines()
		{
			var lc = Character.localCharacter;
			if (lc == null) return;

			if (_mushroomCoroutine != null)
			{
				lc.StopCoroutine(_mushroomCoroutine);
				_mushroomCoroutine = null;
			}
			if (_greatEpeeEffectsCoroutine != null)
			{
				lc.StopCoroutine(_greatEpeeEffectsCoroutine);
				_greatEpeeEffectsCoroutine = null;
			}
			if (_greatEpeeStaminaCoroutine != null)
			{
				lc.StopCoroutine(_greatEpeeStaminaCoroutine);
				_greatEpeeStaminaCoroutine = null;
			}
		}

		public static void ActivateScoutClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.scoutActive = true;
			PeakClassModPlugin.currentClass = "Scout";
		}

		public static void ActivateMedicClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.medicActive = true;
			PeakClassModPlugin.currentClass = "Medic";
		}

		public static void ActivateTechPriestClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.techPriestActive = true;
			PeakClassModPlugin.currentClass = "Tech-Priest";
		}

		public static void ActivateMushroomancerClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.mushroomancerActive = true;
			PeakClassModPlugin.currentClass = "Mushroomancer";
		}

		public static void ActivateGreatEpeeClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.greatEpeeActive = true;
			PeakClassModPlugin.currentClass = "GreatEpee";
		}

		public static void ActivateRogueClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.rogueActive = true;
			PeakClassModPlugin.currentClass = "Rogue";
			if (Character.localCharacter != null)
				Character.localCharacter.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS;
		}

		public static void ActivateGeneralClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.generalActive = true;
			PeakClassModPlugin.currentClass = "General";
		}

		public static void ActivateAstronautClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.astronautActive = true;
			PeakClassModPlugin.currentClass = "Astronaut";
		}

		public static void ActivateGobelinClass()
		{
			ResetAllFlags();
			PeakClassModPlugin.gobelinActive = true;
			PeakClassModPlugin.currentClass = "Gobelin";
		}

		public static void DeactivateAllClasses()
		{
			ResetAllFlags();
			if (Character.localCharacter != null)
				Character.localCharacter.data.jumpsRemaining = 1;
		}

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

		private static readonly string[] ZeroStaminaFungi = new[] {
			"ShelfShroom", "BounceShroom", "CloudFungus"
		};

		public static void StartMushroomancerWatch(Character character)
		{
			if (character != null)
				_mushroomCoroutine = character.StartCoroutine(MushroomancerLocalStaminaWatch(character));
		}

		public static void StartGreatEpeeRoutines(Character character)
		{
			if (character != null)
			{
				_greatEpeeEffectsCoroutine = character.StartCoroutine(GreatEpeePermanentEffects(character));
				_greatEpeeStaminaCoroutine = character.StartCoroutine(GreatEpeeStaminaRoutine(character));
			}
		}

		public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
		{
			while (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
			{
				if (character.data.currentStamina <= 0.02f)
					TryActivateStaminaZeroMechanic(character);

				yield return new WaitForSeconds(0.1f);
			}
		}

		public static void TryActivateStaminaZeroMechanic(Character character)
		{
			if (character?.photonView?.Owner == null) return;
			int actor = character.photonView.Owner.ActorNumber;

			if (lastStaminaZeroTime.TryGetValue(actor, out var last) &&
				Time.time - last < ClassBalance.MUSHROOM_STAMINA_COOLDOWN)
				return;

			lastStaminaZeroTime[actor] = Time.time;

			character.refs?.afflictions?.AddStatus(
				CharacterAfflictions.STATUSTYPE.Curse,
				ClassBalance.MUSHROOM_CURSE_AMOUNT,
				false);

			string randomName = ZeroStaminaFungi[UnityEngine.Random.Range(0, ZeroStaminaFungi.Length)];
			Item fungusItem = ItemCache.Get(randomName);

			if (fungusItem == null)
			{
				PeakClassModPlugin.Log?.LogWarning($"[Mushroomancer] Erreur: Item '{randomName}' introuvable.");
				return;
			}

			ushort id = (ushort)fungusItem.itemID;
			if (character.player != null && character.player.HasEmptySlot(id))
			{
				character.player.AddItem(id, null, out _);
				PeakClassModPlugin.Log?.LogInfo($"[Mushroomancer] Stamina vide ! Curse + Spawn de {randomName}");
			}
			else
			{
				PeakClassModPlugin.Log?.LogInfo($"[Mushroomancer] Stamina vide ! Curse appliquée (Inventaire plein).");
			}
		}

		public static IEnumerator GreatEpeePermanentEffects(Character character)
		{
			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				bool hasFaster = false;
				var afflictionList = character.refs?.afflictions?.afflictionList;

				if (afflictionList != null)
				{
					for (int i = 0; i < afflictionList.Count; i++)
					{
						if (afflictionList[i]?.GetType().Name.Contains("FasterBoi") == true)
						{
							hasFaster = true;
							break;
						}
					}

					if (!hasFaster)
					{
						var fb = new Peak.Afflictions.Affliction_FasterBoi
						{
							totalTime = ClassBalance.GREATEPEE_EFFECT_DURATION
						};
						character.refs.afflictions.AddAffliction(fb, false);
					}
				}

				yield return new WaitForSeconds(2f);
			}
		}

		public static IEnumerator GreatEpeeStaminaRoutine(Character character)
		{
			yield return new WaitForSeconds(5f);

			while (PeakClassModPlugin.greatEpeeActive && character != null && character.IsLocal)
			{
				// Attendre que le joueur se réveille
				while (character?.refs?.afflictions != null &&
					   character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0.05f)
				{
					yield return new WaitForSeconds(1f);
					if (!PeakClassModPlugin.greatEpeeActive) yield break;
				}

				if (character == null) yield break;

				float waitTime = UnityEngine.Random.Range(
					ClassBalance.GREATEPEE_SLEEP_MIN,
					ClassBalance.GREATEPEE_SLEEP_MAX);

				PeakClassModPlugin.Log?.LogInfo(
					$"[GreatEpee] Joueur réveillé. Prochain sommeil dans {waitTime:F0} secondes.");

				yield return new WaitForSeconds(waitTime);

				if (character?.refs?.afflictions == null || !PeakClassModPlugin.greatEpeeActive)
					yield break;

				if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) < 0.1f)
				{
					character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);
					PeakClassModPlugin.Log?.LogInfo("[GreatEpee] Temps écoulé : Le joueur s'endort !");
				}
			}
		}
	}

	// ==================================================
	// GOBELIN PATCHES (Fusionnés et corrigés)
	// ==================================================

	// PATCH UNIFIÉ pour Item.StartUsePrimary (Blowgun + Food)
	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	public class GobelinItemPrimaryPatch
	{
		// État pour le blowgun
		private static bool _originalDestroyOnUse;
		private static bool _isBlowgunUse;

		static bool Prefix(Item __instance)
		{
			_isBlowgunUse = false;

			// Vérification null complète
			if (__instance?.holderCharacter == null) return true;
			if (!ClassCheckUtil.IsGobelin(__instance.holderCharacter)) return true;

			// === BLOWGUN LOGIC ===
			if (__instance.name?.IndexOf("Blowgun", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				if (__instance.data != null)
				{
					_originalDestroyOnUse = __instance.data.destroyOnUse;
					__instance.data.destroyOnUse = false;
					_isBlowgunUse = true;

					// Applique le malus de Curse
					__instance.holderCharacter.refs?.afflictions?.AddStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.GOBELIN_CURSE_PER_BLOWGUN,
						false);
				}
				return true;
			}

			// === FOOD RESTRICTION LOGIC ===
			if (__instance.GetComponent<Action_RestoreHunger>() != null)
			{
				if (!GobelinDiet.IsAllowedFood(__instance))
				{
					PeakClassModPlugin.Log?.LogInfo(
						$"[Gobelin] Beurk ! {__instance.name} refusé (pas naturel).");
					return false;
				}
			}

			return true;
		}

		static void Postfix(Item __instance)
		{
			// Restaurer destroyOnUse pour le blowgun
			if (_isBlowgunUse && __instance?.data != null)
			{
				__instance.data.destroyOnUse = _originalDestroyOnUse;
				_isBlowgunUse = false;
			}
		}
	}

	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	public class GobelinItemSecondaryPatch
	{
		static bool Prefix(Item __instance)
		{
			if (__instance?.holderCharacter == null) return true;
			if (!ClassCheckUtil.IsGobelin(__instance.holderCharacter)) return true;

			if (__instance.GetComponent<Action_RestoreHunger>() != null)
			{
				if (!GobelinDiet.IsAllowedFood(__instance))
				{
					PeakClassModPlugin.Log?.LogInfo(
						$"[Gobelin] Beurk ! {__instance.name} refusé (pas naturel).");
					return false;
				}
			}
			return true;
		}
	}

	// ==================================================
	// GOBELIN SIZE HANDLER (Optimisé)
	// ==================================================
	public class GobelinSizeHandler : MonoBehaviour
	{
		private Character _character;
		private bool _sizeApplied = false;
		private float _lastCheckTime = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
		}

		private void Update()
		{
			// Limiter la fréquence des vérifications
			if (Time.time - _lastCheckTime < ClassBalance.SIZE_CHECK_INTERVAL) return;
			_lastCheckTime = Time.time;

			if (_character == null || !_character.IsLocal) return;

			bool shouldBeGobelin = ClassManager.IsGobelinActive();

			if (shouldBeGobelin && !_sizeApplied)
			{
				_character.transform.localScale = Vector3.one * ClassBalance.GOBELIN_SIZE;
				_sizeApplied = true;
			}
			else if (!shouldBeGobelin && _sizeApplied)
			{
				_character.transform.localScale = Vector3.one;
				_sizeApplied = false;
			}
		}
	}

	// ==================================================
	// TECH PRIEST HANDLERS (Optimisés)
	// ==================================================
	public class TechPriestWeightEnforcer : MonoBehaviour
	{
		private Character _character;
		private CharacterAfflictions _afflictions;
		private bool _isActive;
		private int _weightIndex;
		private float _lastCheck = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
			_weightIndex = (int)CharacterAfflictions.STATUSTYPE.Weight;
			RefreshState();
		}

		private void RefreshState()
		{
			_isActive = _character != null && ClassCheckUtil.IsTechPriest(_character);
			if (_isActive && _character?.refs != null)
				_afflictions = _character.refs.afflictions;
		}

		private void LateUpdate()
		{
			if (!_isActive || !ClassManager.IsTechPriestActive())
			{
				// Vérification périodique moins fréquente
				if (Time.time - _lastCheck > 2f)
				{
					_lastCheck = Time.time;
					RefreshState();
				}
				return;
			}

			if (_afflictions != null && _afflictions.currentStatuses[_weightIndex] > 0f)
			{
				_afflictions.currentStatuses[_weightIndex] = 0f;
			}
		}
	}

	internal static class TechPriestBlocker
	{
		private static readonly HashSet<string> BlockedTypeNames = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase)
		{
			"heal", "cure", "medicine", "restorehealth"
		};

		private static readonly HashSet<string> BlockedItemNames = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase)
		{
			"bandage", "firstaid", "medkit", "cure", "marshmallow"
		};

		private static Dictionary<int, bool> _blockCache = new Dictionary<int, bool>();

		public static bool ShouldBlockItem(Item item)
		{
			if (item == null) return false;

			int instanceId = item.GetInstanceID();
			if (_blockCache.TryGetValue(instanceId, out bool cached))
				return cached;

			bool shouldBlock = false;

			// Vérification composants
			if (item.GetComponent<Action_RestoreHunger>() != null ||
				item.GetComponent<Action_GiveExtraStamina>() != null)
			{
				shouldBlock = true;
			}

			// Vérification nom
			if (!shouldBlock)
			{
				string cleanName = item.name.Replace("(Clone)", "").Trim();
				foreach (var blocked in BlockedItemNames)
				{
					if (cleanName.IndexOf(blocked, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						shouldBlock = true;
						break;
					}
				}
			}

			// Vérification types de composants
			if (!shouldBlock)
			{
				foreach (var comp in item.GetComponents<MonoBehaviour>())
				{
					if (comp == null) continue;
					string typeName = comp.GetType().Name;

					foreach (var blocked in BlockedTypeNames)
					{
						if (typeName.IndexOf(blocked, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							shouldBlock = true;
							break;
						}
					}
					if (shouldBlock) break;
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

	// Méthode partagée pour les patches TechPriest
	internal static class TechPriestItemChecker
	{
		public static bool ShouldBlockUse(Item item)
		{
			if (item?.holderCharacter == null) return false;
			if (!ClassCheckUtil.IsTechPriest(item.holderCharacter)) return false;

			if (TechPriestBlocker.ShouldBlockItem(item)) return true;

			var affAction = item.GetComponent<Action_ApplyAffliction>();
			if (affAction?.affliction != null &&
				affAction.affliction.GetType().Name.Contains("FasterBoi"))
				return true;

			return false;
		}
	}

	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	[HarmonyPriority(Priority.High)]
	public class TechPriestConsumablePatch1
	{
		static bool Prefix(Item __instance) => !TechPriestItemChecker.ShouldBlockUse(__instance);
	}

	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	[HarmonyPriority(Priority.High)]
	public class TechPriestConsumablePatch2
	{
		static bool Prefix(Item __instance) => !TechPriestItemChecker.ShouldBlockUse(__instance);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_FasterBoi), "OnApplied")]
	public class TechPriestFasterBoiBlock
	{
		static bool Prefix(Peak.Afflictions.Affliction_FasterBoi __instance)
			=> __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character);
	}

	[HarmonyPatch(typeof(Character), "AddExtraStamina")]
	public class TechPriestStaminaPatch1
	{
		static bool Prefix(Character __instance)
			=> __instance == null || !ClassCheckUtil.IsTechPriest(__instance);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "OnApplied")]
	public class TechPriestStaminaPatch2
	{
		static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance)
			=> __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character);
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "UpdateEffect")]
	public class TechPriestStaminaPatch2_Update
	{
		static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance)
			=> __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character);
	}

	// ==================================================
	// ASTRONAUT HANDLER (Optimisé - Traverse dans Start)
	// ==================================================
	public class AstronautStatHandler : MonoBehaviour
	{
		private Character _character;
		private CharacterMovement _movement;
		private Traverse _travMove;
		private float _defJump = 400f;
		private float _defSprintMult = 2.0f;
		private bool _defaultsCaptured = false;
		private bool _isModified = false;

		// Champs Traverse initialisés dans Start
		private Traverse _jumpImpulseField;
		private Traverse _sprintMultiplierField;
		private Traverse _velocityField;
		private bool _fieldsInitialized = false;

		private void Start()
		{
			_character = GetComponent<Character>();
			_movement = GetComponent<CharacterMovement>();

			// Initialiser Traverse une seule fois
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
			_fieldsInitialized = true;
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal || !ClassManager.IsAstronautActive())
				return;

			if (_character.input.jumpWasPressed && _character.data.isGrounded)
			{
				StopAllCoroutines();
				StartCoroutine(ApplyWindWalkDelayed());
			}
		}

		private IEnumerator ApplyWindWalkDelayed()
		{
			yield return new WaitForSeconds(1f);

			if (ClassManager.IsAstronautActive() && _character != null && !_character.data.isGrounded)
				TriggerWindWalk();
		}

		private void TriggerWindWalk()
		{
			try
			{
				_character.refs?.afflictions?.AddAffliction(
					new Affliction_LowGravity(3, ClassBalance.ASTRONAUT_LOW_GRAV_DURATION),
					true);
			}
			catch (Exception e)
			{
				PeakClassModPlugin.Log?.LogError($"[Astronaut] WindWalk Error: {e.Message}");
			}
		}

		private void FixedUpdate()
		{
			if (_character == null || !_character.IsLocal || !_fieldsInitialized) return;

			if (ClassManager.IsAstronautActive())
			{
				if (!_defaultsCaptured && !_isModified)
				{
					CaptureDefaults();
				}

				ApplyAstronautStats();
				_isModified = true;
			}
			else if (_isModified)
			{
				RestoreDefaults();
			}
		}

		private void CaptureDefaults()
		{
			float? j = _jumpImpulseField?.GetValue<float>();
			if (j.HasValue && j.Value > 0) _defJump = j.Value;

			float? sm = _sprintMultiplierField?.GetValue<float>();
			if (sm.HasValue) _defSprintMult = sm.Value;

			_defaultsCaptured = true;
		}

		private void ApplyAstronautStats()
		{
			_jumpImpulseField?.SetValue(ClassBalance.ASTRONAUT_JUMP_IMPULSE);
			_sprintMultiplierField?.SetValue(ClassBalance.ASTRONAUT_SPRINT_MULT);

			// Retirer low gravity au sol
			if (_character.data.isGrounded)
			{
				var afflictions = _character.refs?.afflictions?.afflictionList;
				if (afflictions != null)
				{
					for (int i = afflictions.Count - 1; i >= 0; i--)
					{
						if (afflictions[i] is Affliction_LowGravity)
							_character.refs.afflictions.RemoveAffliction(afflictions[i]);
					}
				}
			}

			// Limiter vitesse de marche
			if (!_character.data.isSprinting && _velocityField != null && _velocityField.FieldExists())
			{
				Vector3 currentVel = _velocityField.GetValue<Vector3>();
				Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);

				if (horizontalVel.magnitude > ClassBalance.ASTRONAUT_MAX_WALK_SPEED)
				{
					Vector3 clamped = horizontalVel.normalized * ClassBalance.ASTRONAUT_MAX_WALK_SPEED;
					_velocityField.SetValue(new Vector3(clamped.x, currentVel.y, clamped.z));
				}
			}
		}

		private void RestoreDefaults()
		{
			_jumpImpulseField?.SetValue(_defJump);
			_sprintMultiplierField?.SetValue(_defSprintMult);
			_isModified = false;
			_defaultsCaptured = false;
		}
	}

	// ==================================================
	// MEDIC HANDLER
	// ==================================================
	public class MedicStatHandler : MonoBehaviour
	{
		private Character _character;
		private ClassModNetwork _network;
		private CharacterAfflictions _afflictions;
		private int _healsRemaining = ClassBalance.MEDIC_MAX_HEALS;
		private float _lastHealTime = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
			_network = GetComponent<ClassModNetwork>();
			_healsRemaining = ClassBalance.MEDIC_MAX_HEALS;

			if (_character?.refs != null)
				_afflictions = _character.refs.afflictions;
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;

			if (ClassManager.IsMedicActive())
			{
				if (Input.GetKeyDown(KeyCode.H) && Time.time - _lastHealTime > ClassBalance.MEDIC_HEAL_COOLDOWN)
					TrySelfHeal();
			}
			else if (_healsRemaining < ClassBalance.MEDIC_MAX_HEALS)
			{
				_healsRemaining = ClassBalance.MEDIC_MAX_HEALS;
			}
		}

		private void TrySelfHeal()
		{
			if (_healsRemaining <= 0) return;

			if (_afflictions == null && _character?.refs != null)
				_afflictions = _character.refs.afflictions;

			if (_afflictions != null &&
				_afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f)
			{
				_healsRemaining--;
				_lastHealTime = Time.time;
				_afflictions.SubtractStatus(
					CharacterAfflictions.STATUSTYPE.Injury,
					ClassBalance.MEDIC_HEAL_AMOUNT,
					false);

				if (_network?.photonView != null)
					_network.photonView.RPC("ShowMedicHealEffectRPC", RpcTarget.All, _healsRemaining);
			}
		}
	}

	// ==================================================
	// HAT SELECTION PATCH (Corrigé - pas de magic index)
	// ==================================================
	[HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
	public class SetHat_Patch
	{
		static void Postfix(int index)
		{
			var hats = Singleton<Customization>.Instance?.hats;
			string hatName = (hats != null && index >= 0 && index < hats.Length)
				? hats[index]?.name
				: "Unknown";

			HatUtils.SetLastLocalHatName(hatName);

			// Utilisation du nom au lieu de l'index
			switch (hatName)
			{
				case "Hat_Scout":
					ClassManager.ActivateScoutClass();
					break;
				case "Hat_Medic":
				case "MedicHelmet":
					ClassManager.ActivateMedicClass();
					break;
				case "Hat_Crab":
					ClassManager.ActivateTechPriestClass();
					break;
				case "Hat_Mushroom":
					ClassManager.ActivateMushroomancerClass();
					break;
				case "Hat_Propeller":
					ClassManager.ActivateGreatEpeeClass();
					break;
				case "Hat_NinjaHeadband":
					ClassManager.ActivateRogueClass();
					break;
				case "Hat_Bandana":
				case "Hat_Captain":
					ClassManager.ActivateGeneralClass();
					break;
				case "Hat_Astronaut":
					ClassManager.ActivateAstronautClass();
					break;
				case "MidSummerHat":
				case "Hat_MidSummer":
					ClassManager.ActivateGobelinClass();
					break;
				default:
					ClassManager.DeactivateAllClasses();
					break;
			}

			if (Character.localCharacter != null && GUIManager.instance?.bar != null)
				GUIManager.instance.bar.ChangeBar();
		}
	}

	// ==================================================
	// RUN START PATCH
	// ==================================================
	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix()
		{
			PeakClassModPlugin.Instance?.StartCoroutine(AddEffectsDelayed());
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
				ClassManager.StartMushroomancerWatch(lc);
			}
			else if (ClassCheckUtil.IsGeneral(lc))
			{
				ClassManager.ActivateGeneralClass();
				float currentCurse = lc.refs?.afflictions?.GetCurrentStatus(
					CharacterAfflictions.STATUSTYPE.Curse) ?? 0f;
				if (currentCurse < ClassBalance.GENERAL_CURSE_MIN)
					lc.refs?.afflictions?.SetStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.GENERAL_CURSE_MIN);
			}
			else if (ClassCheckUtil.IsGreatEpeeHat(lc))
			{
				ClassManager.ActivateGreatEpeeClass();
				ClassManager.StartGreatEpeeRoutines(lc);
			}
			else if (ClassCheckUtil.IsMedicHat(lc))
			{
				ClassManager.ActivateMedicClass();
				float currentCurse = lc.refs?.afflictions?.GetCurrentStatus(
					CharacterAfflictions.STATUSTYPE.Curse) ?? 0f;
				if (currentCurse < ClassBalance.MEDIC_CURSE)
					lc.refs?.afflictions?.SetStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.MEDIC_CURSE);
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
			else if (ClassCheckUtil.IsGobelin(lc))
			{
				ClassManager.ActivateGobelinClass();
			}
			else if (ClassCheckUtil.IsScout(lc))
			{
				ClassManager.ActivateScoutClass();
			}
		}
	}

	// ==================================================
	// AFFLICTION PATCHES
	// ==================================================
	[HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
	public class General_AddStatus_Patch
	{
		static bool Prefix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, ref float amount)
		{
			var ch = __instance?.character;
			if (ch == null) return true;

			if (ch.IsLocal)
			{
				// Multiplicateurs de faim
				if (statusType == CharacterAfflictions.STATUSTYPE.Hunger)
				{
					if (ClassManager.IsAstronautActive())
						amount *= ClassBalance.ASTRONAUT_HUNGER_MULT;
					else if (ClassManager.IsScoutActive())
						amount *= ClassBalance.SCOUT_HUNGER_MULT;
					else if (ClassManager.IsGeneralActive())
						amount *= ClassBalance.GENERAL_HUNGER_MULT;
				}
			}

			// Tech Priest immunités
			if (ClassCheckUtil.IsTechPriest(ch) && ClassManager.IsTechPriestActive())
			{
				if (statusType == CharacterAfflictions.STATUSTYPE.Hunger)
				{
					amount = 0f;
				}
				else if (statusType == CharacterAfflictions.STATUSTYPE.Weight)
				{
					amount = 0f;
					return false;
				}
			}

			// Mushroomancer immunité poison
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
			if (Character.localCharacter == null ||
				__instance.afflictionType != CharacterAfflictions.STATUSTYPE.Weight)
				return;

			if (ClassManager.IsScoutActive())
				__instance.size *= ClassBalance.SCOUT_WEIGHT_MULT;
			else if (ClassManager.IsGeneralActive())
				__instance.size *= ClassBalance.GENERAL_WEIGHT_MULT;
			else if (ClassManager.IsTechPriestActive() &&
					 ClassCheckUtil.IsTechPriest(Character.localCharacter))
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
			if (__instance?.character == null || !__instance.character.IsLocal) return;

			float weight = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight);

			if (ClassManager.IsScoutActive())
				__result = __result - weight + (weight * ClassBalance.SCOUT_WEIGHT_MULT);
			else if (ClassManager.IsGeneralActive())
				__result = __result - weight + (weight * ClassBalance.GENERAL_WEIGHT_MULT);
			else if (ClassManager.IsTechPriestActive() && ClassCheckUtil.IsTechPriest(__instance.character))
				__result -= weight;
		}
	}

	[HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
	public class UpdateStatus_TechPriest_Patch
	{
		static void Postfix(CharacterAfflictions __instance)
		{
			if (__instance?.character != null &&
				ClassCheckUtil.IsTechPriest(__instance.character) &&
				ClassManager.IsTechPriestActive())
			{
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Hunger] = 0f;
				__instance.currentStatuses[(int)CharacterAfflictions.STATUSTYPE.Weight] = 0f;
			}
		}
	}

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_PoisonOverTime), "OnApplied")]
	public class PoisonPatch
	{
		static bool Prefix(Peak.Afflictions.Affliction_PoisonOverTime __instance)
			=> __instance?.character == null || !ClassCheckUtil.IsMushroom(__instance.character);
	}

	// ==================================================
	// ROGUE PATCHES
	// ==================================================
	[HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
	public class RogueJumpPatch
	{
		static void Postfix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>();
			if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
				ch.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS;
		}
	}

	[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
	public class RogueJumpRPCPatch
	{
		static bool Prefix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>();
			if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
			{
				if (ch.data.jumpsRemaining > 0 && ch.data.currentStamina > 0.005f)
				{
					ch.data.jumpsRemaining--;
					ch.data.chargingJump = true;
					ch.refs?.view?.RPC("JumpRpc", RpcTarget.All, new object[] { false });
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
			if (__instance == null || !__instance.IsLocal) return;

			if (ClassManager.IsScoutActive())
				usage *= ClassBalance.SCOUT_STAMINA_MULT;
			else if (ClassManager.IsRogueActive())
				usage *= ClassBalance.ROGUE_STAMINA_MULT;
		}
	}
}
