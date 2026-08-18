using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Handlers
{
	public class ClassModNetwork : MonoBehaviourPun
	{
		private Character _character;
		private bool _hasRequestedItems = false;
		private static Dictionary<int, float> _hostGiveCooldown = new Dictionary<int, float>();
		private static Dictionary<int, Character> _characterCache = new Dictionary<int, Character>();
		private static Dictionary<int, string> _networkClasses = new Dictionary<int, string>();
		public static HashSet<int> HasReceivedStartItems = new HashSet<int>();

		private void Awake()
		{
			var c = GetComponents<ClassModNetwork>();
			if (c.Length > 1) { Destroy(this); return; }
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
				int act = _character.photonView.Owner.ActorNumber;
				_characterCache.Remove(act);
				_networkClasses.Remove(act);
			}
		}

		public static void ClearPlayerCache(int actorNumber)
		{
			_characterCache.Remove(actorNumber);
			_networkClasses.Remove(actorNumber);
			_hostGiveCooldown.Remove(actorNumber);
			HasReceivedStartItems.Remove(actorNumber);
		}

		public static void ResetAllPlayerItemFlags()
		{
			HasReceivedStartItems.Clear();
			_hostGiveCooldown.Clear();
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
			_hasRequestedItems = false;
			StartCoroutine(RequestItemsRoutine());
		}

		private IEnumerator RequestItemsRoutine()
		{
			yield return new WaitForSeconds(ClassBalance.ITEM_REQUEST_DELAY);

			if (_character == null) _character = GetComponent<Character>();

			// Attend que les slots soient prêts
			float timeout = 8f;
			while (timeout > 0)
			{
				if (_character?.player != null && _character.player.itemSlots != null && _character.player.itemSlots.Length > 0)
					break;
				timeout -= 0.1f;
				yield return new WaitForSeconds(0.1f);
			}

			yield return new WaitForSeconds(0.5f);

			string myHatName = "None";
			if (HatUtils.TryGetHatName(_character, out string d)) myHatName = d;

			if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
			{
				photonView.RPC(nameof(RPC_HostGiveItems), RpcTarget.MasterClient, myHatName);
			}
			else
			{
				GiveItemsLocally(myHatName);
			}
		}

		public void GiveItemsLocally(string hatName)
		{
			if (_character?.player == null) return;
			var itemsToGive = PeakClassModPlugin.BuildStartItemsByHatName(hatName);
			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null && _character.player.HasEmptySlot((ushort)item.itemID))
				{
					_character.player.AddItem((ushort)item.itemID, null, out _);
				}
			}
		}

		[PunRPC]
		public void RPC_HostGiveItems(string clientHatName, PhotonMessageInfo info)
		{
			if (!PhotonNetwork.IsMasterClient) return;
			int actorID = info.Sender.ActorNumber;
			if (HasReceivedStartItems.Contains(actorID)) return;

			if (_hostGiveCooldown.TryGetValue(actorID, out float lastTime))
				if (Time.time - lastTime < ClassBalance.ITEM_GIVE_COOLDOWN) return;

			_hostGiveCooldown[actorID] = Time.time;
			StartCoroutine(HostGiveItemsRoutine(clientHatName, info.Sender));
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

			HasReceivedStartItems.Add(actorNumber);

			foreach (var itemName in itemsToGive)
			{
				var item = ItemCache.Get(itemName);
				if (item != null && targetChar.player.HasEmptySlot((ushort)item.itemID))
				{
					targetChar.player.AddItem((ushort)item.itemID, null, out _);
				}
				yield return new WaitForSeconds(0.05f);
			}
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

		[PunRPC] public void ShowMedicHealEffectRPC(int remainingHeals) { }

		[PunRPC]
		public void RPC_SyncClassActivation(string className, int actorNumber)
		{
			_networkClasses[actorNumber] = className;
		}
	}
}