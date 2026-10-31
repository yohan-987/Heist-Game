using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Drives one player's <see cref="InsanityModel"/>: ticks it, turns the "take medicine" key into a
    /// dose request, reacts to granted doses, music and apple throws, and bursts the player at the end.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerInsanity : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _ratePerSecond = 0.4f;
        [SerializeField, Min(0f)] private float _doseRelief = 40f;
        [SerializeField, Range(0f, 100f)] private float _uneasyThreshold = 50f;
        [SerializeField, Range(0f, 100f)] private float _terrifiedThreshold = 75f;
        [SerializeField, Min(0.1f)] private float _swellSeconds = 3f;
        [SerializeField, Min(0f)] private float _publishStep = 0.5f;

        private PlayerStats _stats;
        private IPlayerInputSource _input;
        private bool _burstHandled;
        private float _lastPublishedValue = -999f;
        private InsanityStage _lastPublishedStage = InsanityStage.Calm;

        public InsanityModel Model { get; private set; }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _input = GetComponent<IPlayerInputSource>();
            Model = new InsanityModel(_ratePerSecond, _doseRelief, _uneasyThreshold, _terrifiedThreshold, _swellSeconds);
        }

        private void OnEnable()
        {
            var bus = Services.Events;
            bus.Subscribe<DoseGranted>(OnDoseGranted);
            bus.Subscribe<MusicPlaying>(OnMusic);
            bus.Subscribe<InsanityAdded>(OnInsanityAdded);
        }

        private void OnDisable()
        {
            var bus = Services.Events;
            bus.Unsubscribe<DoseGranted>(OnDoseGranted);
            bus.Unsubscribe<MusicPlaying>(OnMusic);
            bus.Unsubscribe<InsanityAdded>(OnInsanityAdded);
        }

        private void Update()
        {
            if (!_stats.IsAlive || Model.Burst) return;

            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            Model.Tick(dt, Services.Difficulty.InsanityRateMultiplier);

            if (_input != null && _input.Current.TakeMedicinePressed)
                Services.Events.Publish(new DoseRequested(_stats.PlayerId));

            PublishIfChanged();

            if (Model.Burst && !_burstHandled)
            {
                _burstHandled = true;
                Services.Events.Publish(new PlayerBurst(_stats.PlayerId));
                // Killing through the normal damage path keeps death handling in one place.
                _stats.ApplyDamage(new DamageInfo(_stats.Health.Current, transform.position, Vector3.zero, this));
            }
        }

        private void OnDoseGranted(DoseGranted evt)
        {
            if (evt.PlayerId != _stats.PlayerId) return;
            Model.TakeDose();
            PublishIfChanged(force: true);
        }

        private void OnMusic(MusicPlaying evt)
        {
            float distance = Vector3.Distance(transform.position, evt.Position);
            // Closer to the radio means more relief; outside the radius means none.
            float strength = NoiseMath.Perceived(evt.Radius, distance);
            if (strength <= 0f) return;
            Model.Relieve(evt.Relief * strength);
        }

        private void OnInsanityAdded(InsanityAdded evt)
        {
            if (evt.PlayerId != _stats.PlayerId) return;
            Model.Add(evt.Amount);
            PublishIfChanged(force: true);
        }

        private void PublishIfChanged(bool force = false)
        {
            bool stageChanged = Model.Stage != _lastPublishedStage;
            bool moved = Mathf.Abs(Model.Value - _lastPublishedValue) >= _publishStep;
            if (!force && !stageChanged && !moved && !Model.IsSwelling) return;

            _lastPublishedValue = Model.Value;
            _lastPublishedStage = Model.Stage;
            Services.Events.Publish(new InsanityChanged(_stats.PlayerId, Model.Value, Model.Stage, Model.SwellProgress));
        }
    }
}
