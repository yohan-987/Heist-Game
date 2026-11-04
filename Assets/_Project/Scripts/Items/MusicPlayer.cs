using Game.Core;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// A radio. While playing it calms nearby players and makes noise that doctors ignore but
    /// nuisance enemies (spiders, ground doctors) hear. Volume scales both the calming and the risk.
    /// It runs on a battery that does not recharge, and only the strongest radio in earshot counts.
    /// Hold it and press the use key to toggle.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour, IUsable
    {
        [SerializeField, Min(1f)] private float _loudness = 16f;
        [Tooltip("Pressure relief per second at point-blank range and full volume. The passive rise is 0.25, so keep this well below a dose's worth.")]
        [SerializeField, Min(0f)] private float _reliefPerSecond = 0.6f;
        [Tooltip("Seconds of playback in one battery.")]
        [SerializeField, Min(1f)] private float _batterySeconds = 40f;
        [SerializeField, Min(0.1f)] private float _tickSeconds = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _volume = 1f;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private bool _playing;

        private float _timer;
        private bool _wasPaused;
        private BatteryModel _battery;
        private Game.Interaction.IGrabbable _grabbable;

        public bool IsPlaying => _playing;
        public float Volume => _volume;
        public float BatteryFraction => Battery.Fraction;

        private BatteryModel Battery => _battery ?? (_battery = new BatteryModel(_batterySeconds));

        /// <summary>How far the music carries: both the calming range and the distance enemies hear it.</summary>
        public float Radius => _loudness * _volume;

        private void Awake() => _grabbable = GetComponent<Game.Interaction.IGrabbable>();

        private void Start() => ApplyAudio();

        /// <summary>A radio that is carried is paused: calm has to cost standing still (GAME_DESIGN 4.5).</summary>
        public bool IsPaused => _grabbable != null && _grabbable.IsHeld;

        public void Use(int playerId) => Toggle();

        public void Toggle() => SetPlaying(!_playing);

        public void SetPlaying(bool playing)
        {
            if (playing && Battery.IsEmpty) playing = false;

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

            bool paused = IsPaused;
            if (paused != _wasPaused)
            {
                _wasPaused = paused;
                _timer = 0f;
                ApplyAudio();
            }
            if (paused) return;

            float dt = Services.Clock.DeltaTime;
            Battery.Drain(dt);
            if (Battery.IsEmpty)
            {
                SetPlaying(false);
                return;
            }

            _timer += dt;
            if (_timer < _tickSeconds) return;
            _timer -= _tickSeconds;

            Vector3 position = transform.position;
            float radius = Radius;

            Services.Events.Publish(new MusicPlaying(GetInstanceID(), position, radius, _reliefPerSecond * _volume, _tickSeconds * 1.5f));
            Services.Events.Publish(new NoiseEmitted(position, radius, NoiseSource.Music));
        }

        private void ApplyAudio()
        {
            if (_audio == null) return;
            _audio.volume = _volume;
            bool shouldPlay = _playing && !IsPaused;
            if (shouldPlay && !_audio.isPlaying) _audio.Play();
            else if (!shouldPlay && _audio.isPlaying) _audio.Stop();
        }
    }
}
