using System;
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
        private float interactionMaxDistance = 2;

        private Vector3Int localBlockPosition;

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
            Vector3 hitChunkPosition = hit.transform.position;
            Vector3 worldBlockPosition = hit.point - (hit.normal / 2);
            localBlockPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(worldBlockPosition.x - hitChunkPosition.x),
                y = Mathf.RoundToInt(worldBlockPosition.y - hitChunkPosition.y),
                z = Mathf.RoundToInt(worldBlockPosition.z - hitChunkPosition.z)
            };

            Chunk localChunk = WorldManager.Instance.GetChunk(hitChunkPosition);
            if (localChunk != null)
            {
                RebuildAffectedChunks(hitChunkPosition, localChunk);
            }
        }

        private void RebuildAffectedChunks(Vector3 hitChunkPosition, Chunk localChunk)
        {
            Block[,,] chunkData = localChunk.GetChunkData();
            if (localBlockPosition.x >= 0 && localBlockPosition.x <= chunkData.GetUpperBound(0)
                && localBlockPosition.y >= 0 && localBlockPosition.y <= chunkData.GetUpperBound(1)
                && localBlockPosition.z >= 0 && localBlockPosition.z <= chunkData.GetUpperBound(2))
            {
                RebuildChunk(localChunk, true);
                RebuildNeighbouringChunks(hitChunkPosition);
            }
        }

        private void RebuildNeighbouringChunks(Vector3 localChunkPosition)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            if (localBlockPosition.x == chunkSize)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x + chunkSize, localChunkPosition.y, localChunkPosition.z)));
            }

            if (localBlockPosition.x == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x - chunkSize, localChunkPosition.y, localChunkPosition.z)));
            }

            if (localBlockPosition.y == chunkSize)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y + chunkSize, localChunkPosition.z)));
            }

            if (localBlockPosition.y == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y - chunkSize, localChunkPosition.z)));
            }

            if (localBlockPosition.z == chunkSize)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y, localChunkPosition.z + chunkSize)));
            }

            if (localBlockPosition.z == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y, localChunkPosition.z - chunkSize)));
            }
        }

        private void RebuildNeighbourChunk(Func<Chunk> chunkGetMethod)
        {
            Chunk neighbourChunk = chunkGetMethod();
            if (neighbourChunk != null)
            {
                Debug.Log(neighbourChunk.GameObject.transform.position);
                RebuildChunk(neighbourChunk, true);
            }
        }

        private void RebuildChunk(Chunk chunk, bool setBlock)
        {
            if (setBlock)
            {
                Block[,,] chunkData = chunk.GetChunkData();
                Block chunkHitBlock = chunkData[localBlockPosition.x, localBlockPosition.y, localBlockPosition.z];
                chunkHitBlock.SetType(BlockType.Air);
            }

            chunk.DestroyChunkData();
            chunk.BuildBlocks();
            SaveManager.Instance.Save(chunk);
        }

        private void OnDisable()
        {
            inputActionsController.InputActions.Player.Interact.performed -= OnInteractPerformed;
        }
    }
}