using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArmorTheVehicle.Car
{
    /// Thin wrapper over the project's InputSystem_Actions "Gameplay" map — the only
    /// place that talks to the Input System directly. Looks actions up by name from a
    /// single assigned InputActionAsset, so wiring this in the Inspector is one reference.
    public sealed class CarInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _actionMapName = "Gameplay";
        [SerializeField] private string _tapActionName = "Tap";
        [SerializeField] private string _aimPointActionName = "AimPoint";

        private InputAction _tapAction;
        private InputAction _aimPointAction;

        public event Action OnTap;
        public Vector2 AimPoint { get; private set; }

        /// True while the finger/mouse button is actually held down. The turret should
        /// only re-aim while this is true — reading AimPoint on its own is not enough,
        /// since it reports (0,0) whenever nothing is currently pressed.
        public bool IsAiming => _tapAction.IsPressed();

        private void Awake()
        {
            InputActionMap map = _actions.FindActionMap(_actionMapName, throwIfNotFound: true);
            _tapAction = map.FindAction(_tapActionName, throwIfNotFound: true);
            _aimPointAction = map.FindAction(_aimPointActionName, throwIfNotFound: true);
        }

        private void OnEnable()
        {
            _tapAction.Enable();
            _aimPointAction.Enable();
            _tapAction.performed += HandleTapPerformed;
        }

        private void OnDisable()
        {
            _tapAction.performed -= HandleTapPerformed;
            _tapAction.Disable();
            _aimPointAction.Disable();
        }

        private void Update()
        {
            AimPoint = _aimPointAction.ReadValue<Vector2>();
        }

        private void HandleTapPerformed(InputAction.CallbackContext context)
        {
            OnTap?.Invoke();
        }
    }
}
