using MakeStack.Ultilities;
using System.Collections.Generic;
using UnityEngine;

namespace MakeStack.Manager
{
    public interface IPooledObject
    {
        void OnSpawn();
        void OnDespawn();
    }

    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        private class Pool
        {
            public GameObject prefab;
            public Queue<GameObject> inactive = new();
            public Transform root;

            public int totalCount;
            public int maxSize;
        }

        private Dictionary<GameObject, Pool> _pools = new();
        private Dictionary<int, Pool> _idToPool = new();
        
        public void CreatePool(GameObject prefab, int initialSize, int maxSize)
        {
            if (prefab == null) return;

            if (_pools.ContainsKey(prefab)) return;

            var pool = new Pool
            {
                prefab = prefab,
                maxSize = Mathf.Max(1, maxSize)
            };

            GameObject rootGO = new GameObject($"Pool_{prefab.name}");
            rootGO.transform.SetParent(this.transform);
            pool.root = rootGO.transform;

            for (int i = 0; i < Mathf.Clamp(initialSize, 0, pool.maxSize); i++)
            {
                var obj = Instantiate(prefab, pool.root);
                obj.SetActive(false);

                _idToPool[obj.GetInstanceID()] = pool;
                pool.inactive.Enqueue(obj);
                pool.totalCount++;
            }

            _pools[prefab] = pool;

            if (enableDebugLog)
                Debug.Log($"[PoolManager] Created pool for {prefab.name}, size {initialSize}/{maxSize}");
        }
        
        public GameObject GetObject(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out var pool)) return null;

            GameObject obj;

            if (pool.inactive.Count > 0)
            {
                obj = pool.inactive.Dequeue();
            }
            else if (pool.totalCount < pool.maxSize)
            {
                obj = Instantiate(prefab);
                pool.totalCount++;
                _idToPool[obj.GetInstanceID()] = pool;
            }
            else
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[PoolManager] Pool for {prefab.name} is full!");
                return null;
            }

            obj.transform.SetParent(null, false);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);

            if (obj.TryGetComponent<IPooledObject>(out var pooled))
                pooled.OnSpawn();

            return obj;
        }
        
        public void ReturnObject(GameObject obj)
        {
            if (obj == null) return;

            if (!_idToPool.TryGetValue(obj.GetInstanceID(), out var pool))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[PoolManager] Destroy {obj.name} (no pool found).");

                Destroy(obj);
                return;
            }

            if (obj.TryGetComponent<IPooledObject>(out var pooled))
                pooled.OnDespawn();

            obj.SetActive(false);
            obj.transform.SetParent(pool.root, false);
            pool.inactive.Enqueue(obj);

            if (enableDebugLog)
                Debug.Log($"[PoolManager] Returned {obj.name} to pool {pool.prefab.name}");
        }

        public int TotalObjects(GameObject prefab)
        {
            return _pools.TryGetValue(prefab, out var pool) ? pool.totalCount : 0;
        }

        public int InactiveCount(GameObject prefab)
        {
            return _pools.TryGetValue(prefab, out var pool) ? pool.inactive.Count : 0;
        }
    }
}
