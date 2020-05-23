using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIManager : Singleton<UIManager>
    {
        private List<UIState> uiStates;
        [SerializeField]
        private UIState startingState = default;
        public UIState CurrentState { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            uiStates = GetComponentsInChildren<UIState>().ToList();
            ActivateState(startingState);
        }

        public void ActivateState(UIState newState)
        {
            for (int i = 0; i < uiStates.Count; i++)
            {
                UIState currentState = uiStates[i];
                if (newState == currentState)
                {
                    currentState.Enable();
                    CurrentState = currentState;
                    continue;
                }

                currentState.Disable();
            }
        }
    }
}