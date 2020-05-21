using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField]
        private InputActionsController inputActionsController = default;

        [SerializeField]
        private float interactionMaxDistance = 4;

        private void OnEnable()
        {
            inputActionsController.InputActions.Player.Interact.performed += OnInteractPerformed;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            Camera mainCam = ReferenceManager.Instance.MainCamera;
            Vector2 rayPosition = new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = mainCam.ScreenPointToRay(rayPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionMaxDistance))
            {
                BlockHit(hit);
            }
        }

        private static void BlockHit(RaycastHit hit)
        {
            Vector3 blockMidPoint = hit.point - (hit.normal / 2);
            Vector3Int blockWorldPosition = new Vector3Int
            {
                x = (int)(blockMidPoint.x - hit.collider.transform.position.x),
                y = (int)(blockMidPoint.y - hit.collider.transform.position.y),
                z = (int)(blockMidPoint.z - hit.collider.transform.position.z)
            };

            Chunk chunk = WorldManager.Instance.GetChunkFromID(WorldManager.Instance.GetChunkID(hit.collider.transform.position));
            if (chunk != null)
            {
                DestroyImmediate(chunk.MeshFilter);
                DestroyImmediate(chunk.MeshRenderer);
                DestroyImmediate(chunk.Collider);
                Block hitBlock = chunk.GetChunkData()[blockWorldPosition.x, blockWorldPosition.y, blockWorldPosition.z];
                hitBlock.SetType(BlockType.Air);
                chunk.BuildBlocks();
            }
        }

        private void OnDisable()
        {
            inputActionsController.InputActions.Player.Interact.performed -= OnInteractPerformed;
        }
    }
}