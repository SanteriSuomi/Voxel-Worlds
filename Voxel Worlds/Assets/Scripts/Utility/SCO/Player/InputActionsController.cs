using UnityEngine;
using Voxel.Input;

namespace Voxel.Utility
{
    [CreateAssetMenu(fileName = "Input Actions Controller", order = 0)]
    public class InputActionsController : ScriptableObject
    {
        private InputActions inputActions;
        public InputActions InputActions
        {
            get
            {
                return inputActions ?? (inputActions = new InputActions());
            }
        }

        private void OnEnable()
        {
            InputActions.Enable();
        }

        private void OnDisable()
        {
            InputActions.Disable();
        }
    }
}