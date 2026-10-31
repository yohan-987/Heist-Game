using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Turns the Input System into a <see cref="PlayerInputState"/>. Actions are built in code so
    /// the project works before an InputActions asset exists; swap for an asset when rebinding is added.
    /// Requires the Input System package (included in the 3D URP template).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        [SerializeField] private bool _lockCursorOnEnable = true;
        [SerializeField, Min(0f)] private float _mouseSensitivity = 0.1f;     // degrees per pixel
        [SerializeField, Min(0f)] private float _stickSensitivity = 140f;     // degrees per second at full tilt

        private InputAction _move, _lookMouse, _lookStick, _sprint, _crouch, _jump, _grab, _throw, _rotate, _scroll;
        private InputAction _medicine, _use, _swat;

        /// <summary>Set false while a menu is open so the character doesn't move behind it.</summary>
        public bool Enabled { get; set; } = true;

        public PlayerInputState Current
        {
            get
            {
                if (!Enabled || _move == null) return PlayerInputState.None;

                var look = _lookMouse.ReadValue<Vector2>() * _mouseSensitivity
                           + _lookStick.ReadValue<Vector2>() * (_stickSensitivity * Time.deltaTime);

                return new PlayerInputState
                {
                    Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f),
                    Look = look,
                    Sprint = _sprint.IsPressed(),
                    Crouch = _crouch.IsPressed(),
                    JumpPressed = _jump.WasPressedThisFrame(),
                    GrabHeld = _grab.IsPressed(),
                    GrabPressed = _grab.WasPressedThisFrame(),
                    ThrowPressed = _throw.WasPressedThisFrame(),
                    RotateHeld = _rotate.IsPressed(),
                    TakeMedicinePressed = _medicine.WasPressedThisFrame(),
                    UsePressed = _use.WasPressedThisFrame(),
                    SwatPressed = _swat.WasPressedThisFrame(),
                    Scroll = Mathf.Clamp(_scroll.ReadValue<float>() / 120f, -3f, 3f),
                };
            }
        }

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _lookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            _lookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            _sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");

            _crouch = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            _crouch.AddBinding("<Gamepad>/buttonEast");

            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _grab = new InputAction("Grab", InputActionType.Button, "<Mouse>/leftButton");
            _grab.AddBinding("<Gamepad>/rightTrigger");

            _throw = new InputAction("Throw", InputActionType.Button, "<Mouse>/rightButton");
            _throw.AddBinding("<Gamepad>/buttonWest");

            _rotate = new InputAction("Rotate", InputActionType.Button, "<Keyboard>/r");
            _rotate.AddBinding("<Gamepad>/leftShoulder");

            _scroll = new InputAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");

            _medicine = new InputAction("TakeMedicine", InputActionType.Button, "<Keyboard>/q");
            _medicine.AddBinding("<Gamepad>/dpad/up");

            _use = new InputAction("Use", InputActionType.Button, "<Keyboard>/e");
            _use.AddBinding("<Gamepad>/buttonNorth");

            _swat = new InputAction("Swat", InputActionType.Button, "<Keyboard>/f");
            _swat.AddBinding("<Gamepad>/rightShoulder");
        }

        private void OnEnable()
        {
            foreach (var a in AllActions()) a.Enable();
            if (_lockCursorOnEnable) SetCursorLocked(true);
        }

        private void OnDisable()
        {
            foreach (var a in AllActions()) a.Disable();
        }

        private void OnDestroy()
        {
            foreach (var a in AllActions()) a?.Dispose();
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private InputAction[] AllActions() => new[]
        {
            _move, _lookMouse, _lookStick, _sprint, _crouch, _jump, _grab, _throw, _rotate, _scroll,
            _medicine, _use, _swat
        };
    }
}
