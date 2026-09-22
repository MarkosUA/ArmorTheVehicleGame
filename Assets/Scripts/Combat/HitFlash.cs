using UnityEngine;

namespace ArmorTheVehicle.Combat
{
    /// Briefly swaps one or more renderers to a flat white material on taking damage, then
    /// restores their original material(s). A flat swap is used instead of tinting the
    /// existing material's color, because an entity's visible color usually comes from its
    /// albedo texture — multiplying that texture by a white tint (usually already close to
    /// white by default) barely changes what's on screen.
    /// The flash material is a single instance shared by every user of this class (lazily
    /// created on first use), since it's always the same flat white regardless of who's
    /// flashing — only the per-renderer material arrays genuinely differ per instance. Every
    /// assignment goes through `sharedMaterials` rather than `materials` — the
    /// instance-property setter clones on every assignment, which would otherwise leak a
    /// new Material each hit.
    internal sealed class HitFlash
    {
        private const float FlashDuration = 0.1f;
        private const string FlashShaderName = "Universal Render Pipeline/Unlit";

        private static Material _sharedFlashMaterial;

        private readonly Renderer[] _renderers;
        private readonly Material[][] _originalMaterials;
        private readonly Material[][] _flashMaterials;
        private int _flashToken;

        /// Uses `assignedRenderers` directly if the caller already has them (e.g. wired up by
        /// hand in the Inspector) — otherwise falls back to collecting every renderer under
        /// `root` (or does nothing if `root` is null too), so callers don't need an inline
        /// null-check ternary of their own.
        public static HitFlash FromModel(Transform root, Renderer[] assignedRenderers = null)
        {
            if (assignedRenderers != null && assignedRenderers.Length > 0)
            {
                return new HitFlash(assignedRenderers);
            }

            return new HitFlash(root != null ? root.GetComponentsInChildren<Renderer>() : null);
        }

        public HitFlash(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return;

            Material flashMaterial = GetOrCreateSharedFlashMaterial();
            if (flashMaterial == null) return; // shader unavailable — flash stays disabled, never throws

            _renderers = renderers;
            _originalMaterials = new Material[_renderers.Length][];
            _flashMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] original = _renderers[i].sharedMaterials;
                _originalMaterials[i] = original;

                Material[] flash = new Material[original.Length];
                for (int j = 0; j < flash.Length; j++)
                {
                    flash[j] = flashMaterial;
                }
                _flashMaterials[i] = flash;
            }
        }

        private static Material GetOrCreateSharedFlashMaterial()
        {
            if (_sharedFlashMaterial != null) return _sharedFlashMaterial;

            Shader shader = Shader.Find(FlashShaderName);
            if (shader == null) return null;

            _sharedFlashMaterial = new Material(shader) { color = Color.white };
            return _sharedFlashMaterial;
        }

        public void Flash()
        {
            if (_renderers == null) return;
            FlashAsync();
        }

        private async void FlashAsync()
        {
            int token = ++_flashToken;
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = _flashMaterials[i];
            }

            await Awaitable.WaitForSecondsAsync(FlashDuration);
            if (token != _flashToken) return; // superseded by a more recent hit — let that one own the revert

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }
    }
}
