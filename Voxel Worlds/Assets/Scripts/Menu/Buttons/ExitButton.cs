using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Voxel.Player;

namespace Voxel.UI.Menu.Buttons
{
    public class ExitButton : Button
    {
        private void Awake()
        {
            AddOnClickAction(() =>
            {
                PlayerManager.Instance.Save();

                #if UNITY_EDITOR
                if (Application.isEditor)
                {
                    EditorApplication.isPlaying = false;
                    return;
                }
                #endif

                Process.GetCurrentProcess().Kill();
            });
        }
    }
}