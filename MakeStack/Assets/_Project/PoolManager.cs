using MakeStack.Ultilities;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MakeStack.Manager
{
    public class PoolManager : Singleton<PoolManager>
    {
        #region --- Methods ---

        /// <summary>
        /// Tạo pool cho một prefab với số lượng ban đầu và giới hạn tối đa.
        /// </summary>
        public void CreatePool(GameObject prefab, int initialSize, int maxSize)
        {
            string key = prefab.name;

            if (!_poolDict.ContainsKey(key))
            {
                _poolDict[key] = new Queue<GameObject>();
                _poolMaxSize[key] = maxSize;

                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    _poolDict[key].Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// Lấy object từ pool, nếu pool trống sẽ tạo thêm (nếu chưa vượt quá max).
        /// </summary>
        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;

            if (!_poolDict.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] Pool for {key} chưa được tạo. Gọi CreatePool trước!");
                return null;
            }

            GameObject obj;

            if (_poolDict[key].Count > 0)
            {
                obj = _poolDict[key].Dequeue();
            }
            else
            {
                // Pool trống → tạo thêm nếu chưa vượt max
                if (TotalObjectsInPool(key) < _poolMaxSize[key])
                {
                    obj = Instantiate(prefab);
                }
                else
                {
                    Debug.LogWarning($"[PoolManager] Pool {key} đã đạt max size {_poolMaxSize[key]}.");
                    return null;
                }
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Trả object về pool (deactivate).
        /// </summary>
        public void ReturnObject(GameObject prefab, GameObject obj)
        {
            string key = prefab.name;

            if (!_poolDict.ContainsKey(key))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            _poolDict[key].Enqueue(obj);
        }

        /// <summary>
        /// Đếm số object hiện tại trong pool (bao gồm active + inactive).
        /// </summary>
        private int TotalObjectsInPool(string key)
        {
            return _poolDict[key].Count;
        }

        #endregion

        #region --- Properties ---



        #endregion

        #region --- Fields ---

        private Dictionary<string, Queue<GameObject>> _poolDict = new();
        private Dictionary<string, int> _poolMaxSize = new();

        #endregion

    }

}