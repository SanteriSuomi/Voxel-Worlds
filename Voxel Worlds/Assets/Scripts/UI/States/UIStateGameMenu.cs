using UnityEngine;
using UnityEngine.Events;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIStateGameMenu : UIState
    {
        [SerializeField]
        private GameObject inventory = default;

        private void Awake()
        {
            UnityAction buildWorldComplete = new UnityAction(DisableMenuCamera);
            EventManager.Listen("BuildWorldComplete", buildWorldComplete);
        }

        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            ReferenceManager.Instance.Crosshair.gameObject.SetActive(true);
            inventory.SetActive(true);
        }

        protected override void OnStateDisable() => inventory.SetActive(false);

        private void DisableMenuCamera() => ReferenceManager.Instance.UICamera.gameObject.SetActive(false);
    }
}