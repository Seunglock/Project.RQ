using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Platform-specific optimizations and graceful degradation
    /// Adjusts game settings based on platform capabilities
    /// </summary>
    public static class PlatformOptimizations
    {
        private static bool isInitialized = false;

        /// <summary>
        /// Initialize platform-specific optimizations
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
                return;

            ApplyPlatformOptimizations();
            isInitialized = true;

            Debug.Log($"Platform optimizations applied for {Application.platform}");
        }

        private static void ApplyPlatformOptimizations()
        {
#if UNITY_STANDALONE
            // PC/Mac/Linux optimizations
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            Screen.fullScreen = true;

            Debug.Log("Applied STANDALONE optimizations: High quality, 60 FPS, VSync enabled");
#endif

#if UNITY_SWITCH
            // Nintendo Switch optimizations (3.5GB RAM limit)
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.SetQualityLevel(1, true); // Medium quality

            // Reduce memory usage
            Resources.UnloadUnusedAssets();

            Debug.Log("Applied SWITCH optimizations: Medium quality, reduced memory usage");
#endif

#if UNITY_ANDROID
            // Android optimizations
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0; // Disable VSync on mobile
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.SetQualityLevel(0, true); // Low quality
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("Applied ANDROID optimizations: Low quality, battery optimized");
#endif

#if UNITY_IOS
            // iOS optimizations
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.SetQualityLevel(0, true); // Low quality
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("Applied IOS optimizations: Low quality, battery optimized");
#endif
        }

        /// <summary>
        /// Get recommended object pool sizes based on platform
        /// </summary>
        public static (int initialSize, int maxSize) GetPoolSizesForPlatform()
        {
#if UNITY_STANDALONE
            return (20, 200); // PC can handle larger pools
#elif UNITY_SWITCH
            return (10, 100); // Switch has memory constraints
#elif UNITY_ANDROID || UNITY_IOS
            return (5, 50); // Mobile has strict memory limits
#else
            return (10, 100); // Default
#endif
        }

        /// <summary>
        /// Check if current platform supports high quality graphics
        /// </summary>
        public static bool SupportsHighQuality()
        {
#if UNITY_STANDALONE
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if current platform requires battery optimization
        /// </summary>
        public static bool RequiresBatteryOptimization()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Get maximum memory budget in MB for current platform
        /// </summary>
        public static float GetMemoryBudgetMB()
        {
#if UNITY_STANDALONE
            return 200.0f; // PC can use more memory
#elif UNITY_SWITCH
            return 100.0f; // Switch has 3.5GB total, be conservative
#elif UNITY_ANDROID || UNITY_IOS
            return 50.0f; // Mobile has strict limits
#else
            return 100.0f; // Default
#endif
        }

        /// <summary>
        /// Apply graceful degradation based on current performance
        /// </summary>
        public static void ApplyGracefulDegradation(float currentFPS, float memoryUsageMB)
        {
            // If FPS is below 45, reduce quality
            if (currentFPS < 45.0f)
            {
                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality > 0)
                {
                    QualitySettings.SetQualityLevel(currentQuality - 1, true);
                    Debug.LogWarning($"FPS below 45 ({currentFPS:F1}). Reducing quality level to {currentQuality - 1}");
                }
            }

            // If memory usage is above 90% of budget, force GC
            float memoryBudget = GetMemoryBudgetMB();
            if (memoryUsageMB > memoryBudget * 0.9f)
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
                Debug.LogWarning($"Memory usage at {memoryUsageMB:F2}MB (budget: {memoryBudget}MB). Triggered cleanup.");
            }
        }

        /// <summary>
        /// Get platform name for display
        /// </summary>
        public static string GetPlatformName()
        {
#if UNITY_STANDALONE_WIN
            return "Windows PC";
#elif UNITY_STANDALONE_OSX
            return "macOS";
#elif UNITY_STANDALONE_LINUX
            return "Linux";
#elif UNITY_SWITCH
            return "Nintendo Switch";
#elif UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#else
            return "Unknown Platform";
#endif
        }

        /// <summary>
        /// Get recommended texture quality for platform
        /// </summary>
        public static int GetRecommendedTextureQuality()
        {
#if UNITY_STANDALONE
            return 0; // Full resolution
#elif UNITY_SWITCH
            return 1; // Half resolution
#elif UNITY_ANDROID || UNITY_IOS
            return 2; // Quarter resolution
#else
            return 1; // Default to half
#endif
        }
    }
}
