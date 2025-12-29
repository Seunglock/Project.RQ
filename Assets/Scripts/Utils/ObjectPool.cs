using UnityEngine;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// Generic object pool for reducing memory allocations and GC pressure
    /// Maintains pools of reusable objects to avoid frequent instantiation/destruction
    /// </summary>
    /// <typeparam name="T">Type of object to pool (must be Component)</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private Queue<T> pool = new Queue<T>();
        private T prefab;
        private Transform parent;
        private int initialSize;
        private int maxSize;
        private int activeCount = 0;

        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.maxSize = maxSize;
            this.parent = parent;

            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }

            // Register with performance manager
            PerformanceManager.Instance.RegisterPooledObject(typeof(T).Name, pool.Count);
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (activeCount < maxSize)
            {
                obj = CreateNewObject();
                pool.Dequeue(); // Remove it immediately since we're returning it
            }
            else
            {
                Debug.LogWarning($"Object pool for {typeof(T).Name} reached max size {maxSize}. Reusing oldest object.");
                obj = pool.Dequeue();
            }

            obj.gameObject.SetActive(true);
            activeCount++;

            // Update performance manager
            PerformanceManager.Instance.RegisterPooledObject(typeof(T).Name, pool.Count);

            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Attempted to return null object to pool");
                return;
            }

            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
            activeCount--;

            // Update performance manager
            PerformanceManager.Instance.RegisterPooledObject(typeof(T).Name, pool.Count);
        }

        /// <summary>
        /// Get the number of objects currently available in the pool
        /// </summary>
        public int AvailableCount => pool.Count;

        /// <summary>
        /// Get the number of objects currently active (in use)
        /// </summary>
        public int ActiveCount => activeCount;

        /// <summary>
        /// Clear all objects from the pool
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            activeCount = 0;

            // Update performance manager
            PerformanceManager.Instance.RegisterPooledObject(typeof(T).Name, 0);
        }
    }

    /// <summary>
    /// Static pool manager for easy access to commonly pooled objects
    /// </summary>
    public static class PoolManager
    {
        private static Dictionary<string, object> pools = new Dictionary<string, object>();

        /// <summary>
        /// Create or get a pool for a specific type
        /// </summary>
        public static ObjectPool<T> GetPool<T>(T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null) where T : Component
        {
            string key = typeof(T).Name;

            if (pools.ContainsKey(key))
            {
                return pools[key] as ObjectPool<T>;
            }

            var newPool = new ObjectPool<T>(prefab, initialSize, maxSize, parent);
            pools[key] = newPool;
            return newPool;
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public static void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                var clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
            }
            pools.Clear();
        }

        /// <summary>
        /// Get statistics for all pools
        /// </summary>
        public static string GetPoolStats()
        {
            if (pools.Count == 0)
                return "No active pools";

            string stats = "Object Pools:\n";
            foreach (var kvp in pools)
            {
                var pool = kvp.Value;
                var availableProperty = pool.GetType().GetProperty("AvailableCount");
                var activeProperty = pool.GetType().GetProperty("ActiveCount");

                int available = (int)availableProperty.GetValue(pool);
                int active = (int)activeProperty.GetValue(pool);

                stats += $"{kvp.Key}: Available={available}, Active={active}\n";
            }
            return stats;
        }
    }
}
