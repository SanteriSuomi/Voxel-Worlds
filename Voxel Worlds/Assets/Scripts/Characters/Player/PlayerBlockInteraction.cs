using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Game;
using Voxel.Saving;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Player
{
    public class PlayerBlockInteraction : MonoBehaviour
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
            if (GameManager.Instance.IsGamePaused) return;

            Camera mainCam = ReferenceManager.Instance.MainCamera;
            Vector2 rayPosition = new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = mainCam.ScreenPointToRay(rayPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionMaxDistance))
            {
                BlockHit(hit);
            }
        }

        private void BlockHit(RaycastHit hit)
        {
            Vector3 chunkPosition = hit.transform.position;
            Vector3 blockMidPoint = hit.point - (hit.normal / 2.0f);
            Vector3Int blockWorldPosition = new Vector3Int
            {
                x = (int)(blockMidPoint.x - chunkPosition.x),
                y = (int)(blockMidPoint.y - chunkPosition.y),
                z = (int)(blockMidPoint.z - chunkPosition.z)
            };

            Chunk chunk = WorldManager.Instance.GetChunkFromID(WorldManager.Instance.GetChunkID(chunkPosition));
            if (chunk != null)
            {
                DestroyImmediate(chunk.MeshFilter);
                DestroyImmediate(chunk.MeshRenderer);
                DestroyImmediate(chunk.Collider);
                Block chunkHitBlock = chunk.GetChunkData()[blockWorldPosition.x, blockWorldPosition.y, blockWorldPosition.z];
                chunkHitBlock.SetType(BlockType.Air);
                chunk.BuildBlocks();
                StartCoroutine(SaveManager.Instance.Save(chunk));
            }
        }

        private void OnDisable()
        {
            inputActionsController.InputActions.Player.Interact.performed -= OnInteractPerformed;
        }
    }
}