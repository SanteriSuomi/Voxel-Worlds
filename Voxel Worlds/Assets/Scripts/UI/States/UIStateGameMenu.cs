using UnityEngine;
using UnityEngine.Events;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIStateGameMenu : UIState
    {
        [SerializeField]
        private GameObject uiCamera = default;

        private void Awake()
        {
            UnityAction buildWorldComplete = new UnityAction(DisableMenuCamera);
            EventManager.Listen("BuildWorldComplete", buildWorldComplete);
        }

        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void DisableMenuCamera()
        {
            uiCamera.SetActive(false);
        }
    }
}