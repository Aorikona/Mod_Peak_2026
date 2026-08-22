using UnityEngine;
using PeakClassMod.Utils;
using Photon.Pun;

namespace PeakClassMod.Handlers
{
	public class RogueMovementHandler : MonoBehaviour
	{
		private Character _character;

		private void Start()
		{
			_character = GetComponent<Character>();
		}

		private void Update()
		{
			if (_character == null || !_character.IsLocal) return;
			if (!ClassManager.IsRogueActive() && !ClassCheckUtil.IsRogueHat(_character)) return;

			// 1. Maintient les compteurs de sauts supplementaires
			if (_character.data != null)
			{
				_character.data.extraJumps = 99;
			}
			if (_character.refs?.afflictions != null)
			{
				_character.refs.afflictions.totalExtraJumps = 99;
			}

			// 2. Detection du saut aerien
			if (_character.input != null && _character.input.jumpWasPressed)
			{
				if (_character.data != null && _character.data.sinceGrounded > 0.12f && _character.data.sinceJump > 0.1f)
				{
					if (_character.data.currentStamina > 0.005f && !_character.data.passedOut && !_character.data.fullyPassedOut && !_character.data.isClimbing)
					{
						_character.data.chargingJump = false;
						_character.data.extraJumps = 99;
						_character.refs?.view?.RPC("JumpRpc", RpcTarget.All, new object[] { false });
					}
				}
			}
		}
	}
}
