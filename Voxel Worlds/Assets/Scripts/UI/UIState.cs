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
        /// What happens OnEnable
        /// </summary>
        protected virtual void OnStateEnable()
        {
        }

        public void Disable()
        {
            LoopComponents(enable: false);
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