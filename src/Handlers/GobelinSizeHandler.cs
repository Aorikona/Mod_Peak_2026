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
			if (Time.time - _lastCheckTime < 0.2f) return;
			_lastCheckTime = Time.time;

			if (_character == null) return;

			bool shouldBeSmall = false;

			if (_character.IsLocal)
			{
				shouldBeSmall = ClassManager.IsGobelinActive();
			}
			else
			{
				shouldBeSmall = ClassCheckUtil.IsGobelin(_character) || ClassModNetwork.GetPlayerClass(_character) == "Gobelin";
			}

			float currentScale = _character.transform.localScale.x;
			float targetScale = shouldBeSmall ? ClassBalance.GOBELIN_SIZE : 1f;

			if (Mathf.Abs(currentScale - targetScale) > 0.05f)
			{
				_character.transform.localScale = Vector3.one * targetScale;
			}
		}
	}
}