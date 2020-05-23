using UnityEngine;
using UnityEngine.Events;

namespace Voxel.UI.Menu.Buttons
{
    public class Button : MonoBehaviour
    {
        [SerializeField]
        protected UnityEngine.UI.Button button = default;

        /// <summary>
        /// Execute given action/delegate on button click.
        /// </summary>
        /// <param name="action"></param>
        protected void AddOnClickAction(UnityAction action) => button.onClick.AddListener(action);
    }
}