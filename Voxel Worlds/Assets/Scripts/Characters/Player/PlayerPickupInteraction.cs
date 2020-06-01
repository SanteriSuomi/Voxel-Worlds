using UnityEngine;
using Voxel.Items;

namespace Voxel.Player
{
    public class PlayerPickupInteraction : MonoBehaviour
    {
        private void OnTriggerEnter(Collider hit)
        {
            if (hit.gameObject.TryGetComponent(out IPickupable pickup))
            {
                pickup.Pickup();
            }
        }
    }
}