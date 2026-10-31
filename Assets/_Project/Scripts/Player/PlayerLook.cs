using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Yaw rotates the body, pitch rotates the head (camera holder).</summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField, Range(10f, 89f)] private float _pitchLimit = 85f;

        private IPlayerInputSource _input;
        private float _pitch;

        private void Awake()
        {
            _input = GetComponent<IPlayerInputSource>();
            if (_head == null) Debug.LogError("PlayerLook needs a head transform assigned.", this);
        }

        private void Update()
        {
            if (_input == null || _head == null || Services.Clock.Paused) return;

            var look = _input.Current.Look;
            transform.Rotate(0f, look.x, 0f, Space.Self);

            _pitch = Mathf.Clamp(_pitch - look.y, -_pitchLimit, _pitchLimit);
            _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
