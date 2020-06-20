using Voxel.Saving;

namespace Voxel.UI.Menu.Buttons
{
    public class SaveButton : Button
    {
        private void Awake() => AddOnClickAction(SaveManager.Instance.SaveAll);
    }
}