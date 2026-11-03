using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// The way out. Once the quota has unlocked the exit, the run is finished as a success when every
    /// living player is standing inside this zone. Put it on a trigger box at the front door.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExitZone : MonoBehaviour
    {
        [Tooltip("Optional. Found automatically if left empty.")]
        [SerializeField] private RunTracker _tracker;
        [SerializeField, Min(0.1f)] private float _checkInterval = 0.25f;

        private readonly HashSet<IPlayerAvatar> _inside = new HashSet<IPlayerAvatar>();
        private float _timer;

        private void Awake() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnDisable() => _inside.Clear();

        private void OnTriggerEnter(Collider other)
        {
            var avatar = other.GetComponentInParent<IPlayerAvatar>();
            if (avatar != null) _inside.Add(avatar);
        }

        private void OnTriggerExit(Collider other)
        {
            var avatar = other.GetComponentInParent<IPlayerAvatar>();
            if (avatar != null) _inside.Remove(avatar);
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer < _checkInterval) return;
            _timer = 0f;

            if (_tracker == null) _tracker = FindFirstObjectByType<RunTracker>();
            if (_tracker == null || !_tracker.Active || !_tracker.Medicine.ExitUnlocked) return;

            if (AllLivingInside()) _tracker.Finish();
        }

        private bool AllLivingInside()
        {
            bool anyAlive = false;
            var players = Services.Players.All;
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].IsAlive) continue;
                anyAlive = true;
                if (!_inside.Contains(players[i])) return false;
            }
            return anyAlive;
        }
    }
}
