using UnityEngine;
using UnityEngine.Events;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIStateGameMenu : UIState
    {
        private void Awake()
        {
            UnityAction buildWorldComplete = new UnityAction(DisableMenuCamera);
            EventManager.Listen("BuildWorldComplete", buildWorldComplete);
        }

        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            ReferenceManager.Instance.Crosshair.gameObject.SetActive(true);
        }

        private void DisableMenuCamera()
        {
            ReferenceManager.Instance.UICamera.gameObject.SetActive(false);
        }
    }
}