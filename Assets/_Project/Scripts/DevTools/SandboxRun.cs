using Game.Core;
using Game.Economy;
using UnityEngine;

namespace Game.DevTools
{
    /// <summary>
    /// For test scenes without the level generator: starts a run with a fixed quota so delivery,
    /// dosing and the progress events work. Put it on the same object as a <see cref="RunTracker"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RunTracker))]
    public sealed class SandboxRun : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _quotaDoses = 3;
        [SerializeField, Min(1f)] private float _valuePerDose = 500f;
        [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.Normal;
        [SerializeField] private bool _logProgress = true;

        private void OnEnable() => Services.Events.Subscribe<RunProgressChanged>(OnProgress);

        private void OnDisable() => Services.Events.Unsubscribe<RunProgressChanged>(OnProgress);

        // Difficulty is set in Awake, before other scripts read it in their own Awake (Apple builds its
        // rot model there). Starting the run waits until Start, when everything else exists.
        private void Awake() => Services.Difficulty = DifficultyProfile.For(_difficulty);

        private void Start() => GetComponent<RunTracker>().Begin(_quotaDoses, _valuePerDose);

        private void OnProgress(RunProgressChanged e)
        {
            if (!_logProgress) return;
            Debug.Log($"Run: delivered {e.DeliveredValue:0}, lost {e.LostValue:0}, banked doses {e.BankedDoses}, taken {e.ConsumedDoses}, quota {e.QuotaDoses}");
        }
    }
}
