using UnityEngine;
using ArmorTheVehicle.Config;

namespace ArmorTheVehicle.Level
{
    /// Measures one instantiated ground tile's bounds, then tiles copies end-to-end
    /// (with a small overlap to hide seams) until the configured level length is covered.
    public sealed class GroundTiler : MonoBehaviour
    {
        [SerializeField] private LevelConfig _config;
        [SerializeField] private Transform _groundTilePrefab;

        private void Awake()
        {
            Transform firstTile = Instantiate(_groundTilePrefab, transform);
            firstTile.localPosition = Vector3.zero;

            float tileDepth = MeasureDepth(firstTile);
            if (tileDepth <= 0f) return;

            // Clamp away from zero/negative: a tile shallower than the configured overlap
            // (or an overlap misconfigured to match/exceed it) would otherwise send the
            // tile count toward infinity instead of degrading gracefully.
            float step = Mathf.Max(tileDepth - _config.groundTileOverlap, 0.01f);
            int tileCount = Mathf.CeilToInt(_config.levelLength / step) + 1;

            for (int i = 1; i < tileCount; i++)
            {
                Transform tile = Instantiate(_groundTilePrefab, transform);
                tile.localPosition = new Vector3(0f, 0f, i * step);
            }
        }

        private static float MeasureDepth(Transform tile)
        {
            var renderers = tile.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.z;
        }
    }
}
