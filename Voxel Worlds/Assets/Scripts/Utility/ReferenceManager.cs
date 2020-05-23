﻿using UnityEngine;
using UnityEngine.UI;

namespace Voxel.Utility
{
    /// <summary>
    /// ReferenceManager contains often used references so they do not need to be found for every class separately.
    /// </summary>
    public class ReferenceManager : Singleton<ReferenceManager>
    {

        private Camera mainCamera;
        public Camera MainCamera
        {
            get
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }

                return mainCamera;
            }
        }

        [SerializeField]
        private Camera uiCamera = default;
        public Camera UICamera => uiCamera;

        [SerializeField]
        private Image crosshair = default;
        public Image Crosshair => crosshair;
    }
}