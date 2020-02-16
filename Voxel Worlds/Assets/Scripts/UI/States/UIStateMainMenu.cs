using UnityEngine;

namespace Voxel.UI
{
    public class UIStateMainMenu : UIState
    {
        protected override void OnStateEnable()
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}