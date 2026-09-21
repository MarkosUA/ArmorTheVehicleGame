using UnityEngine;
using UnityEngine.UI;
using ArmorTheVehicle.Combat;

namespace ArmorTheVehicle.UI
{
    /// Small world-space HP bar floating above an entity (car or enemy). Purely a view:
    /// reads Health's events, hides itself on death if configured, never touches gameplay state.
    public sealed class WorldHealthBarView : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Image _fill;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private bool _hideOnDeath = true;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_visualRoot == null)
            {
                _visualRoot = gameObject;
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged += HandleDamaged;
                _health.OnDied += HandleDied;
                HandleDamaged(_health.CurrentHealth, _health.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleDamaged;
                _health.OnDied -= HandleDied;
            }
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            transform.rotation = _mainCamera.transform.rotation;
        }

        private void HandleDamaged(float current, float max)
        {
            if (_fill != null)
            {
                _fill.fillAmount = max > 0f ? current / max : 0f;
            }

            if (_hideOnDeath && current <= 0f)
            {
                SetVisible(false);
            }
            else
            {
                SetVisible(true);
            }
        }

        private void HandleDied()
        {
            if (_hideOnDeath)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visualRoot != null && _visualRoot.activeSelf != visible)
            {
                _visualRoot.SetActive(visible);
            }
        }
    }
}

