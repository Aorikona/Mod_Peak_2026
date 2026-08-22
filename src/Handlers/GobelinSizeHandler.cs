using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Handlers
{
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
			if (_character == null) return;

			if (Time.time - _lastCheckTime < 0.2f) return;
			_lastCheckTime = Time.time;

			bool shouldBeSmall = false;

			if (_character.IsLocal)
			{
				shouldBeSmall = ClassManager.IsGobelinActive() || ClassCheckUtil.IsGobelin(_character);
			}
			else
			{
				shouldBeSmall = ClassCheckUtil.IsGobelin(_character) || ClassModNetwork.GetPlayerClass(_character) == "Gobelin";
			}

			float targetScale = shouldBeSmall ? ClassBalance.GOBELIN_SIZE : 1f;

			if (Mathf.Abs(_character.transform.localScale.x - targetScale) > 0.02f)
			{
				_character.transform.localScale = Vector3.one * targetScale;

				// Empeche de passer sous le sol lors du rétrécissement
				if (_character.IsLocal && shouldBeSmall)
				{
					_character.transform.position += Vector3.up * 0.35f;
				}
			}
		}
	}
}