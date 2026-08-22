using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using PeakClassMod.Utils;
using HarmonyLib;

namespace PeakClassMod.Handlers
{
	public class ClassModNetwork : MonoBehaviourPun
	{
		private Character _character;
		private bool _hasGivenItemsForThisInstance = false;
		private static Dictionary<int, string> _networkClasses = new Dictionary<int, string>();

		private void Awake()
		{
			var c = GetComponents<ClassModNetwork>();
			if (c.Length > 1) { Destroy(this); return; }
			_character = GetComponent<Character>();
		}

		private void Start()
		{
			if (photonView != null && photonView.IsMine && !_hasGivenItemsForThisInstance)
			{
				StartCoroutine(GiveItemsRoutine());
			}
		}

		private void OnDestroy()
		{
			if (_character?.photonView?.Owner != null)
			{
				_networkClasses.Remove(_character.photonView.Owner.ActorNumber);
			}
		}

		public static void ClearPlayerCache(int actorNumber)
		{
			_networkClasses.Remove(actorNumber);
		}

		public static string GetPlayerClass(Character ch)
		{
			if (ch?.photonView?.Owner != null && _networkClasses.TryGetValue(ch.photonView.Owner.ActorNumber, out var cl))
				return cl;
			return "None";
		}

		public void BroadcastClass(string className)
		{
			if (photonView != null && PhotonNetwork.InRoom)
			{
				photonView.RPC(nameof(RPC_SyncClassActivation), RpcTarget.AllBuffered, className, PhotonNetwork.LocalPlayer.ActorNumber);
			}
		}

		public void TriggerStartItems()
		{
			if (!_hasGivenItemsForThisInstance)
			{
				StartCoroutine(GiveItemsRoutine());
			}
		}

		private IEnumerator GiveItemsRoutine()
		{
			if (_hasGivenItemsForThisInstance) yield break;

			yield return new WaitForSeconds(1.0f);

			if (_character == null) _character = GetComponent<Character>();

			float timeout = 10f;
			while (timeout > 0)
			{
				if (_character?.player != null && _character.player.itemSlots != null && _character.player.itemSlots.Length > 0)
					break;
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			yield return new WaitForSeconds(0.3f);

			if (_hasGivenItemsForThisInstance) yield break;
			_hasGivenItemsForThisInstance = true;

			string myHatName = "None";
			if (HatUtils.TryGetHatName(_character, out string d)) myHatName = d;

			GiveItemsLocally(myHatName);
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal || _character.player == null) return;

			// Touche F8 pour purger le slot 0 (tempFullSlot) en cas de besoin
			if (Input.GetKeyDown(KeyCode.F8))
			{
				if (_character.player.tempFullSlot != null && !_character.player.tempFullSlot.IsEmpty())
				{
					_character.player.tempFullSlot.EmptyOut();
					PeakClassModPlugin.Log?.LogInfo("[ClassMod] Slot 0 (tempFullSlot) purgé par l'utilisateur (F8).");
				}
			}
		}

		public void GiveItemsLocally(string hatName)
		{
			if (_character?.player == null) return;

			// Nettoyage préventif du slot 0
			if (_character.player.tempFullSlot != null && !_character.player.tempFullSlot.IsEmpty())
			{
				_character.player.tempFullSlot.EmptyOut();
			}

			var itemsToGive = PeakClassModPlugin.BuildStartItemsByHatName(hatName);
			if (itemsToGive == null || itemsToGive.Count == 0) return;

			// Compte le nombre d'exemplaires demandés par item
			var requestedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var name in itemsToGive)
			{
				if (!requestedCounts.ContainsKey(name)) requestedCounts[name] = 0;
				requestedCounts[name]++;
			}

			foreach (var kvp in requestedCounts)
			{
				string itemName = kvp.Key;
				int targetCount = kvp.Value;

				var item = ItemCache.Get(itemName);
				if (item == null) continue;

				ushort id = (ushort)item.itemID;
				int currentCount = CountItemInInventory(_character, id, itemName);
				int needed = targetCount - currentCount;

				for (int i = 0; i < needed; i++)
				{
					// Vérifie qu'il y a au moins un slot d'inventaire de base disponible (Slots 1, 2, 3)
					bool hasBaseSlot = false;
					if (_character.player.itemSlots != null)
					{
						for (int s = 0; s < _character.player.itemSlots.Length; s++)
						{
							if (_character.player.itemSlots[s] != null && _character.player.itemSlots[s].IsEmpty())
							{
								hasBaseSlot = true;
								break;
							}
						}
					}

					if (hasBaseSlot && _character.player.HasEmptySlot(id))
					{
						_character.player.AddItem(id, null, out _);
						PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Given starter item '{itemName}' (ID: {id}) ({i + 1}/{needed}) to {_character.name}");
					}
				}
			}

			// S'assure que le slot 0 n'a pas été rempli
			if (_character.player.tempFullSlot != null && !_character.player.tempFullSlot.IsEmpty())
			{
				_character.player.tempFullSlot.EmptyOut();
			}
		}

		public static int CountItemInInventory(Character ch, ushort itemID, string itemName)
		{
			if (ch == null || ch.player == null) return 0;
			int count = 0;

			// 1. Objet actuellement tenu en main
			if (ch.data != null && ch.data.currentItem != null)
			{
				if (ch.data.currentItem.itemID == itemID || ch.data.currentItem.name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
					count++;
			}

			// 2. Inspection des slots de joueur
			if (ch.player.itemSlots != null)
			{
				foreach (var slot in ch.player.itemSlots)
				{
					if (slot != null && !slot.IsEmpty())
					{
						if (slot.prefab != null && (slot.prefab.itemID == itemID || slot.prefab.name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0))
						{
							count++;
							continue;
						}
						string pName = slot.GetPrefabName();
						if (!string.IsNullOrEmpty(pName) && pName.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							count++;
						}
					}
				}
			}

			// 3. Inspection du sac à dos
			if (ch.player.backpackSlot != null && !ch.player.backpackSlot.IsEmpty())
			{
				if (ch.player.backpackSlot.prefab != null && (ch.player.backpackSlot.prefab.itemID == itemID || ch.player.backpackSlot.prefab.name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0))
				{
					count++;
				}
				else
				{
					string bpName = ch.player.backpackSlot.GetPrefabName();
					if (!string.IsNullOrEmpty(bpName) && bpName.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
						count++;
				}
			}

			return count;
		}

		public static bool HasItemInInventory(Character ch, ushort itemID, string itemName)
		{
			return CountItemInInventory(ch, itemID, itemName) > 0;
		}

		[PunRPC]
		public void RPC_ApplyWolfHeal(float amount)
		{
			if (_character == null) _character = GetComponent<Character>();
			_character?.refs?.afflictions?.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, amount, false);
		}

		[PunRPC]
		public void RPC_ApplyWolfDamage(float amount)
		{
			if (_character == null) _character = GetComponent<Character>();
			_character?.refs?.afflictions?.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, amount, false, true, true);
		}

		[PunRPC]
		public void ShowMedicHealEffectRPC(int healsRemaining)
		{
			if (_character == null) _character = GetComponent<Character>();
			if (_character?.refs?.customization != null)
			{
				_character.refs.customization.PulseStatus(new Color(0.2f, 1f, 0.4f, 1f), 1.5f);
			}
		}

		[PunRPC]
		public void RPC_SyncClassActivation(string className, int actorNumber)
		{
			_networkClasses[actorNumber] = className;
		}
	}
}