using UnityEngine;
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
        private Material blockAtlas = default;
        public Material BlockAtlas => blockAtlas;

        [SerializeField]
        private Material blockAtlasTransparent = default;
        public Material BlockAtlasTransparent => blockAtlasTransparent;

        [SerializeField]
        private Camera uiCamera = default;
        public Camera UICamera => uiCamera;

        [SerializeField]
        private RawImage crosshair = default;
        public RawImage Crosshair => crosshair;

        private WaitForSeconds blockPickupWFS;
        public WaitForSeconds GetBlockPickupWFS(float time)
        {
            if (blockPickupWFS == null)
            {
                blockPickupWFS = new WaitForSeconds(time);
            }

            return blockPickupWFS;
        }
    }
}