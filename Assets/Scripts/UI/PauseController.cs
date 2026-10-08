using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.UI
{
    /// Pause button: toggles GameState.IsPaused (which owns the Time.timeScale freeze/
    /// resume) and fades in a panel with Restart/Exit. The fade itself runs on unscaled
    /// time so it still animates at timeScale 0, and UI clicks keep working regardless.
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _buttonLabel;
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private float _fadeSpeed = 5f;

        private GameState _gameState;
        private AudioManager _audioManager;
        private CanvasGroupFader _fader;

        [Inject]
        public void Construct(GameState gameState, AudioManager audioManager)
        {
            _gameState = gameState;
            _audioManager = audioManager;
        }

        private void Awake()
        {
            _fader = new CanvasGroupFader(_panel, _fadeSpeed);
            _panel.alpha = 0f;
            _panel.blocksRaycasts = false;
            _panel.interactable = false;
        }

        private void OnEnable()
        {
            _pauseButton.onClick.AddListener(TogglePause);
            _restartButton.onClick.AddListener(HandleRestart);
            _exitButton.onClick.AddListener(HandleExit);
        }

        private void OnDisable()
        {
            _pauseButton.onClick.RemoveListener(TogglePause);
            _restartButton.onClick.RemoveListener(HandleRestart);
            _exitButton.onClick.RemoveListener(HandleExit);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            _fader.Tick(_gameState.IsPaused);
        }

        private void TogglePause()
        {
            _audioManager?.PlayButtonClick();

            bool isPaused = !_gameState.IsPaused;
            _gameState.SetPaused(isPaused);
            _panel.blocksRaycasts = isPaused;
            _panel.interactable = isPaused;
            _buttonLabel.text = isPaused ? ">" : "II";
        }

        private void HandleRestart()
        {
            TogglePause();
            _gameState.RequestRestartAsync();
        }

        private void HandleExit()
        {
            _audioManager?.PlayButtonClick();
            AppQuit.QuitOrStopPlaying();
        }
    }
}
