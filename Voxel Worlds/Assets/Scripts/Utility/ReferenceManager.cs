using UnityEngine;

namespace Voxel.Utility
{
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
    }
}