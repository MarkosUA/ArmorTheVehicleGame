using UnityEngine;

namespace ArmorTheVehicle.UI
{
    /// Shared "Exit" button behavior: stops Play mode in the Editor, quits in a real build.
    internal static class AppQuit
    {
        public static void QuitOrStopPlaying()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
