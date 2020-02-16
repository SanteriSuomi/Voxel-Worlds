using UnityEngine;

namespace Voxel.Utility
{
    public abstract class BaseScriptableObject<T> : ScriptableObject
    {
        [SerializeField]
        protected T value = default;
        public virtual T Value
        {
            get { return value; }
            set
            {
                ValueChangedEvent?.Invoke(value);
                this.value = value;
            }
        }
        public delegate void ValueChanged(T value);
        /// <summary>
        /// This event gets invoked when the value of this SCO changes.
        /// </summary>
        public event ValueChanged ValueChangedEvent;

        [SerializeField]
        protected bool resetValueOnStart = true;
        [SerializeField]
        protected T valueToResetTo = default;
        public T OriginalValue { get { return valueToResetTo; } }

        protected virtual void OnEnable()
        {
            if (resetValueOnStart)
            {
                value = valueToResetTo;
            }
        }
    }
}