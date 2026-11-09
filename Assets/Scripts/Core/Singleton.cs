using UnityEngine;

namespace Whiskers.Core
{
    // Use OnAwake instead of Awake
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        // Override to keep the manager alive between scene loads
        protected virtual bool PersistAcrossScenes => false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate {typeof(T).Name} found on '{name}', destroying it.");
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;

            if (PersistAcrossScenes)
            {
                // DontDestroyOnLoad only works on root objects
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            OnAwake();
        }

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
