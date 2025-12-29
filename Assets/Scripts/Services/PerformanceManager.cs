using UnityEngine;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// Manages performance optimization to maintain 60 FPS target
    /// Tracks frame rate, memory usage, and applies optimizations
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        private static PerformanceManager instance;
        public static PerformanceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("PerformanceManager");
                    instance = go.AddComponent<PerformanceManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Performance metrics
        private float deltaTime = 0.0f;
        private float fps = 60.0f;
        private float updateInterval = 0.5f; // Update FPS every 0.5 seconds
        private float timeSinceUpdate = 0.0f;

        // Frame rate tracking
        private Queue<float> frameRateHistory = new Queue<float>();
        private const int FRAME_HISTORY_SIZE = 60;

        // Memory tracking
        private float memoryUsageMB = 0.0f;
        private const float MEMORY_WARNING_THRESHOLD = 80.0f; // 80MB warning
        private const float MEMORY_CRITICAL_THRESHOLD = 95.0f; // 95MB critical

        // Performance settings
        private bool enableVSync = true;
        private int targetFrameRate = 60;
        private QualityLevel currentQuality = QualityLevel.High;

        // Object pooling statistics
        private Dictionary<string, int> pooledObjectCounts = new Dictionary<string, int>();

        public enum QualityLevel
        {
            Low = 0,
            Medium = 1,
            High = 2
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePerformanceSettings();
        }

        private void InitializePerformanceSettings()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;

            // Enable VSync for smooth gameplay
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;

            // Set quality based on platform
#if UNITY_SWITCH
            currentQuality = QualityLevel.Medium;
            QualitySettings.SetQualityLevel(1, true);
#elif UNITY_ANDROID || UNITY_IOS
            currentQuality = QualityLevel.Low;
            QualitySettings.SetQualityLevel(0, true);
#else
            currentQuality = QualityLevel.High;
            QualitySettings.SetQualityLevel(2, true);
#endif

            Debug.Log($"Performance initialized: Quality={currentQuality}, TargetFPS={targetFrameRate}, VSync={enableVSync}");
        }

        private void Update()
        {
            // Calculate delta time for FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            timeSinceUpdate += Time.unscaledDeltaTime;

            // Update FPS at regular intervals
            if (timeSinceUpdate >= updateInterval)
            {
                fps = 1.0f / deltaTime;
                timeSinceUpdate = 0.0f;

                // Track frame rate history
                frameRateHistory.Enqueue(fps);
                if (frameRateHistory.Count > FRAME_HISTORY_SIZE)
                {
                    frameRateHistory.Dequeue();
                }

                // Check for performance degradation
                CheckPerformance();
            }

            // Update memory usage every second
            UpdateMemoryUsage();
        }

        private void CheckPerformance()
        {
            float averageFPS = GetAverageFPS();

            // If FPS drops below 50 and we're on high quality, downgrade
            if (averageFPS < 50.0f && currentQuality == QualityLevel.High)
            {
                SetQualityLevel(QualityLevel.Medium);
                Debug.LogWarning($"Performance degradation detected (FPS={averageFPS:F1}). Lowering quality to Medium.");
            }
            else if (averageFPS < 40.0f && currentQuality == QualityLevel.Medium)
            {
                SetQualityLevel(QualityLevel.Low);
                Debug.LogWarning($"Performance degradation detected (FPS={averageFPS:F1}). Lowering quality to Low.");
            }
        }

        private void UpdateMemoryUsage()
        {
            // Update memory usage
            memoryUsageMB = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);

            // Check memory thresholds
            if (memoryUsageMB >= MEMORY_CRITICAL_THRESHOLD)
            {
                Debug.LogError($"CRITICAL: Memory usage at {memoryUsageMB:F2}MB! Triggering garbage collection.");
                System.GC.Collect();
            }
            else if (memoryUsageMB >= MEMORY_WARNING_THRESHOLD)
            {
                Debug.LogWarning($"WARNING: Memory usage at {memoryUsageMB:F2}MB approaching limit.");
            }
        }

        /// <summary>
        /// Get current FPS
        /// </summary>
        public float GetFPS()
        {
            return fps;
        }

        /// <summary>
        /// Get average FPS over the last FRAME_HISTORY_SIZE frames
        /// </summary>
        public float GetAverageFPS()
        {
            if (frameRateHistory.Count == 0)
                return fps;

            float sum = 0.0f;
            foreach (float rate in frameRateHistory)
            {
                sum += rate;
            }
            return sum / frameRateHistory.Count;
        }

        /// <summary>
        /// Get current memory usage in MB
        /// </summary>
        public float GetMemoryUsageMB()
        {
            return memoryUsageMB;
        }

        /// <summary>
        /// Set quality level
        /// </summary>
        public void SetQualityLevel(QualityLevel level)
        {
            currentQuality = level;
            QualitySettings.SetQualityLevel((int)level, true);
            Debug.Log($"Quality level set to {level}");
        }

        /// <summary>
        /// Get current quality level
        /// </summary>
        public QualityLevel GetQualityLevel()
        {
            return currentQuality;
        }

        /// <summary>
        /// Register pooled object for tracking
        /// </summary>
        public void RegisterPooledObject(string poolName, int count)
        {
            if (pooledObjectCounts.ContainsKey(poolName))
            {
                pooledObjectCounts[poolName] = count;
            }
            else
            {
                pooledObjectCounts.Add(poolName, count);
            }
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"FPS: {fps:F1} (Avg: {GetAverageFPS():F1})\n" +
                   $"Memory: {memoryUsageMB:F2}MB / 100MB\n" +
                   $"Quality: {currentQuality}\n" +
                   $"Pooled Objects: {pooledObjectCounts.Count} pools";
        }

        /// <summary>
        /// Force garbage collection (use sparingly)
        /// </summary>
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            Debug.Log("Forced garbage collection");
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
