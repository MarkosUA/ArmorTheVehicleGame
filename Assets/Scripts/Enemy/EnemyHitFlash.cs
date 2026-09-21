using UnityEngine;

namespace ArmorTheVehicle.Enemy
{
    /// Briefly swaps the enemy's renderer to a flat white material on taking damage, then
    /// restores its original material(s). A flat swap is used instead of tinting the
    /// existing material's color, because the enemy's visible color comes from its albedo
    /// texture — multiplying that texture by a white tint (usually already close to white
    /// by default) barely changes what's on screen.
    /// The flash material is a single instance shared by every enemy (lazily created on
    /// first use), since it's always the same flat white regardless of which enemy is
    /// flashing — only the per-renderer material arrays genuinely differ per enemy. Every
    /// assignment goes through `sharedMaterials` rather than `materials` — the
    /// instance-property setter clones on every assignment, which would otherwise leak a
    /// new Material each hit.
    internal sealed class EnemyHitFlash
    {
        private const float FlashDuration = 0.1f;
        private const string FlashShaderName = "Universal Render Pipeline/Unlit";

        private static Material _sharedFlashMaterial;

        private readonly Renderer _renderer;
        private readonly Material[] _originalMaterials;
        private readonly Material[] _flashMaterials;
        private int _flashToken;

        public EnemyHitFlash(Renderer renderer)
        {
            if (renderer == null) return;

            Material flashMaterial = GetOrCreateSharedFlashMaterial();
            if (flashMaterial == null) return; // shader unavailable — flash stays disabled, never throws

            _renderer = renderer;
            _originalMaterials = renderer.sharedMaterials;

            _flashMaterials = new Material[_originalMaterials.Length];
            for (int i = 0; i < _flashMaterials.Length; i++)
            {
                _flashMaterials[i] = flashMaterial;
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
            if (_renderer == null) return;
            FlashAsync();
        }

        private async void FlashAsync()
        {
            int token = ++_flashToken;
            _renderer.sharedMaterials = _flashMaterials;

            await Awaitable.WaitForSecondsAsync(FlashDuration);
            if (token != _flashToken) return; // superseded by a more recent hit — let that one own the revert

            _renderer.sharedMaterials = _originalMaterials;
        }
    }
}
