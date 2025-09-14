using UnityEngine;

namespace MakeStack.Ultilities
{
    /// <summary>
    /// Singleton - Create Singleton Pattern. <br/>
    /// Developer: Duong Nhat Khoa - Created on 2025/09/14.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region --- Methods ---

        /// <summary>
        /// Create a new instance of the singleton if it doesn't already exist.
        /// </summary>
        private static void SetUpInstance()
        {
            if (_instance == null)
            {
                GameObject singleton = new(typeof(T).Name);
                _instance = singleton.AddComponent<T>();

                DontDestroyOnLoad(singleton);
            }
        }

        /// <summary>
        /// Remove duplicate instances of the singleton.
        /// </summary>
        private void RemoveDuplicates()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (_dontDestroyOnLoad)
                {
                    var root = transform.root;

                    if (root != transform)
                    {
                        DontDestroyOnLoad(root);
                    }
                    else
                    {
                        DontDestroyOnLoad(this.gameObject);
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region --- Unity Methods ---

        protected virtual void Awake()
        {
            RemoveDuplicates();
        }

        /// <summary>
        /// Guarantee that the instance is null when the object is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion

        #region --- Fields ---

        private static T _instance;

        [SerializeField] protected bool _dontDestroyOnLoad = true;

        #endregion

        #region --- Properties ---

        /// <summary>
        /// Lazy initialization of the singleton instance.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        SetUpInstance();
                    }
                }

                return _instance;
            }
        }

        #endregion
    }
}
