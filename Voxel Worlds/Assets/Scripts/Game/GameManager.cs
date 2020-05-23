using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.Game
{
    public class GameManager : Singleton<GameManager>
    {
        public bool IsGameRunning { get; private set; }
        public bool IsGamePaused => Mathf.Approximately(Time.deltaTime, 0);

        /// <summary>
        /// Import the mouse_event to simulate mouse clicks.
        /// </summary>
        /// <param name="dwFlags">Mouse event signature</param>
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x01000;

        public void MouseClick(uint signature) => mouse_event(signature, 0, 0, 0, new UIntPtr(0));

        public void Pause() => Time.timeScale = 0;

        public void Resume() => Time.timeScale = 1;

        private void OnEnable() => IsGameRunning = true;

        private void OnDisable() => IsGameRunning = false;
    }
}