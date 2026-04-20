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
            HasReceivedStartItems.Add(actorNumber);
            foreach (var itemName in itemsToGive)
            {
                var item = ItemCache.Get(itemName);
                if (item != null && targetChar.player.HasEmptySlot((ushort)item.itemID)) targetChar.player.AddItem((ushort)item.itemID, null, out _);
                yield return new WaitForSeconds(0.02f);
            }
        }

        [PunRPC]
        public void RPC_ApplyWolfHeal(float amount)
        {
            if (_character == null) _character = GetComponent<Character>();
            var afflictions = _character?.refs?.afflictions;
            if (afflictions == null) return;
            afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, amount, false);
        }

        [PunRPC]
        public void RPC_ApplyWolfDamage(float amount)
        {
            if (_character == null) _character = GetComponent<Character>();
            var afflictions = _character?.refs?.afflictions;
            if (afflictions == null) return;
            afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, amount, false, true, true);
        }

        [PunRPC] public void ShowMedicHealEffectRPC(int remainingHeals) { PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Medic heal: {remainingHeals}"); }
        [PunRPC] public void RPC_SyncClassActivation(string className, int actorNumber) { PeakClassModPlugin.Log?.LogInfo($"[ClassMod] Player {actorNumber} class: {className}"); }
    }
}
