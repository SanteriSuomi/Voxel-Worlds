using System;
using System.Collections;
using UnityEngine;
using Voxel.Game;
using Voxel.Saving;
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

        // Used in build block UpdateBlock solely
        public Vector3Int AdjustedBlockPosition { get; set; }

        public BlockActionData(Chunk chunk, Block block, RaycastHit hitInfo)
        {
            Chunk = chunk;
            Block = block;
            HitInfo = hitInfo;
            AdjustedBlockPosition = Vector3Int.zero;
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
        private WaitUntil gameIsPausedWU;
        private BlockType blockReplaceType;

        private bool canPerformDestroyBlock = true;
        private bool destroyBlockTriggered;

        private int ChunkEdge => WorldManager.Instance.ChunkSize - 2;

        private void Awake()
        {
            destroyBlockWFS = new WaitForSeconds(destroyBlockMaxSpeed);
            gameIsPausedWU = new WaitUntil(() => !GameManager.Instance.IsGamePaused);
        }

        private void OnEnable()
        {
            GameManager.Instance.OnGameActiveStateChangeEvent += OnGameActiveStateChange;
            DisableInteractCoroutine();
            LeftMouseClick();
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
                CalculateCanDestroyBlock();
                bool placeBlockTriggered = inputActionsController.InputActions.Player.PlaceBlock.triggered;
                if (GameManager.Instance.IsGamePaused)
                {
                    yield return gameIsPausedWU;
                    LeftMouseClick();
                    canPerformDestroyBlock = true;
                    destroyBlockTriggered = false;
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

        // TODO: Simulate a mouse left click. Used here for fixing a bug related to the destroy block hold. Temporary solution.
        private static void LeftMouseClick()
        {
            GameManager.Instance.MouseClick(GameManager.MouseEvents.MOUSEEVENTF_LEFTDOWN);
            GameManager.Instance.MouseClick(GameManager.MouseEvents.MOUSEEVENTF_LEFTUP);
        }

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

            data.AdjustedBlockPosition = adjustedBlockPosition;
            if (adjustedBlockPosition.x == -1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Left, new Vector3Int(ChunkEdge + 1, 0, 0)));
            }
            else if (adjustedBlockPosition.x == ChunkEdge + 1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Right, new Vector3Int(-(ChunkEdge + 1), 0, 0)));
            }
            else if (adjustedBlockPosition.y == -1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Bottom, new Vector3Int(0, ChunkEdge + 1, 0)));
            }
            else if (adjustedBlockPosition.y == ChunkEdge + 1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Top, new Vector3Int(0, -(ChunkEdge + 1), 0)));
            }
            else if (adjustedBlockPosition.z == -1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Back, new Vector3Int(0, 0, ChunkEdge + 1)));
            }
            else if (adjustedBlockPosition.z == ChunkEdge + 1)
            {
                UpdateBlock(data, (true, ChunkNeighbour.Front, new Vector3Int(0, 0, -(ChunkEdge + 1))));
            }
            else
            {
                // Update local block
                UpdateBlock(data, (false, 0, Vector3Int.zero));
            }
        }

        private void UpdateBlock(BlockActionData data, (bool updateNeighbour, ChunkNeighbour neighbour, Vector3Int offset) updateData)
        {
            Chunk chunk = data.Chunk;
            Vector3Int reAdjustedBlockPosition = data.AdjustedBlockPosition;
            if (updateData.updateNeighbour)
            {
                chunk = data.Chunk.GetChunkNeighbour(updateData.neighbour);
                reAdjustedBlockPosition += updateData.offset;
            }

            Block[,,] chunkData = chunk.GetChunkData();
            if (reAdjustedBlockPosition.x >= 0 && reAdjustedBlockPosition.x <= chunkData.GetUpperBound(0)
                && reAdjustedBlockPosition.y >= 0 && reAdjustedBlockPosition.y <= chunkData.GetUpperBound(1)
                && reAdjustedBlockPosition.z >= 0 && reAdjustedBlockPosition.z <= chunkData.GetUpperBound(2))
            {
                Block adjustedBlock = chunkData[reAdjustedBlockPosition.x, reAdjustedBlockPosition.y, reAdjustedBlockPosition.z];
                if (adjustedBlock?.BlockType == BlockType.Air)
                {
                    adjustedBlock.ReplaceBlock(blockReplaceType);
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