using UnityEngine;

namespace PeakClassMod.Handlers
{
    public class DualNatureAuraVisual : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _radius = ClassBalance.DUALNATURE_RADIUS; // Large circle representing the aura radius
        private int _segments = 64; // More segments for a smoother large circle
        private Material _material;

        private void Awake()
        {
            // Create the child object for the line renderer to ensure it doesn't mess with character scaling
            GameObject ringObj = new GameObject("DualNatureAuraRing");
            ringObj.transform.SetParent(transform);
            ringObj.transform.localPosition = new Vector3(0, 0.1f, 0); // Slightly above ground
            ringObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Flat on ground

            _lineRenderer = ringObj.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = _segments + 1;
            _lineRenderer.useWorldSpace = false;

            // Set basic visual properties for a noticeable aura radius
            _lineRenderer.startWidth = 0.25f;
            _lineRenderer.endWidth = 0.25f;

            // Attempt to find a standard Unity diffuse material or unlit to draw solid color
            _material = new Material(Shader.Find("Hidden/Internal-Colored"));
            _material.hideFlags = HideFlags.HideAndDontSave;

            // Turn on alpha blending
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _material.SetInt("_ZWrite", 0);

            _lineRenderer.material = _material;

            DrawCircle();

            // Initialize with proper state
            var handler = GetComponentInParent<DualNatureHandler>();
            if (handler != null)
            {
                SetState(handler.IsDayTime());
            }
            DualNatureHandler.OnDualNatureStateChanged += HandleStateChange;
        }

        private void DrawCircle()
        {
            float angle = 0f;
            for (int i = 0; i < (_segments + 1); i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * _radius;
                float y = Mathf.Cos(Mathf.Deg2Rad * angle) * _radius;

                _lineRenderer.SetPosition(i, new Vector3(x, y, 0));
                angle += (360f / _segments);
            }
        }

        private void HandleStateChange(bool isDay, Character character)
        {
            // Update color if this is our character changing states
            if (character != null && (character.gameObject == this.gameObject || character.gameObject == transform.parent?.gameObject))
            {
                SetState(isDay);
            }
        }

        public void SetState(bool isDay)
        {
            // Set Color based on state: Green for Healing (Day), Red for Damaging (Night)
            Color targetColor = isDay ? new Color(0f, 1f, 0.2f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            _lineRenderer.startColor = targetColor;
            _lineRenderer.endColor = targetColor;

            if (_material != null)
            {
                _material.color = targetColor;
            }

            _lineRenderer.enabled = true; // Permanently visible
        }

        private void OnDestroy()
        {
            DualNatureHandler.OnDualNatureStateChanged -= HandleStateChange;
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}