// ============================================================
// PEAK Class Mod - Version: 5.1.0 (Gobelin Infinite Fix)
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
	// CONSTANTES DE BALANCE
	// ==================================================
	public static class ClassBalance
	{
		// --- GOBELIN ---
		// Nom exact trouvé grâce à votre espionnage
		public const string BLOWGUN = "HealingDart Variant";

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

		public const float ROGUE_STAMINA_MULT = 1.7f;
		public const int ROGUE_EXTRA_JUMPS = 2;

		public const float GREATEPEE_SLEEP_MIN = 30f;
		public const float GREATEPEE_SLEEP_MAX = 60f;
		public const float GREATEPEE_EFFECT_DURATION = 99999f;

		public const float MUSHROOM_STAMINA_COOLDOWN = 10f;
		public const float MUSHROOM_CURSE_AMOUNT = 0.02f;

		public const float ITEM_REQUEST_DELAY = 0.3f;
		public const float ITEM_GIVE_COOLDOWN = 2f;
		public const float ITEM_CACHE_DURATION = 300f;
		public const float SIZE_CHECK_INTERVAL = 0.5f;
	}

	[BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "5.1.0")]
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

			Log?.LogInfo("[ClassMod] Ready. Version 5.1.0 loaded (Infinite Fix).");
		}



		private void OnDestroy()
		{
			try { harmony?.UnpatchSelf(); } catch { }
			if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
		}

		public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
		{
			if (go != null && go.GetComponent<T>() == null) go.AddComponent<T>();
		}

		public static List<string> BuildStartItemsByHatName(string hatName)
		{
			var items = new List<string>();
			if (string.IsNullOrEmpty(hatName)) return items;

			switch (hatName)
			{
				case "Hat_Mushroom":
					for (int i = 0; i < 3; i++) items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
					break;
				case "Hat_Medic": items.Add("FirstAidKit"); items.Add("Cure-All"); break;
				case "Hat_Crab": items.Add("ChainShooter"); items.Add("ChainShooter"); break;
				case "Hat_NinjaHeadband": items.Add("RopeSpool"); items.Add("RopeSpool"); break;
				case "Hat_Astronaut": items.Add("BalloonBunch"); break;
				case "Hat_Midsummer":items.Add(ClassBalance.BLOWGUN); items.Add("MagicBean"); break;
			}
			return items;
		}

		private static readonly string[] StartBerries = new[] { "Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green" };
	}

	// ==================================================
	// GOBELIN DIET (VERSION TAGS OPTIMISÉE)
	// ==================================================
	internal static class GobelinDiet
	{
		public static bool IsPackagedFood(Item item)
		{
			if (item == null) return false;

			// On vérifie si l'objet possède le tag "PackagedFood"
			// C'est une opération binaire (Bitwise check) car c'est un enum [Flags]
			bool hasTag = (item.itemTags & Item.ItemTags.PackagedFood) != 0;

			return hasTag;
		}
	}


	// ==================================================
	// GOBELIN INFINITE ITEM FIX (CORRIGÉ)
	// ==================================================

	[HarmonyPatch(typeof(Item), "Consume")]
	public class GobelinInfiniteConsumePatch
	{
		static bool Prefix(Item __instance)
		{
			// Sécurité de base
			if (__instance == null || __instance.holderCharacter == null) return true;

			// 1. Est-ce un Gobelin ?
			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				// 2. Est-ce la Sarbacane ?
				if (__instance.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// --- MANUELLEMENT FINIR L'ACTION ---

					// A. Mettre à jour le timer (pour dire "c'est bon, il a tiré")
					__instance.holderCharacter.data.lastConsumedItem = Time.time;

					// B. DÉBLOQUER L'ANIMATION (C'est ça qui bloquait !)
					// On dit à l'animateur : "L'objet est consommé, reviens en position normale"
					if (__instance.holderCharacter.refs != null && __instance.holderCharacter.refs.animator != null)
					{
						__instance.holderCharacter.refs.animator.SetBool("Consumed Item", true);
					}
			
					return false;
				}
			}

			return true;
		}
	}

	// ==================================================
	// APPLICATION DE LA CURSE AU TIR (FIABLE)
	// ==================================================
	[HarmonyPatch(typeof(Item), "FinishCastPrimary")]
	public class GobelinCurseOnShootPatch
	{
		static void Postfix(Item __instance)
		{
			// Sécurité
			if (__instance == null || __instance.holderCharacter == null) return;

			// On ne l'applique que pour le joueur Local (pour ne pas curse les autres sur mon écran)
			if (__instance.holderCharacter.IsLocal && ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				// Vérifier si c'est la Sarbacane
				if (__instance.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// Appliquer la Curse
					__instance.holderCharacter.refs?.afflictions?.AddStatus(
						CharacterAfflictions.STATUSTYPE.Curse,
						ClassBalance.GOBELIN_CURSE_PER_SHOT,
						false);

					// PeakClassModPlugin.Log?.LogInfo("[Gobelin] Pan ! Curse appliquée.");
				}
			}
		}
	}
	// ==================================================
	// PROTECTION ABSOLUE DE LA SARBACANE (CLIENT FIX & SOULBOUND V2)
	// ==================================================
	[HarmonyPatch(typeof(Item), "Update")]
	public class GobelinBlowgunEnforcer
	{
		static void Postfix(Item __instance)
		{
			// Sécurité de base
			if (__instance == null) return;

			// On ne s'intéresse qu'aux sarbacanes
			if (__instance.name.IndexOf(ClassBalance.BLOWGUN, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				Character holder = __instance.holderCharacter;
				bool heldByGobelin = holder != null && ClassCheckUtil.IsGobelin(holder);

				// --- CAS 1 : TENU PAR UN GOBELIN ---
				if (heldByGobelin)
				{
					// A. Logique Infini (Seulement Local pour éviter conflits réseau)
					if (holder.IsLocal)
					{
						var trav = Traverse.Create(__instance);
						var field = trav.Field("destroyOnUse");
						// On force la NON-destruction
						if (field.FieldExists() && field.GetValue<bool>() == true)
						{
							field.SetValue(false);
						}

						// On force les munitions à -1 (infini)
						try
						{
							if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
							{
								var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
								if (usesData != null && usesData.Value != -1) usesData.Value = -1;
							}
						}
						catch { }
					}

					// B. Logique Soulbound (Restrictions d'inventaire)
					if (__instance.UIData != null)
					{
						__instance.UIData.canDrop = false;        // Touche G interdite
						__instance.UIData.canBackpack = false;    // Interdit de mettre dans le sac (pour donner)
						__instance.UIData.canThrow = false;       // Interdit de lancer


						// CORRECTION CRITIQUE ICI :
						__instance.UIData.canPocket = true;       // DOIT ÊTRE TRUE sinon l'objet tombe quand on l'équipe !
					}
				}
				// --- CAS 2 : TENU PAR QUELQU'UN D'AUTRE (OU AU SOL) ---
				else
				{
					// Si ce n'est pas un Gobelin qui la tient, on remet tout à la normale.
					// C'est crucial si le Gobelin meurt et lâche son arme.

					// 1. Réactiver les mouvements d'inventaire
					if (__instance.UIData != null)
					{
						__instance.UIData.canDrop = true;
						__instance.UIData.canBackpack = true;
						__instance.UIData.canThrow = true;

						__instance.UIData.canPocket = true;
					}

					// 2. RETIRER L'INFINI (Correction du bug pour les autres joueurs)
					// On remet la destruction active
					var trav = Traverse.Create(__instance);
					var field = trav.Field("destroyOnUse");
					if (field.FieldExists())
					{
						// On remet à TRUE (l'objet doit se détruire après usage pour les non-Gobelins)
						field.SetValue(true);
					}

					// On remet les munitions à 1 (usage unique) si elles étaient à -1
					try
					{
						if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
						{
							var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
							if (usesData != null && usesData.Value == -1)
							{
								usesData.Value = 1;
							}
						}
					}
					catch { }
				}
			}
		}
	}

	[HarmonyPatch(typeof(Item), "StartUsePrimary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerPrimary
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				// On vérifie si c'est de la nourriture ET si c'est "PackagedFood" via les tags
				if (__instance.GetComponent<Action_RestoreHunger>() != null &&
					GobelinDiet.IsPackagedFood(__instance)) // Nouvelle méthode
				{
					// PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Beurk ! Nourriture industrielle refusée : {__instance.name}");
					return false; // Bloque l'action
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	[HarmonyPriority(Priority.High)]
	public class GobelinFoodBlockerSecondary
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null || __instance.holderCharacter == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.holderCharacter))
			{
				if (__instance.GetComponent<Action_RestoreHunger>() != null &&
					GobelinDiet.IsPackagedFood(__instance)) // Nouvelle méthode
				{
					return false; // Bloque l'action
				}
			}
			return true;
		}
	}
	// ==================================================
	// EMPÊCHER DE NOURRIR LE GOBELIN (FIXED)
	// ==================================================
	[HarmonyPatch(typeof(Item), "StartUseSecondary")]
	public class GobelinAntiFeedPatch
	{
		static bool Prefix(Item __instance)
		{
			if (__instance == null) return true;

			// 1. Est-ce de la nourriture industrielle ?
			if (__instance.GetComponent<Action_RestoreHunger>() != null &&
				GobelinDiet.IsPackagedFood(__instance))
			{
				// 2. Accéder au système d'interaction
				var interact = Interaction.instance;
				if (interact == null) return true;

				// 3. REFLECTION : On force l'accès à la variable interne 'bestCharacter'
				// On utilise Traverse pour lire le champ "bestCharacter" dans l'instance Interaction
				var bestCharInteractible = Traverse.Create(interact).Field("bestCharacter").GetValue<MonoBehaviour>();

				// Si on vise quelqu'un (bestCharacter n'est pas null)
				if (bestCharInteractible != null)
				{
					// 4. On récupère le "Character" à l'intérieur de CharacterInteractible
					// (On suppose que CharacterInteractible a un champ public 'character')
					// Comme on n'a pas le type CharacterInteractible, on utilise aussi Traverse par sécurité
					var targetChar = Traverse.Create(bestCharInteractible).Field("character").GetValue<Character>();

					// 5. Vérification finale : Si la cible est un Gobelin
					if (targetChar != null && ClassCheckUtil.IsGobelin(targetChar))
					{
						// PeakClassModPlugin.Log?.LogInfo($"[Anti-Feed] Bloqué : On ne nourrit pas le Gobelin !");
						return false; // BLOQUE L'ACTION
					}
				}
			}
			return true;
		}
	}

	public class GobelinSizeHandler : MonoBehaviour
	{
		private Character _character;
		private float _lastCheckTime = 0f;

		private void Start()
		{
			_character = GetComponent<Character>();
		}

		private void Update()
		{
			// Vérification fréquente (toutes les 0.2s)
			if (Time.time - _lastCheckTime < 0.2f) return;
			_lastCheckTime = Time.time;

			if (_character == null) return;

			// 1. Est-ce que ce personnage doit être un Gobelin ?
			bool shouldBeSmall = false;

			if (_character.IsLocal)
			{
				// Pour MOI : Je regarde ma classe active
				shouldBeSmall = ClassManager.IsGobelinActive();
			}
			else
			{
				// Pour les AUTRES : Je regarde leur chapeau
				shouldBeSmall = ClassCheckUtil.IsGobelin(_character);
			}

			// 2. Application FORCE BRUTE
			// On regarde la taille actuelle. Si elle est différente de la cible, on force.
			// Vector3.one = (1,1,1) | GOBELIN_SIZE = 0.5

			float currentScale = _character.transform.localScale.x; // On regarde juste X par simplicité
			float targetScale = shouldBeSmall ? ClassBalance.GOBELIN_SIZE : 1f;

			// Si la différence est significative (plus de 0.05), on corrige
			if (Mathf.Abs(currentScale - targetScale) > 0.05f)
			{
				_character.transform.localScale = Vector3.one * targetScale;
				// PeakClassModPlugin.Log?.LogInfo($"[Gobelin] Correction de taille appliquée sur {_character.name}");
			}
		}
	}

	// ==================================================
	// NETWORK & UTILS
	// ==================================================
	public class ClassModNetwork : MonoBehaviourPun
	{
		private Character _character;
		private bool _hasRequestedItems = false;
		private static Dictionary<int, float> _hostGiveCooldown = new Dictionary<int, float>();
		private static Dictionary<int, Character> _characterCache = new Dictionary<int, Character>();
		public static HashSet<int> HasReceivedStartItems = new HashSet<int>();

		private void Awake() { var c = GetComponents<ClassModNetwork>(); if (c.Length > 1) { Destroy(this); return; } _character = GetComponent<Character>(); }
		private void Start() { if (photonView != null && photonView.IsMine && !_hasRequestedItems) { _hasRequestedItems = true; StartCoroutine(RequestItemsRoutine()); } }
		private void OnDestroy() { if (_character?.photonView?.Owner != null) _characterCache.Remove(_character.photonView.Owner.ActorNumber); }
		public static void ClearPlayerCache(int actorNumber) { _characterCache.Remove(actorNumber); _hostGiveCooldown.Remove(actorNumber); HasReceivedStartItems.Remove(actorNumber); }

		private IEnumerator RequestItemsRoutine()
		{
			yield return new WaitForSeconds(ClassBalance.ITEM_REQUEST_DELAY);
			while (!PhotonNetwork.InRoom) yield return null;
			if (_character == null) _character = GetComponent<Character>();
			float timeout = 5f; bool inventoryReady = false;
			while (timeout > 0)
			{
				if (_character?.player?.itemSlots != null && _character.player.itemSlots.Length > 0 && !_character.player.itemSlots[0].IsEmpty()) { inventoryReady = true; break; }
				timeout -= 0.1f; yield return new WaitForSeconds(0.1f);
			}
			string myHatName = "None"; if (HatUtils.TryGetHatName(_character, out string d)) myHatName = d;
			if (inventoryReady || timeout <= 0) photonView.RPC("RPC_HostGiveItems", RpcTarget.MasterClient, myHatName);
		}

		[PunRPC]
		public void RPC_HostGiveItems(string clientHatName, PhotonMessageInfo info)
		{
			if (!PhotonNetwork.IsMasterClient) return;
			int actorID = info.Sender.ActorNumber;

			// Si le joueur a déjà reçu ses items de départ dans cette run, on ignore
			if (HasReceivedStartItems.Contains(actorID)) return;

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

			// On marque le joueur comme ayant reçu ses items de départ
			HasReceivedStartItems.Add(actorNumber);

			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null && targetChar.player.HasEmptySlot((ushort)item.itemID)) targetChar.player.AddItem((ushort)item.itemID, null, out _);
				yield return new WaitForSeconds(0.02f);
			}
		}
		[PunRPC] public void ShowMedicHealEffectRPC(int remainingHeals) { PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Medic heal: {remainingHeals}"); }
		[PunRPC] public void RPC_SyncClassActivation(string className, int actorNumber) { PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Player {actorNumber} class: {className}"); }
	}

	public class PeakClassModCallbacks : MonoBehaviourPunCallbacks { public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => ClassModNetwork.ClearPlayerCache(otherPlayer.ActorNumber); }

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
		public static bool HasHat(Character ch, string expected) => TryGetHatName(ch, out var name) && string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
	}

	internal static class ClassCheckUtil
	{
		public static bool IsGeneral(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Bandana", "Hat_Captain");
		public static bool IsTechPriest(Character ch) => HatUtils.HasHat(ch, "Hat_Crab");
		public static bool IsMedicHat(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Medic", "MedicHelmet");
		public static bool IsMushroom(Character ch) => HatUtils.HasHat(ch, "Hat_Mushroom");
		public static bool IsRogueHat(Character ch) => HatUtils.HasHat(ch, "Hat_NinjaHeadband");
		public static bool IsGreatEpeeHat(Character ch) => HatUtils.HasHat(ch, "Hat_Propeller");
		public static bool IsAstronaut(Character ch) => HatUtils.HasHat(ch, "Hat_Astronaut");
		public static bool IsGobelin(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Midsummer"); // Corrected
		public static bool IsScout(Character ch) => HatUtils.HasHat(ch, "Hat_Scoutmaster");
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

	public static class ClassManager
	{
		private static readonly Dictionary<int, float> lastStaminaZeroTime = new Dictionary<int, float>();
		private static readonly Dictionary<int, int> staminaZeroCount = new Dictionary<int, int>();
		private static Coroutine _mushroomCoroutine;
		private static Coroutine _greatEpeeEffectsCoroutine;
		private static Coroutine _greatEpeeStaminaCoroutine;

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

		public static void ActivateScoutClass() { ResetAllFlags(); PeakClassModPlugin.scoutActive = true; PeakClassModPlugin.currentClass = "Scout"; }
		public static void ActivateMedicClass() { ResetAllFlags(); PeakClassModPlugin.medicActive = true; PeakClassModPlugin.currentClass = "Medic"; }
		public static void ActivateTechPriestClass() { ResetAllFlags(); PeakClassModPlugin.techPriestActive = true; PeakClassModPlugin.currentClass = "Tech-Priest"; }
		public static void ActivateMushroomancerClass() { ResetAllFlags(); PeakClassModPlugin.mushroomancerActive = true; PeakClassModPlugin.currentClass = "Mushroomancer"; }
		public static void ActivateGreatEpeeClass() { ResetAllFlags(); PeakClassModPlugin.greatEpeeActive = true; PeakClassModPlugin.currentClass = "GreatEpee"; }
		public static void ActivateRogueClass() { ResetAllFlags(); PeakClassModPlugin.rogueActive = true; PeakClassModPlugin.currentClass = "Rogue"; if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; }
		public static void ActivateGeneralClass() { ResetAllFlags(); PeakClassModPlugin.generalActive = true; PeakClassModPlugin.currentClass = "General"; }
		public static void ActivateAstronautClass() { ResetAllFlags(); PeakClassModPlugin.astronautActive = true; PeakClassModPlugin.currentClass = "Astronaut"; }
		public static void ActivateGobelinClass() { ResetAllFlags(); PeakClassModPlugin.gobelinActive = true; PeakClassModPlugin.currentClass = "Gobelin"; }

		public static void DeactivateAllClasses() { ResetAllFlags(); if (Character.localCharacter != null) Character.localCharacter.data.jumpsRemaining = 1; }

		public static bool IsScoutActive() => PeakClassModPlugin.scoutActive;
		public static bool IsMedicActive() => PeakClassModPlugin.medicActive;
		public static bool IsTechPriestActive() => PeakClassModPlugin.techPriestActive;
		public static bool IsMushroomancerActive() => PeakClassModPlugin.mushroomancerActive;
		public static bool IsGreatEpeeActive() => PeakClassModPlugin.greatEpeeActive;
		public static bool IsRogueActive() => PeakClassModPlugin.rogueActive;
		public static bool IsGeneralActive() => PeakClassModPlugin.generalActive;
		public static bool IsAstronautActive() => PeakClassModPlugin.astronautActive;
		public static bool IsGobelinActive() => PeakClassModPlugin.gobelinActive;

		private static readonly string[] ZeroStaminaFungi = new[] { "ShelfShroom", "BounceShroom", "CloudFungus" };

		public static void StartMushroomancerWatch(Character character) { if (character != null) _mushroomCoroutine = character.StartCoroutine(MushroomancerLocalStaminaWatch(character)); }

		public static void StartGreatEpeeRoutines(Character character)
		{
			if (character != null)
			{
				_greatEpeeEffectsCoroutine = character.StartCoroutine(GreatEpeePermanentEffects(character));
				_greatEpeeStaminaCoroutine = character.StartCoroutine(GreatEpeeStaminaRoutine(character));
			}
		}

		// --- CORRECTION MUSHROOMANCER ---
		public static IEnumerator MushroomancerLocalStaminaWatch(Character character)
		{
			while (character != null && PeakClassModPlugin.mushroomancerActive && character.IsLocal)
			{
				// Vérification basée sur le code source DnSpy :
				// On vérifie si le joueur est évanoui (PassedOut) OU mort.
				bool isKnockedOut = false;

				if (character.data != null)
				{
					if (character.data.passedOut || character.data.fullyPassedOut || character.data.dead)
					{
						isKnockedOut = true;
					}
				}

				// On applique la punition SEULEMENT si le joueur est conscient (debout)
				if (!isKnockedOut && character.data.currentStamina <= 0.02f)
				{
					TryActivateStaminaZeroMechanic(character);
				}

				yield return new WaitForSeconds(0.1f);
			}
		}

		public static void TryActivateStaminaZeroMechanic(Character character)
		{
			if (character?.photonView?.Owner == null) return;
			int actor = character.photonView.Owner.ActorNumber;
			if (lastStaminaZeroTime.TryGetValue(actor, out var last) && Time.time - last < ClassBalance.MUSHROOM_STAMINA_COOLDOWN) return;
			lastStaminaZeroTime[actor] = Time.time;

			// On incrémente le compteur
			if (!staminaZeroCount.ContainsKey(actor)) staminaZeroCount[actor] = 0;
			staminaZeroCount[actor]++;

			// "gives 2% curse every second time!"
			// Donc une fois sur deux (les nombres pairs : 2, 4, 6...)
			if (staminaZeroCount[actor] % 2 == 0)
			{
				character.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MUSHROOM_CURSE_AMOUNT, false);
			}

			string randomName = ZeroStaminaFungi[UnityEngine.Random.Range(0, ZeroStaminaFungi.Length)];
			Item fungusItem = ItemCache.Get(randomName);
			if (fungusItem == null) return;
			ushort id = (ushort)fungusItem.itemID;

			// Retarder SEULEMENT le don de l'objet via coroutine
			if (character != null) {
				character.StartCoroutine(MushroomancerGiveItemDelayed(character, id));
			}
		}

		private static IEnumerator MushroomancerGiveItemDelayed(Character character, ushort itemId)
		{
			// Wait maximum of 5 seconds to avoid memory leaks
			float timeout = 5f;
			while (character != null && character.data != null && (!character.data.isGrounded || character.data.isSprinting) && timeout > 0)
			{
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			if (character != null && character.player != null && character.player.itemSlots != null)
			{
				// On vérifie MANUELLEMENT les 3 slots de base (0, 1, 2)
				bool hasSpaceInBaseSlots = false;
				int maxSlotsToCheck = Mathf.Min(3, character.player.itemSlots.Length);

				for (int i = 0; i < maxSlotsToCheck; i++)
				{
					if (character.player.itemSlots[i] != null && character.player.itemSlots[i].IsEmpty())
					{
						hasSpaceInBaseSlots = true;
						break;
					}
				}

				// On ne donne l'objet QUE si on a de la place dans les slots principaux
				if (hasSpaceInBaseSlots && character.player.HasEmptySlot(itemId))
				{
					character.player.AddItem(itemId, null, out _);
				}
				// Sinon, l'inventaire principal est plein. L'objet ne sera PAS forcé dans le slot 4 (les mains).
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
				while (character?.refs?.afflictions != null && character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0.05f)
				{
					yield return new WaitForSeconds(1f);
					if (!PeakClassModPlugin.greatEpeeActive) yield break;
				}
				if (character == null) yield break;
				yield return new WaitForSeconds(UnityEngine.Random.Range(ClassBalance.GREATEPEE_SLEEP_MIN, ClassBalance.GREATEPEE_SLEEP_MAX));
				if (character?.refs?.afflictions == null || !PeakClassModPlugin.greatEpeeActive) yield break;
				if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) < 0.1f)
					character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 1f, false);
			}
		}
	}

	// ==================================================
	// AUTRES HANDLERS (TechPriest, Astronaut, etc.)
	// ==================================================
	public class TechPriestWeightEnforcer : MonoBehaviour
	{
		private Character _character; private CharacterAfflictions _afflictions; private bool _isActive; private int _weightIndex; private float _lastCheck = 0f;
		private void Start() { _character = GetComponent<Character>(); _weightIndex = (int)CharacterAfflictions.STATUSTYPE.Weight; RefreshState(); }
		private void RefreshState() { _isActive = _character != null && ClassCheckUtil.IsTechPriest(_character); if (_isActive) _afflictions = _character.refs.afflictions; }
		private void LateUpdate()
		{
			if (!_isActive || !ClassManager.IsTechPriestActive()) { if (Time.time - _lastCheck > 2f) { _lastCheck = Time.time; RefreshState(); } return; }
			if (_afflictions != null && _afflictions.currentStatuses[_weightIndex] > 0f) _afflictions.currentStatuses[_weightIndex] = 0f;
		}
	}

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

	public class MedicStatHandler : MonoBehaviour
	{
		private Character _character; private ClassModNetwork _network; private CharacterAfflictions _afflictions; private int _healsRemaining = ClassBalance.MEDIC_MAX_HEALS; private float _lastHealTime = 0f;
		private void Start() { _character = GetComponent<Character>(); _network = GetComponent<ClassModNetwork>(); if (_character?.refs != null) _afflictions = _character.refs.afflictions; }
		private void Update() { if (_character == null || !_character.IsLocal) return; if (ClassManager.IsMedicActive()) { if (Input.GetKeyDown(KeyCode.H) && Time.time - _lastHealTime > ClassBalance.MEDIC_HEAL_COOLDOWN) TrySelfHeal(); } else if (_healsRemaining < ClassBalance.MEDIC_MAX_HEALS) _healsRemaining = ClassBalance.MEDIC_MAX_HEALS; }
		private void TrySelfHeal() { if (_healsRemaining <= 0) return; if (_afflictions == null && _character?.refs != null) _afflictions = _character.refs.afflictions; if (_afflictions != null && _afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f) { _healsRemaining--; _lastHealTime = Time.time; _afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, ClassBalance.MEDIC_HEAL_AMOUNT, false); if (_network?.photonView != null) _network.photonView.RPC("ShowMedicHealEffectRPC", RpcTarget.All, _healsRemaining); } }
	}

	[HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
	public class SetHat_Patch
	{
		static void Postfix(int index)
		{
			var hats = Singleton<Customization>.Instance?.hats;
			string hatName = (hats != null && index >= 0 && index < hats.Length) ? hats[index]?.name : "Unknown";
			HatUtils.SetLastLocalHatName(hatName);
			switch (hatName)
			{
				case "Hat_Scoutmaster": ClassManager.ActivateScoutClass(); break;
				case "Hat_Medic": case "MedicHelmet": ClassManager.ActivateMedicClass(); break;
				case "Hat_Crab": ClassManager.ActivateTechPriestClass(); break;
				case "Hat_Mushroom": ClassManager.ActivateMushroomancerClass(); break;
				case "Hat_Propeller": ClassManager.ActivateGreatEpeeClass(); break;
				case "Hat_NinjaHeadband": ClassManager.ActivateRogueClass(); break;
				case "Hat_Bandana": case "Hat_Captain": ClassManager.ActivateGeneralClass(); break;
				case "Hat_Astronaut": ClassManager.ActivateAstronautClass(); break;
				case "Hat_Midsummer": ClassManager.ActivateGobelinClass(); break;
				default: ClassManager.DeactivateAllClasses(); break;
			}
			if (Character.localCharacter != null && GUIManager.instance?.bar != null) GUIManager.instance.bar.ChangeBar();
		}
	}

	[HarmonyPatch(typeof(RunManager), "StartRun")]
	public class RunManager_StartRun_Patch
	{
		static void Postfix() {
			if (PhotonNetwork.IsMasterClient) { ClassModNetwork.HasReceivedStartItems.Clear(); }
			PeakClassModPlugin.Instance?.StartCoroutine(AddEffectsDelayed());
		}
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
			// --- GOBELIN IMMUNITY (NOUVEAU) ---
			if (ClassCheckUtil.IsGobelin(ch))
			{
				// Si on essaie d'ajouter des Spores au Gobelin
				if (statusType == CharacterAfflictions.STATUSTYPE.Spores)
				{
					amount = 0f; // On annule le montant
					return false; // On bloque l'exécution
				}
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
	[HarmonyPatch(typeof(CharacterAfflictions), "AddAffliction")]
	public class GobelinSporeAfflictionPatch
	{
		static bool Prefix(CharacterAfflictions __instance, Affliction affliction)
		{
			if (__instance == null || __instance.character == null || affliction == null) return true;

			if (ClassCheckUtil.IsGobelin(__instance.character))
			{
				string name = affliction.GetType().Name;
				// Bloque la confusion et tout ce qui contient "Spore" dans le nom du script
				if (name.Contains("Confus") || name.Contains("Spore"))
				{
					return false; // Bloque l'effet
				}
			}
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

	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_PoisonOverTime), "OnApplied")]
	public class PoisonPatch { static bool Prefix(Peak.Afflictions.Affliction_PoisonOverTime __instance) => __instance?.character == null || !ClassCheckUtil.IsMushroom(__instance.character); }

	[HarmonyPatch(typeof(CharacterMovement), "EvaluateGroundChecks")]
	public class RogueJumpPatch { static void Postfix(CharacterMovement __instance) { var ch = __instance?.GetComponent<Character>(); if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive) ch.data.jumpsRemaining = ClassBalance.ROGUE_EXTRA_JUMPS; } }

	[HarmonyPatch(typeof(CharacterMovement), "TryToJump")]
	public class RogueJumpRPCPatch
	{
		static bool Prefix(CharacterMovement __instance)
		{
			var ch = __instance?.GetComponent<Character>();
			if (ch != null && ch.IsLocal && PeakClassModPlugin.rogueActive)
			{
				// Remove the jumpsRemaining limit for Rogue. They just need stamina.
				if (ch.data.currentStamina > 0.005f)
				{
					// Maintain jumpsRemaining at 1 so the game allows the jump
					ch.data.jumpsRemaining = 1;
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
			if (ClassManager.IsScoutActive()) usage *= ClassBalance.SCOUT_STAMINA_MULT;
			else if (ClassManager.IsRogueActive()) usage *= ClassBalance.ROGUE_STAMINA_MULT;
		}
	}

	internal static class TechPriestBlocker
	{
		private static readonly HashSet<string> BlockedTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "heal", "cure", "medicine", "restorehealth" };
		private static readonly HashSet<string> BlockedItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bandage", "firstaid", "medkit", "cure", "marshmallow" };
		private static Dictionary<int, bool> _blockCache = new Dictionary<int, bool>();
		public static bool ShouldBlockItem(Item item)
		{
			if (item == null) return false; int id = item.GetInstanceID(); if (_blockCache.TryGetValue(id, out bool cached)) return cached;
			bool block = false;
			if (item.GetComponent<Action_RestoreHunger>() != null || item.GetComponent<Action_GiveExtraStamina>() != null) block = true;
			if (!block) { string n = item.name?.Replace("(Clone)", "").Trim() ?? ""; foreach (var b in BlockedItemNames) if (n.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0) { block = true; break; } }
			if (!block) { foreach (var c in item.GetComponents<MonoBehaviour>()) { if (c == null) continue; string t = c.GetType().Name; foreach (var b in BlockedTypeNames) if (t.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0) { block = true; break; } if (block) break; } }
			_blockCache[id] = block; return block;
		}
		public static void ClearCache() => _blockCache.Clear();
	}
	internal static class TechPriestItemChecker
	{
		public static bool ShouldBlockUse(Item item)
		{
			if (item == null || item.holderCharacter == null || !ClassCheckUtil.IsTechPriest(item.holderCharacter)) return false;
			if (TechPriestBlocker.ShouldBlockItem(item)) return true;
			var a = item.GetComponent<Action_ApplyAffliction>();
			return a?.affliction != null && a.affliction.GetType().Name.Contains("FasterBoi");
		}
	}
	[HarmonyPatch(typeof(Item), "StartUsePrimary")][HarmonyPriority(Priority.Normal)] public class TechPriestConsumablePatch1 { static bool Prefix(Item __instance) => !TechPriestItemChecker.ShouldBlockUse(__instance); }
	[HarmonyPatch(typeof(Item), "StartUseSecondary")][HarmonyPriority(Priority.Normal)] public class TechPriestConsumablePatch2 { static bool Prefix(Item __instance) => !TechPriestItemChecker.ShouldBlockUse(__instance); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_FasterBoi), "OnApplied")] public class TechPriestFasterBoiBlock { static bool Prefix(Peak.Afflictions.Affliction_FasterBoi __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
	[HarmonyPatch(typeof(Character), "AddExtraStamina")] public class TechPriestStaminaPatch1 { static bool Prefix(Character __instance) => __instance == null || !ClassCheckUtil.IsTechPriest(__instance); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "OnApplied")] public class TechPriestStaminaPatch2 { static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
	[HarmonyPatch(typeof(Peak.Afflictions.Affliction_InfiniteStamina), "UpdateEffect")] public class TechPriestStaminaPatch2_Update { static bool Prefix(Peak.Afflictions.Affliction_InfiniteStamina __instance) => __instance?.character == null || !ClassCheckUtil.IsTechPriest(__instance.character); }
}
