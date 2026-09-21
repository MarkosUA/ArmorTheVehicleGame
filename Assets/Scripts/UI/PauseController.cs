using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.UI
{
    /// Pause button: freezes/resumes via Time.timeScale and fades in a panel with
    /// Restart/Exit. The fade itself runs on unscaled time so it still animates at
    /// timeScale 0, and UI clicks keep working at timeScale 0 regardless.
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
        private bool _isPaused;

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
            _fader.Tick(_isPaused);
        }

        private void TogglePause()
        {
            _audioManager?.PlayButtonClick();

            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            _panel.blocksRaycasts = _isPaused;
            _panel.interactable = _isPaused;
            _buttonLabel.text = _isPaused ? "▶" : "II";
            _gameState.SetPaused(_isPaused);
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
