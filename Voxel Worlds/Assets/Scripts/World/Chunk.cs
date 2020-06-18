using System.Collections.Generic;
using UnityEngine;
using Voxel.Characters.Enemy;
using Voxel.Saving;
using Voxel.Utility;

namespace Voxel.World
{
    public enum ChunkStatus
    {
        None,
        Draw,
        Done,
        Keep
    }

    public struct ChunkResetData
    {
        /// <summary>
        /// Perform a reset on the block at the specified position?
        /// </summary>
        public bool ResetBlock { get; }
        public Vector3Int Position { get; }

        public ChunkResetData(bool reset, Vector3Int position)
        {
            ResetBlock = reset;
            Position = position;
        }

        public static ChunkResetData GetEmpty() => new ChunkResetData(false, Vector3Int.zero);
    }

    public class Chunk
    {
        public ChunkStatus ChunkStatus { get; set; }

        public GameObject BlockGameObject { get; }
        public GameObject FluidGameObject { get; }
        public GameObject VegetationGameObject { get; }

        public MeshFilter[] MeshFilters { get; }
        public MeshRenderer[] MeshRenderers { get; }
        public Collider Collider { get; private set; }

        private readonly Block[,,] blockData;
        public Block[,,] GetBlockData() => blockData;

        private readonly BlockType[,,] blockTypeData;
        public BlockType[,,] GetBlockTypeData() => blockTypeData;

        public List<Tree> Trees { get; }
        public bool TreesCreated { get; private set; }

        public List<Enemy> Enemies { get; }

        public Chunk(Vector3 position, Transform parent)
        {
            BlockGameObject = new GameObject
            {
                name = $"{position}",
                tag = "Chunk"
            };

            BlockGameObject.transform.position = position;
            BlockGameObject.transform.SetParent(parent);

            FluidGameObject = new GameObject
            {
                name = $"{position}_Fluid",
                tag = "Chunk"
            };

            FluidGameObject.transform.position = position;
            FluidGameObject.transform.SetParent(parent);

            VegetationGameObject = new GameObject
            {
                name = $"{position}_Vegetation",
                tag = "Chunk"
            };

            VegetationGameObject.transform.position = position;
            VegetationGameObject.transform.SetParent(parent);

            MeshFilters = new MeshFilter[3];
            MeshRenderers = new MeshRenderer[3];

            int chunkSize = WorldManager.Instance.ChunkSize;
            blockData = new Block[chunkSize, chunkSize, chunkSize];
            blockTypeData = new BlockType[chunkSize, chunkSize, chunkSize];
            Trees = new List<Tree>();
            Enemies = new List<Enemy>();
            ChunkStatus = ChunkStatus.None;
        }

        public void RebuildChunk(ChunkResetData data)
        {
            if (data.ResetBlock)
            {
                Block block = GetBlockData()[data.Position.x, data.Position.y, data.Position.z];
                if (HasFluidNeighbour(block))
                {
                    block.UpdateBlockType(BlockType.Fluid);
                }
                else
                {
                    block.UpdateBlockType(BlockType.Air);
                }
            }

            DestroyChunkMesh();
            BuildBlocks();
            SaveManager.Instance.Save(this);
        }

        private static bool HasFluidNeighbour(Block block)
        {
            return block?.GetBlockNeighbour(Neighbour.Right).BlockType == BlockType.Fluid
                   || block?.GetBlockNeighbour(Neighbour.Left).BlockType == BlockType.Fluid
                   || block?.GetBlockNeighbour(Neighbour.Front).BlockType == BlockType.Fluid
                   || block?.GetBlockNeighbour(Neighbour.Back).BlockType == BlockType.Fluid;
        }

        private void DestroyChunkMesh()
        {
            for (int i = 0; i < MeshFilters.Length; i++)
            {
                Object.DestroyImmediate(MeshFilters[i]);
            }

            for (int i = 0; i < MeshRenderers.Length; i++)
            {
                Object.DestroyImmediate(MeshRenderers[i]);
            }

            Object.DestroyImmediate(Collider);
        }

        public Chunk GetChunkNeighbour(Neighbour neighbour)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            Vector3 chunkPosition = BlockGameObject.transform.position;
            switch (neighbour)
            {
                case Neighbour.Left:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x - chunkSize, chunkPosition.y, chunkPosition.z));

