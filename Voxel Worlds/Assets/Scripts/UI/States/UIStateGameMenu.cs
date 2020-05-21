using UnityEngine;
using UnityEngine.Events;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIStateGameMenu : UIState
    {
        [SerializeField]
        private GameObject uiCamera = default;
        [SerializeField]
        private GameObject crosshair = default;

        private void Awake()
        {
            UnityAction buildWorldComplete = new UnityAction(DisableMenuCamera);
            EventManager.Listen("BuildWorldComplete", buildWorldComplete);
        }

        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            crosshair.SetActive(true);
        }

        private void DisableMenuCamera()
        {
            uiCamera.SetActive(false);
        }
    }
}