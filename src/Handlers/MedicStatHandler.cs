using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PeakClassMod.Handlers
{
    public class MedicStatHandler : MonoBehaviour
    {
        private Character _character; private ClassModNetwork _network; private CharacterAfflictions _afflictions; private int _healsRemaining = ClassBalance.MEDIC_MAX_HEALS; private float _lastHealTime = 0f;
        private void Start() { 
            _character = GetComponent<Character>(); 
            _network = GetComponent<ClassModNetwork>(); 
            if (_character?.refs != null) 
                _afflictions = _character.refs.afflictions; 
        }
        private void Update() { 
            if (_character == null || !_character.IsLocal) return; 
            if (ClassManager.IsMedicActive()) { 
                if (Input.GetKeyDown(KeyCode.H) && Time.time - _lastHealTime > ClassBalance.MEDIC_HEAL_COOLDOWN) TrySelfHeal(); 
            } else if (_healsRemaining < ClassBalance.MEDIC_MAX_HEALS) 
                _healsRemaining = ClassBalance.MEDIC_MAX_HEALS; 
        }
        private void TrySelfHeal() { 
            if (_healsRemaining <= 0) return; 
            if (_afflictions == null && _character?.refs != null) 
                _afflictions = _character.refs.afflictions; 
            if (_afflictions != null && _afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) > 0f) { 
                _healsRemaining--; 
                _lastHealTime = Time.time; 
                _afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury, ClassBalance.MEDIC_HEAL_AMOUNT, false); 
                if (_network?.photonView != null) _network.photonView.RPC("ShowMedicHealEffectRPC", Photon.Pun.RpcTarget.All, _healsRemaining); 
            } 
        }
    }
}