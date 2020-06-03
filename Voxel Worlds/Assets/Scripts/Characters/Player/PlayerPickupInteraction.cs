using UnityEngine;
using Voxel.Items;

namespace Voxel.Player
{
    public class PlayerPickupInteraction : MonoBehaviour
    {
        [SerializeField]
        private Transform scanStartLocation = default;
        [SerializeField]
        private LayerMask pickupDetectionLayers = default;
        [SerializeField]
        private string pickupDetectionTag = "Pickup";

        [SerializeField]
        private float pickupRadius = 1;

        private readonly Collider[] collisions = new Collider[5];

        private void Update() => ScanRadiusForPickups();

        private void ScanRadiusForPickups()
        {
            int hitAmount = Physics.OverlapSphereNonAlloc(scanStartLocation.position, pickupRadius, collisions, pickupDetectionLayers);
            if (hitAmount > 0)
            {
                for (int i = 0; i < collisions.Length; i++)
                {
                    if (collisions[i] != null)
                    {
                        CheckCollisionIndex(collisions[i]);
                    }
                }
            }
        }

        private void CheckCollisionIndex(Collider collider)
        {
            if (collider.transform != null
                && collider.transform.childCount > 0)
            {
                // Pickup script is contained in a child object of the the pickup parent
                Transform child = collider.transform.GetChild(0);
                if (collider.CompareTag(pickupDetectionTag)
                    && child.TryGetComponent(out IPickupable pickup))
                {
                    pickup.Pickup();
                }
            }
        }
    }
}