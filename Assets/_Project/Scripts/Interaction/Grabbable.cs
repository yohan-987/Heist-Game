using Game.Core;
using UnityEngine;

namespace Game.Interaction
{
    public interface IGrabbable
    {
        bool CanBeGrabbed { get; }
        bool IsHeld { get; }
        Rigidbody Body { get; }
        float Mass { get; }
        bool RequestGrab(PlayerGrabber grabber);
        void Release(PlayerGrabber grabber);
    }

    /// <summary>
    /// Put on a Rigidbody the player can pick up. While held the object switches to the Held layer
    /// (so it doesn't shove the holder) and uses continuous collision so it can't tunnel.
    /// All state changes go through <see cref="RequestGrab"/> / <see cref="Release"/> so they can
    /// become network requests later.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Grabbable : MonoBehaviour, IGrabbable
    {
        [SerializeField] private bool _canBeGrabbed = true;
        [SerializeField, Min(0f)] private float _heldDrag = 4f;
        [SerializeField, Min(0f)] private float _heldAngularDrag = 6f;

        private Rigidbody _body;
        private EntityIdentity _identity;
        private Collider[] _colliders;
        private int[] _originalLayers;
        private CollisionDetectionMode _originalDetection;
        private float _originalDrag, _originalAngularDrag;

        public PlayerGrabber Holder { get; private set; }

        public bool CanBeGrabbed => _canBeGrabbed && Holder == null;
        public bool IsHeld => Holder != null;
        public Rigidbody Body => _body;
        public float Mass => _body != null ? _body.mass : 0f;
        public EntityId Id
        {
            get
            {
                // Resolve lazily: the Spawner may add the identity after Awake, and objects placed by
                // hand in a scene have none until something asks for it.
                if (_identity == null) _identity = GetComponent<EntityIdentity>();
                if (_identity == null || !_identity.Id.IsValid)
                {
                    Services.Spawner.EnsureIdentity(gameObject);
                    _identity = GetComponent<EntityIdentity>();
                }
                return _identity.Id;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _identity = GetComponent<EntityIdentity>();
            _colliders = GetComponentsInChildren<Collider>();
            _originalLayers = new int[_colliders.Length];
        }

        public void SetCanBeGrabbed(bool value) => _canBeGrabbed = value;

        public bool RequestGrab(PlayerGrabber grabber)
        {
            if (grabber == null || !CanBeGrabbed) return false;

            Holder = grabber;

            for (int i = 0; i < _colliders.Length; i++)
            {
                _originalLayers[i] = _colliders[i].gameObject.layer;
                _colliders[i].gameObject.layer = GameLayers.Held;
            }

            _originalDetection = _body.collisionDetectionMode;
            _originalDrag = _body.linearDamping;
            _originalAngularDrag = _body.angularDamping;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.linearDamping = _heldDrag;
            _body.angularDamping = _heldAngularDrag;
            _body.WakeUp();

            Services.Events.Publish(new ObjectGrabbed(Id, grabber.PlayerId));
            return true;
        }

        public void Release(PlayerGrabber grabber)
        {
            if (Holder == null || Holder != grabber) return;

            int playerId = Holder.PlayerId;
            Holder = null;

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].gameObject.layer = _originalLayers[i];
            }

            _body.collisionDetectionMode = _originalDetection;
            _body.linearDamping = _originalDrag;
            _body.angularDamping = _originalAngularDrag;

            Services.Events.Publish(new ObjectReleased(Id, playerId));
        }

        private void OnDisable()
        {
            if (Holder != null) Holder.ForceRelease();
        }
    }
}
