using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.UI
{
    /// Shows the Win/Lose panel on GameState events with Restart and Exit buttons.
    public sealed class ResultOverlayController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private string _winMessage = "You win";
        [SerializeField] private string _loseMessage = "You lose";
        [SerializeField] private float _fadeSpeed = 4f;

        private GameState _gameState;
        private AudioManager _audioManager;
        private CanvasGroupFader _fader;
        private bool _visible;

        [Inject]
        public void Construct(GameState gameState, AudioManager audioManager)
        {
            _gameState = gameState;
            _audioManager = audioManager;
        }

        private void Awake()
        {
            _fader = new CanvasGroupFader(_panel, _fadeSpeed);
            SetVisible(false, instant: true);
        }

        private void OnEnable()
        {
            _gameState.OnWon += HandleWon;
            _gameState.OnLost += HandleLost;
            _gameState.OnRestartRequested += HandleRestart;

            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);
            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            _gameState.OnWon -= HandleWon;
            _gameState.OnLost -= HandleLost;
            _gameState.OnRestartRequested -= HandleRestart;

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void Update()
        {
            _fader.Tick(_visible);
        }

        private void HandleWon()
        {
            _audioManager?.PlayVictory();
            _resultText.text = _winMessage;
            SetVisible(true);
        }

        private void HandleLost()
        {
            _audioManager?.PlayDefeat();
            _resultText.text = _loseMessage;
            SetVisible(true);
        }

        private void HandleRestart()
        {
            SetVisible(false);
        }

        private void OnRestartClicked()
        {
            _audioManager?.PlayButtonClick();
            SetVisible(false);
            _gameState.RequestRestartAsync();
        }

        private void OnExitClicked()
        {
            _audioManager?.PlayButtonClick();
            AppQuit.QuitOrStopPlaying();
        }

        private void SetVisible(bool visible, bool instant = false)
        {
            _visible = visible;
            _panel.blocksRaycasts = visible;
            _panel.interactable = visible;
            if (instant) _panel.alpha = visible ? 1f : 0f;
        }
    }
}

