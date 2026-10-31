using Game.Core;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// A radio. While playing it calms nearby players and makes noise that doctors ignore but
    /// nuisance enemies (spiders, ground doctors) hear. Volume scales both the calming and the risk.
    /// Hold it and press the use key to toggle.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour, IUsable
    {
        [SerializeField, Min(1f)] private float _loudness = 16f;
        [SerializeField, Min(0f)] private float _reliefPerSecond = 3f;
        [SerializeField, Min(0.1f)] private float _tickSeconds = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _volume = 1f;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private bool _playing;

        private float _timer;

        public bool IsPlaying => _playing;
        public float Volume => _volume;

        /// <summary>How far the music carries: both the calming range and the distance enemies hear it.</summary>
        public float Radius => _loudness * _volume;

        private void Start() => ApplyAudio();

        public void Use(int playerId) => Toggle();

        public void Toggle() => SetPlaying(!_playing);

        public void SetPlaying(bool playing)
        {
            _playing = playing;
            _timer = 0f;
            ApplyAudio();
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp(volume, 0.1f, 1f);
            ApplyAudio();
        }

        private void Update()
        {
            if (!_playing) return;

            _timer += Services.Clock.DeltaTime;
            if (_timer < _tickSeconds) return;
            _timer -= _tickSeconds;

            Vector3 position = transform.position;
            float radius = Radius;

            Services.Events.Publish(new MusicPlaying(position, radius, _reliefPerSecond * _tickSeconds * _volume));
            Services.Events.Publish(new NoiseEmitted(position, radius, NoiseSource.Music));
        }

        private void ApplyAudio()
        {
            if (_audio == null) return;
            _audio.volume = _volume;
            if (_playing && !_audio.isPlaying) _audio.Play();
            else if (!_playing && _audio.isPlaying) _audio.Stop();
        }
    }
}
