using UnityEngine;

namespace ArmorTheVehicle.Car
{
    /// A single grow-then-return "punch" on a Transform's localScale — e.g. a brief hit
    /// reaction. Cancel-safe: retriggering mid-pulse restarts cleanly instead of stacking or
    /// leaving the scale stuck away from its base value, using the same token-guard idiom as
    /// ArmorTheVehicle.Combat.HitFlash.
    internal sealed class ScalePulse
    {
        private const float Duration = 0.15f; // total, grow+return

        private readonly Transform _transform;
        private readonly Vector3 _baseScale;
        private readonly float _peakMultiplier;
        private int _token;

        /// `transform` may be null (e.g. the model wasn't found) — Pulse() then just no-ops.
        public ScalePulse(Transform transform, float peakMultiplier)
        {
            _transform = transform;
            _baseScale = transform != null ? transform.localScale : Vector3.one;
            _peakMultiplier = peakMultiplier;
        }

        public void Pulse()
        {
            if (_transform == null) return;
            PulseAsync();
        }

        private async void PulseAsync()
        {
            int token = ++_token;
            float half = Duration / 2f;

            if (!await Animate(_baseScale, _baseScale * _peakMultiplier, half, token)) return;
            if (!await Animate(_transform.localScale, _baseScale, half, token)) return;

            _transform.localScale = _baseScale; // guard against float drift after the loop above
        }

        // Returns false if a newer Pulse() call superseded this one mid-animation.
        private async Awaitable<bool> Animate(Vector3 from, Vector3 to, float duration, int token)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (token != _token) return false;
                _transform.localScale = Vector3.Lerp(from, to, t / duration);
                await Awaitable.NextFrameAsync();
            }

            return token == _token;
        }
    }
}