                case Neighbour.Right:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x + chunkSize, chunkPosition.y, chunkPosition.z));

                case Neighbour.Bottom:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y - chunkSize, chunkPosition.z));

                case Neighbour.Top:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y + chunkSize, chunkPosition.z));

                case Neighbour.Back:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z - chunkSize));

                case Neighbour.Front:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z + chunkSize));
            }

            return null;
        }

        public void BuildChunk()
        {
            (bool saveExists, ChunkSaveData chunkData) = SaveManager.Instance.Load(this);
            if (saveExists)
            {
                TreesCreated = chunkData.TreesCreated;
                LoadChunk(chunkData);
                return;
            }

            GenerateChunk();
        }

        private void LoadChunk(ChunkSaveData chunkData)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        Vector3Int localPosition = new Vector3Int(x, y, z);
                        NewLocalBlock(chunkData.BlockTypeData[x, y, z], localPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Generate the bulk of the chunk, the blocks. Determine things such as biomes and where to spawn trees etc.
        /// </summary>
        private void GenerateChunk()
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int chunkTopIndex = chunkSize;
                    bool surfaceBlockAlreadyPlaced = false; // Bool to determine is the top block of a certain column has been placed in this Y loop
                    for (int y = chunkTopIndex; y >= 0; y--) // Start from the top so we can easily place the top block
                    {
                        Vector3Int localPosition = new Vector3Int(x, y, z);
                        int worldPositionY = (int)(y + BlockGameObject.transform.position.y);

                        // Bedrock
                        if (worldPositionY == 0)
                        {
                            NewLocalBlock(BlockType.Bedrock, localPosition);
                            continue;
                        }

                        int worldPositionX = (int)(x + BlockGameObject.transform.position.x);
                        int worldPositionZ = (int)(z + BlockGameObject.transform.position.z);
                        int noise2D = (int)(NoiseUtils.FBM2D(worldPositionX, worldPositionZ)
                            * (WorldManager.Instance.MaxWorldHeight * 2)); // Multiply to match noise scale to world height scale
                        int undergroundLayerStart = noise2D - 6; // This is where underground layer starts

                        bool containsSandBlock = WorldManager.Instance.ContainsSandBlock(BlockGameObject.transform, localPosition);

                        // Between water and underground layer
                        if (worldPositionY == undergroundLayerStart + 1)
                        {
                            NewLocalBlock(BlockType.Dirt, localPosition);
                            continue;
                        }
                        else if (worldPositionY == undergroundLayerStart + 2
                                 && !containsSandBlock)
                        {
                            NewLocalBlock(BlockType.Sand, localPosition);
                            continue;
                        }

                        // Water
                        if (worldPositionY > undergroundLayerStart + 1
                            && worldPositionY < WorldManager.Instance.MaxWorldHeight / 2.25f)
                        {
                            if (containsSandBlock)
                            {
                                Block sandBlock = NewLocalBlock(BlockType.Sand, localPosition);

                                // "Beach"
                                CalculateBeach(sandBlock);
                                continue;
                            }

                            NewLocalBlock(BlockType.Fluid, localPosition);
                            continue;
                        }

                        // Air
                        if (worldPositionY >= noise2D)
                        {
                            NewLocalBlock(BlockType.Air, localPosition);
                            continue;
                        }

                        float noise3D;
                        // Underground layer (stone, diamond, etc)
                        if (worldPositionY <= undergroundLayerStart)
                        {
                            noise3D = NoiseUtils.FBM3D(worldPositionX, worldPositionY, worldPositionZ);
                            if (noise3D >= 0.135f && noise3D <= 0.1325f)
                            {
                                NewLocalBlock(BlockType.Diamond, localPosition);
                            }
                            // Caves are applied below this noise level but must be above certain range from the bottom
                            else if (worldPositionY >= 4 && noise3D < 0.13f)
                            {
                                NewLocalBlock(BlockType.Air, localPosition);
                            }
                            else
                            {
                                NewLocalBlock(BlockType.Stone, localPosition);
                            }

                            continue;
                        }

                        // Surface (grass, dirt, etc)
                        if (surfaceBlockAlreadyPlaced)
                        {
                            NewLocalBlock(BlockType.Dirt, localPosition);
                        }
                        else
                        {
                            WorldManager.Instance.AddSandBlock(BlockGameObject.transform, localPosition);
                            noise3D = NoiseUtils.FBM3D(worldPositionX, worldPositionY, worldPositionZ);
                            Block biomeBlock; // Type of biome (== block)
                            // "Biomes"
                            if (noise2D >= 28 && noise2D <= 30)
                            {
                                biomeBlock = NewLocalBlock(BlockType.Sand, localPosition);
                            }
                            else
                            {
                                biomeBlock = NewLocalBlock(BlockType.Grass, localPosition);
                            }

                            //bool hasSandBlockNearby = HasSandBlocksNearby(x, z, y);
                            if (biomeBlock.BlockType != BlockType.Sand
                                /*&& !hasSandBlockNearby*/)
                            {
                                // Non-block grass (vegetation)
                                if ((noise3D >= 0.08f && noise3D <= 0.082f)
                                    || (noise3D >= 0.16f && noise3D <= 0.162f)
                                    || (noise3D >= 0.24f && noise3D <= 0.242f)
                                    || (noise3D >= 0.3f && noise3D <= 0.32f)
                                    || (noise3D >= 0.35f && noise3D <= 0.352f)
                                    || (noise3D >= 0.45f && noise3D <= 0.452f)
                                    || (noise3D >= 0.65f && noise3D <= 0.652f))
                                {
                                    NewLocalBlock(BlockType.GrassNonBlock, new Vector3Int(x, y + 1, z));
                                }

                                // Trees
                                if ((noise3D >= 0.145f && noise3D <= 0.147f)
                                    || (noise3D >= 0.245f && noise3D <= 0.247f)
                                    || (noise3D >= 0.345f && noise3D <= 0.347f)
                                    || (noise3D >= 0.705f && noise3D <= 0.707f))
                                {
                                    Block treeBase = NewLocalBlock(BlockType.TreeBase, localPosition);
                                    Trees.Add(new Tree(blockData, treeBase));
                                }
                            }

                            TrySpawnEnemy(localPosition);
                            surfaceBlockAlreadyPlaced = true;
                        }
                    }
                }
            }
        }

        // TODO: refactor for better performance
        //private bool HasSandBlocksNearby(int x, int z, int y)
        //{
        //    bool hasSandBlocksNearby = false;
        //    var neighbours = blockData[x, y, z].GetAllBlockNeighbours();
        //    for (int i = 0; i < neighbours.Count; i++)
        //    {
        //        Block neighbourBlock = neighbours.ElementAt(i).Value;
        //        if (neighbourBlock?.BlockType == BlockType.Sand)
        //        {
        //            hasSandBlocksNearby = true;
        //            break;
        //        }
        //    }

        //    return hasSandBlocksNearby;
        //}

        private static void CalculateBeach(Block block)
        {
            Block topBlock = block.GetBlockNeighbour(Neighbour.Top);

            if (topBlock == null) return;

            topBlock.UpdateBlockType(BlockType.Sand);

            Block topBlockRight = topBlock.GetBlockNeighbour(Neighbour.Right);
            Block topBlockLeft = topBlock.GetBlockNeighbour(Neighbour.Left);
            Block topBlockFront = topBlock.GetBlockNeighbour(Neighbour.Front);
            Block topBlockBack = topBlock.GetBlockNeighbour(Neighbour.Back);

            if (topBlockRight?.BlockType == BlockType.Fluid)
            {
                topBlockLeft?.UpdateBlockType(BlockType.Sand);
            }
            else if (topBlockLeft?.BlockType == BlockType.Fluid)
            {
                topBlockRight?.UpdateBlockType(BlockType.Sand);
            }
            else if (topBlockFront?.BlockType == BlockType.Fluid)
            {
                topBlockBack?.UpdateBlockType(BlockType.Sand);
            }
            else if (topBlockBack?.BlockType == BlockType.Fluid)
            {
                topBlockFront?.UpdateBlockType(BlockType.Sand);
            }
        }

        public void TryStartTreeGeneration()
        {
            if (!TreesCreated)
            {
                TreesCreated = true;
                for (int i = 0; i < Trees.Count; i++)
                {
                    Trees[i].GenerateTree();
                }
            }
        }

        private Block NewLocalBlock(BlockType type, Vector3Int position)
        {
            if (position.x >= 0 && position.x <= blockData.GetUpperBound(0)
                && position.y >= 0 && position.y <= blockData.GetUpperBound(1)
                && position.z >= 0 && position.z <= blockData.GetUpperBound(2))
            {
                GameObject chunkGameObj;
                if (type == BlockType.Fluid)
                {
                    chunkGameObj = FluidGameObject;
                }
                else if (type == BlockType.GrassNonBlock)
                {
                    chunkGameObj = VegetationGameObject;
                }
                else
                {
                    chunkGameObj = BlockGameObject;
                }

                blockTypeData[position.x, position.y, position.z] = type;
                return blockData[position.x, position.y, position.z] = new Block(type, position, chunkGameObj, this);
            }

            return null;
        }

        private void TrySpawnEnemy(Vector3Int localPosition)
        {
            if (Random.Range(0, EnemySpawner.Instance.EnemySpawnChance) == EnemySpawner.Instance.EnemySpawnChance / 2)
            {
                EnemySpawner.Instance.Spawn(EnemyType.Spider, BlockGameObject.transform.position + (localPosition + Vector3.up));
            }
        }

        public void BuildBlocks()
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        blockData[x, y, z].BuildBlock();
                    }
                }
            }

            FinalizeMeshes();
            ChunkStatus = ChunkStatus.Keep;
        }

        private void FinalizeMeshes()
        {
            MeshComponents dataChunk = MeshUtils.CombineMesh<MeshCollider>(BlockGameObject, ReferenceManager.Instance.BlockAtlas);
            MeshFilters[0] = dataChunk.MeshFilter;
            MeshRenderers[0] = dataChunk.MeshRenderer;
            Collider = dataChunk.Collider;

            MeshComponents dataFluidChunk = MeshUtils.CombineMesh(FluidGameObject, ReferenceManager.Instance.BlockAtlasTransparent);
            MeshFilters[1] = dataFluidChunk.MeshFilter;
            MeshRenderers[1] = dataFluidChunk.MeshRenderer;

            MeshComponents dataVegetationChunk = MeshUtils.CombineMesh(VegetationGameObject, ReferenceManager.Instance.BlockAtlas);
            MeshFilters[2] = dataVegetationChunk.MeshFilter;
            MeshRenderers[2] = dataVegetationChunk.MeshRenderer;
        }
    }
}