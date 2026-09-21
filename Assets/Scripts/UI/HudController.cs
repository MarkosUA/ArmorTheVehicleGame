using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArmorTheVehicle.Config;

namespace ArmorTheVehicle.UI
{
    /// Level-progress bar, distance indicator, coins, and level badge.
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private LevelConfig _config;
        [SerializeField] private Transform _carTransform;
        [SerializeField] private Image _progressFill;
        [SerializeField] private RectTransform _progressIndicator;
        [SerializeField] private RectTransform _progressBarRect;
        [SerializeField] private TMP_Text _distanceText;
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _levelText;

        private void Awake()
        {
            if (_coinText != null && string.IsNullOrEmpty(_coinText.text))
                _coinText.text = "110";
            if (_levelText != null && string.IsNullOrEmpty(_levelText.text))
                _levelText.text = "1";
        }

        private void Update()
        {
            if (_carTransform == null || _config == null) return;

            float maxDist = Mathf.Max(0.01f, _config.levelLength);
            float currentDist = Mathf.Max(0f, _carTransform.position.z);
            float progress = Mathf.Clamp01(currentDist / maxDist);

            if (_progressFill != null)
            {
                _progressFill.fillAmount = progress;
            }

            if (_progressIndicator != null && _progressBarRect != null)
            {
                float barHeight = _progressBarRect.rect.height;
                float usableHeight = Mathf.Max(0f, barHeight - 8f);
                float y = 4f + progress * usableHeight;
                Vector2 pos = _progressIndicator.anchoredPosition;
                pos.y = y;
                _progressIndicator.anchoredPosition = pos;
            }

            if (_distanceText != null)
            {
                _distanceText.text = $"{Mathf.RoundToInt(currentDist)}m";
            }
        }

        public void SetCoins(int coins)
        {
            if (_coinText != null) _coinText.text = coins.ToString();
        }

        public void SetLevel(int level)
        {
            if (_levelText != null) _levelText.text = level.ToString();
        }
    }
}

