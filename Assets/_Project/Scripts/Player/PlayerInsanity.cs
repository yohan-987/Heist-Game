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
        [SerializeField, Min(0f)] private float _ratePerSecond = 0.25f;
        [SerializeField, Min(0f)] private float _doseRelief = 40f;
        [SerializeField, Range(0f, 100f)] private float _uneasyThreshold = 50f;
        [SerializeField, Range(0f, 100f)] private float _terrifiedThreshold = 75f;
        [SerializeField, Min(0.1f)] private float _swellSeconds = 5f;
        [SerializeField, Min(0f)] private float _publishStep = 0.5f;

        [Header("Dose gate")]
        [Tooltip("A dose is refused (and not taken from the pool) below this pressure.")]
        [SerializeField, Range(0f, 100f)] private float _minDosePressure = 20f;
        [SerializeField, Min(0f)] private float _doseCooldown = 10f;

        [Header("Pressure from events")]
        [SerializeField, Min(0f)] private float _pressureOnGrab = 8f;
        [SerializeField, Min(0f)] private float _pressureOnScare = 5f;

        private PlayerStats _stats;
        private IPlayerInputSource _input;
        private bool _burstHandled;
        private DoseGate _gate;
        private readonly MusicReliefMixer _music = new MusicReliefMixer();
        private float _sinceDose = 999f;
        private float _lastPublishedValue = -999f;
        private InsanityStage _lastPublishedStage = InsanityStage.Calm;

        public InsanityModel Model { get; private set; }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _input = GetComponent<IPlayerInputSource>();
            _gate = new DoseGate(_minDosePressure, _doseCooldown);
            Model = new InsanityModel(_ratePerSecond, _doseRelief, _uneasyThreshold, _terrifiedThreshold, _swellSeconds);
        }

        private void OnEnable()
        {
            var bus = Services.Events;
            bus.Subscribe<DoseGranted>(OnDoseGranted);
            bus.Subscribe<MusicPlaying>(OnMusic);
            bus.Subscribe<InsanityAdded>(OnInsanityAdded);
            bus.Subscribe<PlayerGrabbedByEnemy>(OnGrabbed);
            bus.Subscribe<Jumpscare>(OnScare);
        }

        private void OnDisable()
        {
            var bus = Services.Events;
            bus.Unsubscribe<DoseGranted>(OnDoseGranted);
            bus.Unsubscribe<MusicPlaying>(OnMusic);
            bus.Unsubscribe<InsanityAdded>(OnInsanityAdded);
            bus.Unsubscribe<PlayerGrabbedByEnemy>(OnGrabbed);
            bus.Unsubscribe<Jumpscare>(OnScare);
        }

        private void Update()
        {
            if (!_stats.IsAlive || Model.Burst) return;

            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            Model.Tick(dt, Services.Difficulty.InsanityRateMultiplier);
            _sinceDose += dt;

            // Music is continuous relief from the strongest source in earshot (radios never stack).
            float musicRate = _music.CurrentRate(Services.Clock.Now);
            if (musicRate > 0f) Model.Relieve(musicRate * dt);

            if (_input != null && _input.Current.TakeMedicinePressed) RequestDose();

            PublishIfChanged();

            if (Model.Burst && !_burstHandled)
            {
                _burstHandled = true;
                Services.Events.Publish(new PlayerBurst(_stats.PlayerId));
                // The burst kills outright: it skips hit protection and will skip the downed state in co-op.
                _stats.Kill();
            }
        }

        private void RequestDose()
        {
            var check = _gate.Check(Model.Value, Model.IsSwelling, _sinceDose);
            if (check != DoseCheck.Allowed)
            {
                var reason = check == DoseCheck.TooCalm ? DoseRefusal.TooCalm : DoseRefusal.Cooldown;
                Services.Events.Publish(new DoseRefused(_stats.PlayerId, reason));
                return;
            }

            Services.Events.Publish(new DoseRequested(_stats.PlayerId));
        }

        private void OnDoseGranted(DoseGranted evt)
        {
            if (evt.PlayerId != _stats.PlayerId) return;
            _sinceDose = 0f;
            Model.TakeDose();
            PublishIfChanged(force: true);
        }

        private void OnMusic(MusicPlaying evt)
        {
            float distance = Vector3.Distance(transform.position, evt.Position);
            // Closer to the radio means more relief; outside the radius means none.
            float strength = NoiseMath.Perceived(evt.Radius, distance);
            if (strength <= 0f) return;

            _music.Add(evt.SourceId, evt.ReliefPerSecond * strength, Services.Clock.Now + evt.Validity);
        }

        private void OnGrabbed(PlayerGrabbedByEnemy evt)
        {
            if (evt.PlayerId != _stats.PlayerId) return;
            AddPressure(_pressureOnGrab);
        }

        private void OnScare(Jumpscare evt)
        {
            // Scares are shown to the local player only, so only the local player pays for them.
            // The introductory scare is cosmetic and adds nothing (GAME_DESIGN 4.8).
            if (evt.Kind != ScareKind.Scripted || !evt.CostsPressure || !_stats.IsLocal) return;
            AddPressure(_pressureOnScare);
        }

        private void AddPressure(float amount)
        {
            Model.Add(amount * Services.Difficulty.EventPressureMultiplier);
            PublishIfChanged(force: true);
        }

        private void OnInsanityAdded(InsanityAdded evt)
        {
            if (evt.PlayerId != _stats.PlayerId) return;
            AddPressure(evt.Amount);
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
