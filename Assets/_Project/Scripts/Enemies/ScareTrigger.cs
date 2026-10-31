using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>A trigger volume that asks the director to fire one scripted scare when the local player enters.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ScareTrigger : MonoBehaviour
    {
        [SerializeField] private int _scareId;
        [SerializeField] private JumpscareDirector _director;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_director == null) return;

            var avatar = other.GetComponentInParent<IPlayerAvatar>();
            if (avatar == null || !avatar.IsLocal) return;

            // If the gap hasn't elapsed the scare is skipped, and re-entering the volume tries again.
            _director.TriggerScripted(_scareId);
        }
    }
}
