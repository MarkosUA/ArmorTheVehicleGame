using UnityEngine;

namespace ArmorTheVehicle.UI
{
    /// Fades a CanvasGroup's alpha toward 0/1 on unscaled time, so it keeps animating even
    /// at Time.timeScale 0 (e.g. while the game itself is paused).
    internal sealed class CanvasGroupFader
    {
        private readonly CanvasGroup _group;
        private readonly float _speed;

        public CanvasGroupFader(CanvasGroup group, float speed)
        {
            _group = group;
            _speed = speed;
        }

        public void Tick(bool visible)
        {
            float target = visible ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, _speed * Time.unscaledDeltaTime);
        }
    }
}
