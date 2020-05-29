using System;
using System.Collections;
using UnityEngine;
using Voxel.Game;
using Voxel.Saving;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Player
{
    public class PlayerBlockInteraction : MonoBehaviour
    {
        [SerializeField]
        private BlockType[] nonMineableBlockTypes = default;

        [SerializeField]
        private InputActionsController inputActionsController = default;
        [SerializeField]
        private float interactionMaxDistance = 2;

        private Vector3Int currentLocalBlockPosition;
        private Coroutine interactCoroutine;

        private int ChunkEdge => WorldManager.Instance.ChunkSize - 2;

        private void OnEnable()
        {
            GameManager.Instance.OnGameActiveStateChangeEvent += OnGameActiveStateChange;
            DisableInteractCoroutine();
            interactCoroutine = StartCoroutine(OnInteractPerformedCoroutine());
        }

        private void OnGameActiveStateChange(bool state)
        {
            if (state)
            {
                DisableInteractCoroutine();
                interactCoroutine = StartCoroutine(OnInteractPerformedCoroutine());
            }
            else
            {
                DisableInteractCoroutine();
            }
        }

        private IEnumerator OnInteractPerformedCoroutine()
        {
            while (enabled)
            {
                bool interactionTriggered = inputActionsController.InputActions.Player.Interact.triggered;
                if (GameManager.Instance.IsGamePaused)
                {
                    yield return new WaitUntil(() => !GameManager.Instance.IsGamePaused);
                    interactionTriggered = false;
                }

                if (interactionTriggered)
                {
                    Vector2 rayPosition = new Vector2(Screen.width / 2, Screen.height / 2);
                    Ray ray = ReferenceManager.Instance.MainCamera.ScreenPointToRay(rayPosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, interactionMaxDistance))
                    {
                        BlockHit(hit);
                    }
                }

                yield return null;
            }
        }
        private Vector3 asd;
        private void BlockHit(RaycastHit hit)
        {
            Vector3 hitChunkPosition = hit.transform.position;
            Vector3 worldBlockPosition = hit.point - (hit.normal / 2);
            currentLocalBlockPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(worldBlockPosition.x - hitChunkPosition.x),
                y = Mathf.RoundToInt(worldBlockPosition.y - hitChunkPosition.y),
                z = Mathf.RoundToInt(worldBlockPosition.z - hitChunkPosition.z)
            };

            Chunk localChunk = WorldManager.Instance.GetChunk(hitChunkPosition);
            if (localChunk != null)
            {
                Block localBlock = localChunk.GetChunkData()[currentLocalBlockPosition.x, currentLocalBlockPosition.y, currentLocalBlockPosition.z];
                asd = localBlock.BlockPositionAverage;
                Debug.Log(asd);
                Debug.DrawLine(transform.position, localBlock.BlockPositionAverage, Color.red, 5);
                if (IsPermittedBlock(localBlock))
                {
                    RebuildAffectedChunks(hitChunkPosition, localChunk);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(asd, 0.2f);
        }

        private bool IsPermittedBlock(Block block)
        {
            for (int i = 0; i < nonMineableBlockTypes.Length; i++)
            {
                if (nonMineableBlockTypes[i] == block.BlockType)
                {
                    return false;
                }
            }

            return true;
        }

        private void RebuildAffectedChunks(Vector3 hitChunkPosition, Chunk hitChunk)
        {
            RebuildChunk(hitChunk, setBlockType: true);
            RebuildNeighbouringChunks(hitChunkPosition);
        }

        private void RebuildNeighbouringChunks(Vector3 localChunkPosition)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;

            if (currentLocalBlockPosition.x == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x - chunkSize, localChunkPosition.y, localChunkPosition.z)));
            }
            if (currentLocalBlockPosition.x == ChunkEdge)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x + chunkSize, localChunkPosition.y, localChunkPosition.z)));
            }

            if (currentLocalBlockPosition.y == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y - chunkSize, localChunkPosition.z)));
            }
            if (currentLocalBlockPosition.y == ChunkEdge)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y + chunkSize, localChunkPosition.z)));
            }

            if (currentLocalBlockPosition.z == 0)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y, localChunkPosition.z - chunkSize)));
            }
            if (currentLocalBlockPosition.z == ChunkEdge)
            {
                RebuildNeighbourChunk(() => WorldManager.Instance.GetChunk(new Vector3(localChunkPosition.x, localChunkPosition.y, localChunkPosition.z + chunkSize)));
            }
        }

        private void RebuildNeighbourChunk(Func<Chunk> chunkGetMethod)
        {
            Chunk neighbourChunk = chunkGetMethod();
            if (neighbourChunk != null)
            {
                RebuildChunk(neighbourChunk, setBlockType: false);
            }
        }

        private void RebuildChunk(Chunk chunk, bool setBlockType)
        {
            if (setBlockType)
            {
                UpdateBlockType(chunk);
            }

            chunk.DestroyChunkMesh();
            chunk.BuildBlocks();
            SaveManager.Instance.Save(chunk);
        }

        private void UpdateBlockType(Chunk chunk)
        {
            Block chunkHitBlock = chunk.GetChunkData()[currentLocalBlockPosition.x, currentLocalBlockPosition.y, currentLocalBlockPosition.z];
            chunkHitBlock.UpdateBlockType(BlockType.Air);
            BlockType[,,] blockTypeData = chunk.GetBlockTypeData();
            blockTypeData[currentLocalBlockPosition.x, currentLocalBlockPosition.y, currentLocalBlockPosition.z] = BlockType.Air;
        }

        private void DisableInteractCoroutine()
        {
            if (interactCoroutine != null)
            {
                StopCoroutine(interactCoroutine);
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameActiveStateChangeEvent -= OnGameActiveStateChange;
            }
        }
    }
}