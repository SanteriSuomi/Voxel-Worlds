using UnityEngine;
using Voxel.Utility;
using UnityEngine.InputSystem;
using Voxel.Game;

namespace Voxel.UI.Menu
{
    public class MenuManager : Singleton<MenuManager>
    {
        [SerializeField]
        private InputActionsController inputActionsController = default;
        [SerializeField]
        private UIState gameMainMenuState = default;
        [SerializeField]
        private UIState gameMenuState = default;

        private void OnEnable()
        {
            inputActionsController.InputActions.Player.Menu.performed += OnMenuPerformed;
        }

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            if (UIManager.Instance.CurrentState != (gameMainMenuState || gameMenuState))
            {
                return;
            }

            if (GameManager.Instance.IsGamePaused)
            {
                UIManager.Instance.ActivateState(gameMenuState);
            }
            else
            {
                UIManager.Instance.ActivateState(gameMainMenuState);
            }
        }

        private void OnDisable()
        {
            inputActionsController.InputActions.Player.Menu.performed -= OnMenuPerformed;
        }
    }
}