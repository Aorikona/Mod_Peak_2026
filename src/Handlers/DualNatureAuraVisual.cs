using UnityEngine;

namespace PeakClassMod.Handlers
{
    public class DualNatureAuraVisual : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _timeRemaining = 0f;
        private float _radius = 1.5f; // Small circle under the affected character
        private int _segments = 36;
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

            // Set basic visual properties
            _lineRenderer.startWidth = 0.15f;
            _lineRenderer.endWidth = 0.15f;

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
            _lineRenderer.enabled = false; // Hidden by default
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

        public void Ping(bool isDay)
        {
            // Reset the timer, duration is slightly longer than the tick rate to keep it continuous if staying in radius
            _timeRemaining = ClassBalance.DUALNATURE_AURA_TICK_RATE + 0.5f;

            // Set Color based on state
            Color targetColor = isDay ? new Color(0f, 1f, 0.2f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
            _lineRenderer.startColor = targetColor;
            _lineRenderer.endColor = targetColor;

            if (_material != null)
            {
                _material.color = targetColor;
            }

            _lineRenderer.enabled = true;
        }

        private void Update()
        {
            if (_timeRemaining > 0)
            {
                _timeRemaining -= Time.deltaTime;

                // Fade out effect
                if (_timeRemaining < 1f && _lineRenderer.enabled)
                {
                    float alpha = _timeRemaining * 0.6f;
                    Color c = _lineRenderer.startColor;
                    c.a = alpha;
                    _lineRenderer.startColor = c;
                    _lineRenderer.endColor = c;

                    if (_material != null) _material.color = c;
                }

                if (_timeRemaining <= 0)
                {
                    _lineRenderer.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}