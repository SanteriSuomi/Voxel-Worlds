using System;
using System.Collections;
using UnityEngine;
using Voxel.Game;
using Voxel.Utility;
using Voxel.Utility.Pooling;
using Voxel.World;

namespace Voxel.Player
{
    public struct BlockActionData
    {
        public Chunk Chunk { get; }
        public Block Block { get; }
        public RaycastHit HitInfo { get; }

        public BlockActionData(Chunk chunk, Block block, RaycastHit hitInfo)
        {
            Chunk = chunk;
            Block = block;
            HitInfo = hitInfo;
        }
    }

    public class PlayerBlockInteraction : MonoBehaviour
    {
        [SerializeField]
        private InputActionsController inputActionsController = default;

        [SerializeField]
        private BlockType[] nonMineableBlockTypes = default;

        [SerializeField]
        private float interactionMaxDistance = 2;
        [SerializeField]
        private float destroyBlockMaxSpeed = 0.5f;

        private Vector3Int currentLocalBlockPosition;
        private Coroutine interactCoroutine;
        private WaitForSeconds destroyBlockWFS;
        private BlockType blockReplaceType;

        private bool canPerformDestroyBlock = true;
        private bool destroyBlockTriggered;

        private int ChunkEdge => WorldManager.Instance.ChunkSize - 2;

        private void Awake() => destroyBlockWFS = new WaitForSeconds(destroyBlockMaxSpeed);

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
            #region Mouse Start Hold Bugfix
            // TODO: find alternative solution to mouse input 1 at game start
            GameManager.Instance.MouseClick(GameManager.MouseEvents.MOUSEEVENTF_LEFTDOWN);
            GameManager.Instance.MouseClick(GameManager.MouseEvents.MOUSEEVENTF_LEFTUP);
            #endregion
            while (enabled)
            {
                CalculateCanDestroyBlock();
                bool placeBlockTriggered = inputActionsController.InputActions.Player.PlaceBlock.triggered;
                if (GameManager.Instance.IsGamePaused)
                {
                    yield return new WaitUntil(() => !GameManager.Instance.IsGamePaused);
                    DisableDestroyBlockInput();
                    placeBlockTriggered = false;
                }

                if (destroyBlockTriggered)
                {
                    StartCoroutine(WaitForDestroyBlockReactivation());
                    BlockAction(DamageBlock, true);
                }

                if (placeBlockTriggered)
                {
                    blockReplaceType = BlockType.Dirt;
                    BlockAction(BuildBlock, false);
                }

                yield return null;
            }
        }

        #region Destroy Block Input Methods
        private void CalculateCanDestroyBlock()
        {
            if (canPerformDestroyBlock)
            {
                float destroyBlockValue = inputActionsController.InputActions.Player.DestroyBlock.ReadValue<float>();
                destroyBlockTriggered = destroyBlockValue > 0;
            }
        }

        private IEnumerator WaitForDestroyBlockReactivation()
        {
            DisableDestroyBlockInput();
            yield return destroyBlockWFS;
            canPerformDestroyBlock = true;
        }

        private void DisableDestroyBlockInput()
        {
            canPerformDestroyBlock = false;
            destroyBlockTriggered = false;
        }
        #endregion

