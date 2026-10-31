using Game.Core;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Listens to gameplay events, feeds the <see cref="RunLedger"/>, and announces progress.
    /// Ends the run when asked to (<see cref="Finish"/>) or when every player is dead.
    /// </summary>
    public sealed class RunTracker : MonoBehaviour
    {
        private RunLedger _ledger = new RunLedger(0f);
        private bool _active;

        public RunLedger Ledger => _ledger;
        public bool Active => _active;

        private void OnEnable()
        {
            var bus = Services.Events;
            bus.Subscribe<ExtractionValueChanged>(OnExtraction);
            bus.Subscribe<ValuableDamaged>(OnDamaged);
            bus.Subscribe<PlayerDied>(OnPlayerDied);
        }

        private void OnDisable()
        {
            var bus = Services.Events;
            bus.Unsubscribe<ExtractionValueChanged>(OnExtraction);
            bus.Unsubscribe<ValuableDamaged>(OnDamaged);
            bus.Unsubscribe<PlayerDied>(OnPlayerDied);
        }

        public void Begin(float quota)
        {
            _ledger.Reset(quota);
            _active = true;
            Publish();
        }

        /// <summary>Ends the run. Succeeds only if the quota is met.</summary>
        public void Finish()
        {
            if (!_active) return;
            _active = false;
            Services.Events.Publish(new LevelCompleted(_ledger.QuotaMet, _ledger.Delivered, _ledger.Quota));
        }

        private void OnExtraction(ExtractionValueChanged evt)
        {
            if (!_active) return;
            _ledger.SetDelivered(evt.Total);
            Publish();
        }

        private void OnDamaged(ValuableDamaged evt)
        {
            if (!_active) return;
            _ledger.AddLost(evt.Lost);
            Publish();
        }

        private void OnPlayerDied(PlayerDied evt)
        {
            if (!_active) return;
            if (!Services.Players.AnyAlive()) Finish();
        }

        private void Publish() =>
            Services.Events.Publish(new RunProgressChanged(_ledger.Delivered, _ledger.Quota, _ledger.Lost));
    }
}
