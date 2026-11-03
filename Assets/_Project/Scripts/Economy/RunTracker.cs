using Game.Core;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Listens to gameplay events, feeds the value ledger and the medicine ledger, answers dose
    /// requests, and announces progress. Ends the run when asked to (<see cref="Finish"/>) or when
    /// every player is dead.
    /// </summary>
    public sealed class RunTracker : MonoBehaviour
    {
        private readonly RunLedger _value = new RunLedger(0f);
        private readonly MedicineLedger _medicine = new MedicineLedger(0, 500f);
        private bool _active;

        public RunLedger Value => _value;
        public MedicineLedger Medicine => _medicine;
        public bool Active => _active;

        private void OnEnable()
        {
            var bus = Services.Events;
            bus.Subscribe<ValuableDelivered>(OnDelivered);
            bus.Subscribe<ValuableDamaged>(OnDamaged);
            bus.Subscribe<PlayerDied>(OnPlayerDied);
            bus.Subscribe<DoseRequested>(OnDoseRequested);
        }

        private void OnDisable()
        {
            var bus = Services.Events;
            bus.Unsubscribe<ValuableDelivered>(OnDelivered);
            bus.Unsubscribe<ValuableDamaged>(OnDamaged);
            bus.Unsubscribe<PlayerDied>(OnPlayerDied);
            bus.Unsubscribe<DoseRequested>(OnDoseRequested);
        }

        /// <summary>Start a run. <paramref name="valuePerDose"/> is the base recipe; difficulty scales it.</summary>
        public void Begin(int quotaDoses, float valuePerDose)
        {
            float scaled = valuePerDose * Services.Difficulty.ValuePerDoseMultiplier;
            _value.Reset(0f);
            _medicine.Reset(quotaDoses, scaled);
            _active = true;
            Publish();
        }

        /// <summary>Ends the run. Succeeds only if the exit was unlocked (the quota was reached).</summary>
        public void Finish()
        {
            if (!_active) return;
            _active = false;
            Services.Events.Publish(new LevelCompleted(_medicine.ExitUnlocked, _medicine.Banked, _medicine.QuotaDoses));
        }

        private void OnDelivered(ValuableDelivered evt)
        {
            if (!_active) return;

            bool wasUnlocked = _medicine.ExitUnlocked;
            _medicine.Commit(evt.Value);
            _value.SetDelivered(_medicine.CommittedValue);
            Publish();

            if (!wasUnlocked && _medicine.ExitUnlocked)
                Services.Events.Publish(new ExitUnlocked(_medicine.Banked, _medicine.QuotaDoses));
        }

        private void OnDamaged(ValuableDamaged evt)
        {
            if (!_active) return;
            _value.AddLost(evt.Lost);
            Publish();
        }

        private void OnDoseRequested(DoseRequested evt)
        {
            if (!_active) return;

            if (_medicine.TryConsume())
            {
                Services.Events.Publish(new DoseGranted(evt.PlayerId));
                Publish();
            }
            else
            {
                Services.Events.Publish(new DoseDenied(evt.PlayerId));
            }
        }

        private void OnPlayerDied(PlayerDied evt)
        {
            if (!_active) return;
            if (!Services.Players.AnyAlive()) Finish();
        }

        private void Publish() =>
            Services.Events.Publish(new RunProgressChanged(
                _value.Delivered, _value.Lost, _medicine.Banked, _medicine.Consumed, _medicine.QuotaDoses));
    }
}
