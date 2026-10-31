using UnityEngine;

namespace Game.DevTools
{
    /// <summary>
    /// On-screen performance readout: FPS, physics body counts, and enemy/valuable counts.
    /// Compiled out of release builds. Toggle with the Enabled property or the inspector checkbox.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool _visible = true;
        [SerializeField, Min(0.1f)] private float _refreshSeconds = 0.5f;

        private float _timer, _fps, _frameTimeMs;
        private int _frames, _bodies, _awake;
        private float _accumulated;
        private GUIStyle _style;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        private void Update()
        {
            _frames++;
            _accumulated += Time.unscaledDeltaTime;
            _timer += Time.unscaledDeltaTime;
            if (_timer < _refreshSeconds) return;

            _fps = _frames / _accumulated;
            _frameTimeMs = 1000f / Mathf.Max(0.01f, _fps);
            _frames = 0;
            _accumulated = 0f;
            _timer = 0f;

            var bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            _bodies = bodies.Length;
            _awake = 0;
            foreach (var b in bodies)
                if (!b.IsSleeping()) _awake++;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
            }

            GUI.Label(new Rect(10, 10, 360, 70),
                $"{_fps:0} FPS ({_frameTimeMs:0.0} ms)\nRigidbodies: {_bodies} (awake {_awake})", _style);
        }
#endif
    }
}
