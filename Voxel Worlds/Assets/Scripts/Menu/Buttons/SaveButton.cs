using Voxel.Items.Inventory;
using Voxel.Player;

namespace Voxel.UI.Menu.Buttons
{
    public class SaveButton : Button
    {
        private void Awake() => AddOnClickAction(SavePlayerState);

        private void SavePlayerState()
        {
            PlayerManager.Instance.Save();
            InventoryManager.Instance.Save();
        }
    }
}