        #region Block Validation
        /// <summary>
        /// Activate the block world position detection and consequently the block validation.
        /// </summary>
        /// <param name="onValidatedAction">Action delegate that gets executed if block is succesfully validated.</param>
        /// <param name="checkPermission">Should the block be checked for permission? (using the nonMineableBlockTypes array).</param>
        private void BlockAction(Action<BlockActionData> onValidatedAction, bool checkPermission)
        {
            Vector2 rayPosition = new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = ReferenceManager.Instance.MainCamera.ScreenPointToRay(rayPosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, interactionMaxDistance))
            {
                ValidateBlock(hitInfo, onValidatedAction, checkPermission);
            }
        }

        private void ValidateBlock(RaycastHit hitInfo, Action<BlockActionData> onValidatedAction, bool checkPermission)
        {
            Vector3 hitChunkPosition = hitInfo.transform.position;
            Vector3 worldBlockPosition = hitInfo.point - (hitInfo.normal / 2);
            currentLocalBlockPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(worldBlockPosition.x - hitChunkPosition.x),
                y = Mathf.RoundToInt(worldBlockPosition.y - hitChunkPosition.y),
                z = Mathf.RoundToInt(worldBlockPosition.z - hitChunkPosition.z)
            };

            Chunk hitChunk = WorldManager.Instance.GetChunk(hitChunkPosition);
            if (hitChunk != null)
            {
                Block hitBlock = hitChunk.GetChunkData()[currentLocalBlockPosition.x, currentLocalBlockPosition.y, currentLocalBlockPosition.z];
                if (checkPermission && IsPermittedBlock(hitBlock))
                {
                    onValidatedAction(new BlockActionData(hitChunk, hitBlock, hitInfo));
                }
                else if (!checkPermission)
                {
                    onValidatedAction(new BlockActionData(hitChunk, hitBlock, hitInfo));
                }
            }
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
        #endregion

        #region Damage Block
        private void DamageBlock(BlockActionData data)
        {
            InstantiateHitDecal(data.Block);
            if (data.Block.DamageBlock())
            {
                RebuildNeighbouringChunks(data.Chunk);
            }
        }

        private static void InstantiateHitDecal(Block localBlock)
        {
            string decalDatabaseKey = localBlock.BlockPositionAverage.ToString();
            if (!WorldManager.Instance.HitDecalDatabase.ContainsKey(decalDatabaseKey))
            {
                HitDecalPool.Instance.Get().Activate(localBlock, decalDatabaseKey);
            }
        }

        private void RebuildNeighbouringChunks(Chunk chunk)
        {
            if (currentLocalBlockPosition.x == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Left));
            }
            if (currentLocalBlockPosition.x == ChunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Right));
            }

            if (currentLocalBlockPosition.y == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Bottom));
            }
            if (currentLocalBlockPosition.y == ChunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Top));
            }

            if (currentLocalBlockPosition.z == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Back));
            }
            if (currentLocalBlockPosition.z == ChunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(ChunkNeighbour.Front));
            }
        }

        private void RebuildNeighbourChunk(Func<Chunk> chunkGetMethod)
        {
            Chunk neighbourChunk = chunkGetMethod();
            neighbourChunk?.RebuildChunk((false, Vector3Int.zero));
        }
        #endregion

        private void BuildBlock(BlockActionData data)
        {
            // Offset the hit position so we're not replacing the hit block itself but rather the block next to it, as air blocks cannot be hit by raycast.
            Vector3Int adjustedBlockPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(data.Block.Position.x + data.HitInfo.normal.x),
                y = Mathf.RoundToInt(data.Block.Position.y + data.HitInfo.normal.y),
                z = Mathf.RoundToInt(data.Block.Position.z + data.HitInfo.normal.z)
            };

            
            if (adjustedBlockPosition.x == -1)
            {
                Debug.Log("Left");
                Chunk chunk = data.Chunk.GetChunkNeighbour(ChunkNeighbour.Left);
                Vector3Int reAdjustedBlockPosition = adjustedBlockPosition;
                reAdjustedBlockPosition.x = ChunkEdge;
                Debug.Log(reAdjustedBlockPosition);
                Block adjustedBlock = chunk.GetChunkData()[reAdjustedBlockPosition.x, reAdjustedBlockPosition.y, reAdjustedBlockPosition.z];
                if (adjustedBlock?.BlockType == BlockType.Air)
                {
                    adjustedBlock.ReplaceBlock(blockReplaceType);
                }
            }
            else
            {
                Block[,,] chunkData = data.Chunk.GetChunkData();
                if (adjustedBlockPosition.x >= 0 && adjustedBlockPosition.x <= chunkData.GetUpperBound(0)
                && adjustedBlockPosition.y >= 0 && adjustedBlockPosition.y <= chunkData.GetUpperBound(1)
                && adjustedBlockPosition.z >= 0 && adjustedBlockPosition.z <= chunkData.GetUpperBound(2))
                {
                    Block adjustedBlock = chunkData[adjustedBlockPosition.x, adjustedBlockPosition.y, adjustedBlockPosition.z];
                    if (adjustedBlock?.BlockType == BlockType.Air)
                    {
                        adjustedBlock.ReplaceBlock(blockReplaceType);
                    }
                }
            }
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