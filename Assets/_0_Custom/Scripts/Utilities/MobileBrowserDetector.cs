using UnityEngine;
#if !UNITY_EDITOR && UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace ParkourLegion.Utilities
{
    public static class MobileBrowserDetector
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern int IsMobileBrowser();
#endif

        public static bool IsMobile()
        {
#if UNITY_EDITOR
            return false;
#elif UNITY_WEBGL
            try
            {
                int result = IsMobileBrowser();
                return result == 1;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MobileBrowserDetector: Failed to detect mobile - {e.Message}");
                return false;
            }
#else
            return Application.isMobilePlatform;
#endif
        }

        public static string GetDeviceInfo()
        {
#if UNITY_EDITOR
            return "Editor (Desktop)";
#elif UNITY_WEBGL
            bool isMobile = IsMobile();
            return isMobile ? "WebGL (Mobile Browser)" : "WebGL (Desktop Browser)";
#else
            return $"{Application.platform}";
#endif
        }
    }
}
