using System;
using System.Collections;
using UnityEngine;
using Voxel.Game;
using Voxel.Items;
using Voxel.Items.Inventory;
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

        // Used in build block BuildBlock solely
        public Vector3Int AdjustedBlockPosition { get; set; }

        public BlockActionData(Chunk chunk, Block block, RaycastHit hitInfo)
        {
            Chunk = chunk;
            Block = block;
            HitInfo = hitInfo;
            AdjustedBlockPosition = Vector3Int.zero;
        }
    }

    public struct BlockUpdateData
    {
        public bool Update { get; }
        public Neighbour Neighbour { get; }
        public Vector3Int Offset { get; }

        public BlockUpdateData(bool update, Neighbour neighbour, Vector3Int position)
        {
            Update = update;
            Neighbour = neighbour;
            Offset = position;
        }

        public static BlockUpdateData GetEmpty() => new BlockUpdateData(false, 0, Vector3Int.zero);
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
        [SerializeField]
        private Vector3 blockDestroyScale = new Vector3(0.35f, 0.35f, 0.35f);

        private Vector3Int currentLocalBlockPosition;
        private Coroutine interactCoroutine;
        private WaitForSeconds destroyBlockWFS;
        private WaitUntil gameIsPausedWU;

        private bool canPerformDestroyBlock = true;
        private bool destroyBlockTriggered;

        // Does the player have valid block selected from inventory?
        private bool validBlockSelected;
        private BlockType selectedBlockType;

        private void Awake()
        {
            destroyBlockWFS = new WaitForSeconds(destroyBlockMaxSpeed);
            gameIsPausedWU = new WaitUntil(() => !GameManager.Instance.IsGamePaused);
        }

        private void OnEnable()
        {
            GameManager.Instance.OnGameActiveStateChangeEvent += OnGameActiveStateChange;
            Slot.OnSelectedItemChangedEvent += OnInventorySelectedItemChanged;
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

        private void OnInventorySelectedItemChanged(SelectedItemData selectedItemData)
        {
            validBlockSelected = selectedItemData.ValidItemSelected;
            selectedBlockType = selectedItemData.SelectedBlockType;
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

                if (placeBlockTriggered && validBlockSelected)
                {
                    BlockAction(BuildBlock, false);
                    InventoryManager.Instance.Remove(selectedBlockType);
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

        private void DisableInteractCoroutine()
        {
            if (interactCoroutine != null)
            {
                StopCoroutine(interactCoroutine);
            }
        }
        #endregion

        // TODO: Simulate a mouse left click. Used here for fixing a bug related to the destroy block hold. Possible find another solution?
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
            BlockType damagedBlockType = data.Block.BlockType;
            SetHitDecal(data.Block);
            if (data.Block.DamageBlock())
            {
                // On block destroy
                var outputData = Block.InstantiateWorldBlock<BoxCollider, BlockPickup>(new InstantiateBlockInputData(damagedBlockType,
                                                                                                                     data.Block.WorldPositionAverage,
                                                                                                                     blockDestroyScale));
                outputData.Obj.BlockType = damagedBlockType;
                RebuildNeighbouringChunks(data.Chunk);
            }
        }

        private static void SetHitDecal(Block localBlock)
        {
            string decalDatabaseKey = localBlock.WorldPositionAverage.ToString();
            if (!WorldManager.Instance.HitDecalDatabase.ContainsKey(decalDatabaseKey))
            {
                HitDecalPool.Instance.Get().Activate(localBlock, decalDatabaseKey);
            }
        }

        private void RebuildNeighbouringChunks(Chunk chunk)
        {
            int chunkEdge = WorldManager.Instance.ChunkEdge;
            if (currentLocalBlockPosition.x == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Left));
            }
            if (currentLocalBlockPosition.x == chunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Right));
            }

            if (currentLocalBlockPosition.y == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Bottom));
            }
            if (currentLocalBlockPosition.y == chunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Top));
            }

            if (currentLocalBlockPosition.z == 0)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Back));
            }
            if (currentLocalBlockPosition.z == chunkEdge)
            {
                RebuildNeighbourChunk(() => chunk.GetChunkNeighbour(Neighbour.Front));
            }
        }

        private void RebuildNeighbourChunk(Func<Chunk> chunkGetMethod)
        {
            Chunk neighbourChunk = chunkGetMethod();
            neighbourChunk?.RebuildChunk(ChunkResetData.GetEmpty());
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

            int chunkEdge = WorldManager.Instance.ChunkEdge + 1;
            data.AdjustedBlockPosition = adjustedBlockPosition;
            if (adjustedBlockPosition.x == -1)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Left, new Vector3Int(chunkEdge, 0, 0)));
            }
            else if (adjustedBlockPosition.x == chunkEdge)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Right, new Vector3Int(-chunkEdge, 0, 0)));
            }
            else if (adjustedBlockPosition.y == -1)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Bottom, new Vector3Int(0, chunkEdge, 0)));
            }
            else if (adjustedBlockPosition.y == chunkEdge)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Top, new Vector3Int(0, -chunkEdge, 0)));
            }
            else if (adjustedBlockPosition.z == -1)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Back, new Vector3Int(0, 0, chunkEdge)));
            }
            else if (adjustedBlockPosition.z == chunkEdge)
            {
                UpdateBlock(data, new BlockUpdateData(true, Neighbour.Front, new Vector3Int(0, 0, -chunkEdge)));
            }
            else
            {
                // Update local block
                UpdateBlock(data, BlockUpdateData.GetEmpty());
            }
        }

        private void UpdateBlock(BlockActionData actionData, BlockUpdateData updateData)
        {
            Chunk chunk = actionData.Chunk;
            Vector3Int reAdjustedBlockPosition = actionData.AdjustedBlockPosition;
            if (updateData.Update)
            {
                chunk = actionData.Chunk.GetChunkNeighbour(updateData.Neighbour);
                reAdjustedBlockPosition += updateData.Offset;
            }

            Block[,,] chunkData = chunk.GetChunkData();
            if (reAdjustedBlockPosition.x >= 0 && reAdjustedBlockPosition.x <= chunkData.GetUpperBound(0)
                && reAdjustedBlockPosition.y >= 0 && reAdjustedBlockPosition.y <= chunkData.GetUpperBound(1)
                && reAdjustedBlockPosition.z >= 0 && reAdjustedBlockPosition.z <= chunkData.GetUpperBound(2))
            {
                Block adjustedBlock = chunkData[reAdjustedBlockPosition.x, reAdjustedBlockPosition.y, reAdjustedBlockPosition.z];
                if (adjustedBlock?.BlockType == BlockType.Air)
                {
                    adjustedBlock.ReplaceBlock(selectedBlockType);
                }
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameActiveStateChangeEvent -= OnGameActiveStateChange;
            }

            Slot.OnSelectedItemChangedEvent -= OnInventorySelectedItemChanged;
        }
    }
}