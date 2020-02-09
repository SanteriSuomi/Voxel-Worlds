using UnityEngine;

namespace Voxel.UI
{
    public class UIStateGameMenu : UIState
    {
        [SerializeField]
        private GameObject uiCamera = default;

        protected override void OnStateEnable()
        {
            uiCamera.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}