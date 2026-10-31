using UnityEngine;

namespace Game.Core
{
    /// <summary>Put on anything that can make noise. Publishes through the event bus.</summary>
    public sealed class NoiseEmitter : MonoBehaviour
    {
        [SerializeField] private NoiseSource _defaultSource = NoiseSource.Other;
        [SerializeField, Min(0f)] private float _defaultLoudness = 8f;
        [SerializeField] private bool _drawGizmo;

        private float _lastLoudness;

        public void Emit() => Emit(_defaultLoudness, _defaultSource);

        public void Emit(float loudness) => Emit(loudness, _defaultSource);

        public void Emit(float loudness, NoiseSource source)
        {
            if (loudness <= 0f) return;
            _lastLoudness = loudness;
            Services.Events.Publish(new NoiseEmitted(transform.position, loudness, source));
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmo) return;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _lastLoudness > 0f ? _lastLoudness : _defaultLoudness);
        }
    }
}
