using UnityEngine;
using Voxel.Game;
using Voxel.Utility;

namespace Voxel.UI
{
    public class UIStateGameMainMenu : UIState
    {
        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            ReferenceManager.Instance.Crosshair.gameObject.SetActive(false);
            GameManager.Instance.Pause();
        }

        protected override void OnStateDisable()
        {
            GameManager.Instance.Resume();
            GameManager.Instance.MouseClick(GameManager.MouseEvents.MOUSEEVENTF_LEFTDOWN);
        }
    }
}