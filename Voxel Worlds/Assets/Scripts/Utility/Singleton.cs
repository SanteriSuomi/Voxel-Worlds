using UnityEngine;

namespace Voxel.Utility
{
    public abstract class SingletonBase : MonoBehaviour
    {
        protected static bool ApplicationIsClosing { get; set; }
    }

    public abstract class Singleton<T> : SingletonBase where T : Component
    {
        private static T BaseInstance { get; set; }
        public static T Instance
        {
            get
            {
                if (ApplicationIsClosing)
                {
                    return null;
                }
                else if (ValidateInstance())
                {
                    return BaseInstance;
                }
                else
                {
                    LogInitializationError();
                    return default;
                }
            }
        }

        protected virtual void Awake()
        {
            ApplicationIsClosing = false;
            if (!ValidateInstance()) LogInitializationError();
        }

        private static void LogInitializationError()
            => Debug.LogError($"{typeof(T).Name} error, instance initialization has most likely failed.");

        private static bool ValidateInstance()
        {
            if (BaseInstance != null) return true;

            T[] instances = FindObjectsOfType<T>();
            if (instances.Length == 0)
            {
                GameObject typeGameObject = new GameObject($"{typeof(T).Name}");
                ActivateInstance(typeGameObject.AddComponent<T>());
            }
            else if (instances.Length == 1)
            {
                ActivateInstance(instances[0]);
            }
            else if (instances.Length >= 2)
            {
                for (int i = 1; i < instances.Length; i++)
                {
                    Destroy(instances[i].gameObject);
                }

                ActivateInstance(instances[0]);
            }
            else if (BaseInstance == null)
            {
                return false;
            }

            return true;
        }

        private static void ActivateInstance(T newInstance)
        {
            BaseInstance = newInstance;
            newInstance.gameObject.SetActive(true);
            if (newInstance.transform.parent == null
                && Application.isPlaying)
            {
                //DontDestroyOnLoad(newInstance.gameObject);
            }
        }

        protected virtual void OnDestroy() => ApplicationIsClosing = true;

    #if UNITY_ANDROID || UNITY_IOS
    protected virtual void OnApplicationPause(bool hasPaused)
    {
        if (hasPaused)
        {
            ApplicationIsClosing = true;
        }
        else
        {
            ApplicationIsClosing = false;
        }
    }
    #endif
    }
}