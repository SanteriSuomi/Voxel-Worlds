using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Utility.Pooling
{
    public class ObjectPool<T> : Singleton<ObjectPool<T>> where T : Component
    {
        private Queue<T> pool;
        private Transform parent;
        [SerializeField]
        private T prefabToPool = default;
        [SerializeField]
        private int poolSize = 25;

        protected override void Awake()
        {
            base.Awake();
            InitializePool();
        }

        private void InitializePool()
        {
            pool = new Queue<T>(poolSize);
            AddParent();
            for (int i = 0; i < poolSize; i++)
            {
                NewPoolObject();
            }
        }

        private void AddParent()
        {
            string parentName = $"{typeof(T).Name} Pool Objects";
            if (GameObject.Find(parentName) == null)
            {
                parent = new GameObject(parentName).transform;
                //DontDestroyOnLoad(parent);
            }
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <returns>The object that's pooled.</returns>
        public T Get()
        {
            CheckObjectValidity(pool.Peek());

            T obj = pool.Dequeue();
            Activate(obj, true);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="obj"></param>
        public void Return(T obj)
        {
            Activate(obj, false);
            pool.Enqueue(obj);
        }

        private void CheckObjectValidity(T obj)
        {
            if (obj is null)
            {
                NewPoolObject();
            }
            else if (obj.gameObject.activeSelf)
            {
                NewPoolObject();
            }
        }

        private void NewPoolObject()
        {
            T obj = Instantiate(prefabToPool);
            Activate(obj, false);
            obj.transform.SetParent(parent);
            pool.Enqueue(obj);
        }

        private void Activate(T obj, bool value)
            => obj.gameObject.SetActive(value);
    }
}