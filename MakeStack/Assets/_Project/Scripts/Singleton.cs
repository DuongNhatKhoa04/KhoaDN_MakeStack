using UnityEngine;

namespace MakeStack.Ultilities
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        [SerializeField] protected bool dontDestroyOnLoad = true;
        
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<T>();
                
                if (_instance != null) return _instance;

                SetUpInstance();
                
                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            RemoveDuplicates();
        }
        
        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        
        private static void SetUpInstance()
        {
            if (_instance != null) return;
            
            GameObject singleton = new(typeof(T).Name);
            _instance = singleton.AddComponent<T>();

            DontDestroyOnLoad(singleton);
        }
        
        private void RemoveDuplicates()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (!dontDestroyOnLoad) return;
                
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
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
