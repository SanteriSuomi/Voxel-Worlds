using System.Collections.Generic;
using UnityEngine;

namespace Voxel.UI
{
    public abstract class UIState : MonoBehaviour
    {
        [SerializeField]
        protected List<GameObject> uiStateComponents;

        public void Enable()
        {
            LoopComponents(enable : true);
            OnStateEnable();
        }

        /// <summary>
        /// What happens when a state gets enabled
        /// </summary>
        protected virtual void OnStateEnable()
        {
        }

        public void Disable()
        {
            LoopComponents(enable: false);
            OnStateDisable();
        }

        /// <summary>
        /// What happens when a state gets disabled
        /// </summary>
        protected virtual void OnStateDisable()
        {
        }

        private void LoopComponents(bool enable)
        {
            for (int i = 0; i < uiStateComponents.Count; i++)
            {
                uiStateComponents[i].SetActive(enable);
            }
        }
    }
}