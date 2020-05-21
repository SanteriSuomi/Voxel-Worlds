using UnityEngine;

namespace Voxel.Utility
{
    public class ReferenceManager : Singleton<ReferenceManager>
    {
        public Camera MainCamera { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            EventManager.Listen("BuildWorldComplete", () => MainCamera = Camera.main);
        }
    }
}