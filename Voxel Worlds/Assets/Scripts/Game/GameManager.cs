using DG.Tweening;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Voxel.Utility;

namespace Voxel.Game
{
    public class GameManager : Singleton<GameManager>
    {
        public delegate void OnGameActiveStateChange(bool state);
        public event OnGameActiveStateChange OnGameActiveStateChangeEvent;

        public bool IsGameRunning { get; private set; }
        public bool IsGamePaused => Mathf.Approximately(Time.deltaTime, 0);

        protected override void Awake() => DOTween.Init(true, true, LogBehaviour.ErrorsOnly);

        private void OnEnable() => IsGameRunning = true;

        /// <summary>
        /// Import the mouse_event to simulate mouse clicks.
        /// </summary>
        /// <param name="dwFlags">Mouse event signature</param>
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        public enum MouseEvents
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_HWHEEL = 0x01000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }

        public void MouseClick(MouseEvents mouseEvent) => mouse_event((uint)mouseEvent, 0, 0, 0, new UIntPtr(0));

        public void Pause()
        {
            OnGameActiveStateChangeEvent?.Invoke(false);
            Time.timeScale = 0;
        }

        public void Resume()
        {
            OnGameActiveStateChangeEvent?.Invoke(true);
            Time.timeScale = 1;
        }

        private void OnDisable() => IsGameRunning = false;
    }
}