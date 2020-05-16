using UnityEngine;

namespace Voxel.Utility
{
    [CreateAssetMenu(fileName = "Input Actions Controller", order = 0)]
    public class InputActionsController : ScriptableObject
    {
        private inputActionsController inputActions;
        public inputActionsController InputActions
        {
            get
            {
                return inputActions ?? (inputActions = new inputActionsController());
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