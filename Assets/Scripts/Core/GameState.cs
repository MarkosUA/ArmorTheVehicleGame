using System;
using UnityEngine;
using ArmorTheVehicle.Config;

namespace ArmorTheVehicle.Core
{
    /// Single source of truth for run/win/lose state. Holds no references to other
    /// systems — everyone who cares about a transition subscribes to the matching event
    /// and resets itself; this class never reaches into anyone else.
    public sealed class GameState
    {
        private readonly LevelConfig _config;

        public GameState(LevelConfig config)
        {
            _config = config;
        }

        public bool IsRunning { get; private set; }
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }
        public bool IsPaused { get; private set; }

        /// Shorthand for "actively running and not paused" — the gate every gameplay system
        /// (turret, enemies, car) should check before reacting to input or ticking.
        public bool IsPlayable => IsRunning && !IsPaused;

        public event Action OnStarted;
        public event Action OnWon;
        public event Action OnLost;
        public event Action OnRestartRequested;

        public void StartRun()
        {
            if (IsRunning || HasWon || HasLost) return;

            IsRunning = true;
            OnStarted?.Invoke();
        }

        public void ReportWin()
        {
            if (!IsRunning) return;

            IsRunning = false;
            HasWon = true;
            OnWon?.Invoke();
        }

        public void ReportLoss()
        {
            if (!IsRunning) return;

            IsRunning = false;
            HasLost = true;
            OnLost?.Invoke();
        }

        /// Set by whatever UI currently owns the pause menu (see PauseController) — a
        /// separate axis from IsRunning, since the run is still "in progress" while paused,
        /// just with input/behavior that should react to it temporarily suppressed.
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        private bool _isRestarting;

        /// Callable from a won/lost state (tap-to-restart) or mid-run (pause menu's
        /// Restart button) alike — always just resets back to the initial idle state.
        /// Fire-and-forget by design (called from a synchronous tap/click handler, nothing
        /// awaits it) — async void matches Unity's own guidance for this shape and avoids
        /// an unretained Awaitable being pooled before its delay elapses.
        public async void RequestRestartAsync()
        {
            if (_isRestarting) return;

            _isRestarting = true;
            await Awaitable.WaitForSecondsAsync(_config.resultFadeDuration);

            IsRunning = false;
            HasWon = false;
            HasLost = false;
            _isRestarting = false;
            OnRestartRequested?.Invoke();
        }
    }
}
