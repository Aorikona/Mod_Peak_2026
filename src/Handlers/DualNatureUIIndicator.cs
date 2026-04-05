using UnityEngine;
using UnityEngine.UI;

namespace PeakClassMod.Handlers
{
    public class DualNatureUIIndicator : MonoBehaviour
    {
        private GameObject _uiCanvas;
        private Text _statusText;

        private void Start()
        {
            CreateUI();
            DualNatureHandler.OnDualNatureStateChanged += HandleStateChange;

            // Initialize with current state if possible
            var handler = GetComponent<DualNatureHandler>();
            if (handler != null)
            {
                UpdateUI(handler.IsDayTime());
            }
        }

        private void CreateUI()
        {
            _uiCanvas = new GameObject("DualNatureUIPanel");
            var canvas = _uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure it renders on top

            _uiCanvas.AddComponent<CanvasScaler>();
            _uiCanvas.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(_uiCanvas.transform, false);

            _statusText = textObj.AddComponent<Text>();

            // Attempt to find a built-in font
            _statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _statusText.alignment = TextAnchor.UpperCenter;
            _statusText.fontSize = 24;
            _statusText.text = "Day Form";
            _statusText.color = Color.green;

            // Position at top center
            RectTransform rectTransform = _statusText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -20);
            rectTransform.sizeDelta = new Vector2(300, 50);
        }

        private void HandleStateChange(bool isDay, Character character)
        {
            // Only update UI if this event belongs to the local player
            if (character != null && character.gameObject == this.gameObject)
            {
                UpdateUI(isDay);
            }
        }

        private void UpdateUI(bool isDay)
        {
            if (_statusText == null) return;

            if (isDay)
            {
                _statusText.text = "Day Form - Wolf";
                _statusText.color = Color.green;
            }
            else
            {
                _statusText.text = "Night Form - Wolf";
                _statusText.color = Color.red;
            }
        }

        private void OnDestroy()
        {
            DualNatureHandler.OnDualNatureStateChanged -= HandleStateChange;
            if (_uiCanvas != null)
            {
                Destroy(_uiCanvas);
            }
        }
    }
}