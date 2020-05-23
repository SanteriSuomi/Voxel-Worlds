using Voxel.Player;

namespace Voxel.UI.Menu.Buttons
{
    public class SaveButton : Button
    {
        private void Awake() => AddOnClickAction(() => PlayerManager.Instance.SavePlayer());
    }
}