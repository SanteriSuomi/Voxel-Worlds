﻿using UnityEngine;
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
        public bool ResetBlockType { get; }
        public Vector3Int Position { get; }

        public ChunkResetData(bool reset, Vector3Int position)
        {
            ResetBlockType = reset;
            Position = position;
        }

        public static ChunkResetData GetEmpty() => new ChunkResetData(false, Vector3Int.zero);
    }

    public class Chunk
    {
        public ChunkStatus ChunkStatus { get; set; }

        public GameObject GameObject { get; }
        public GameObject FluidGameObject { get; }

        public MeshFilter[] MeshFilters { get; }
        public MeshRenderer[] MeshRenderers { get; }
        public Collider Collider { get; private set; }

        private readonly Block[,,] chunkData;
        public Block[,,] GetChunkData() => chunkData;

        private readonly BlockType[,,] blockTypeData;
        public BlockType[,,] GetBlockTypeData() => blockTypeData;

        public Chunk(Vector3 position, Transform parent)
        {
            GameObject = new GameObject
            {
                name = position.ToString(),
                tag = "Chunk"
            };

            GameObject.transform.position = position;
            GameObject.transform.SetParent(parent);

            FluidGameObject = new GameObject
            {
                name = $"{position}_Fluid",
                tag = "Chunk"
            };

            FluidGameObject.transform.position = position;
            FluidGameObject.transform.SetParent(parent);

            MeshFilters = new MeshFilter[2];
            MeshRenderers = new MeshRenderer[2];

            int chunkSize = WorldManager.Instance.ChunkSize;
            chunkData = new Block[chunkSize, chunkSize, chunkSize];
            blockTypeData = new BlockType[chunkSize, chunkSize, chunkSize];
            ChunkStatus = ChunkStatus.None;
        }

        public void RebuildChunk(ChunkResetData data)
        {
            if (data.ResetBlockType)
            {
                ResetBlockType(data.Position);
            }

            DestroyChunkMesh();
            BuildBlocks();
            SaveManager.Instance.Save(this);
        }

        private void ResetBlockType(Vector3Int position)
            => GetChunkData()[position.x, position.y, position.z].UpdateBlockType(BlockType.Air);

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
            Vector3 chunkPosition = GameObject.transform.position;
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
                LoadChunk(chunkData);
                return;
            }

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
                        int worldPositionY = (int)(y + GameObject.transform.position.y);

                        // Bedrock
                        if (worldPositionY == 0)
                        {
                            NewLocalBlock(BlockType.Bedrock, localPosition);
                            continue;
                        }

                        int worldPositionX = (int)(x + GameObject.transform.position.x);
                        int worldPositionZ = (int)(z + GameObject.transform.position.z);
                        int noise2D = (int)(NoiseUtils.FBM2D(worldPositionX, worldPositionZ)
                            * (WorldManager.Instance.MaxWorldHeight * 2)); // Multiply to match noise scale to world height scale
                        int undergroundLayerStart = noise2D - 6; // This is where underground layer starts

                        if (worldPositionY == undergroundLayerStart + 1)
                        {
                            NewLocalBlock(BlockType.Dirt, localPosition);
                            continue;
                        }

                        // Water
                        if (worldPositionY > undergroundLayerStart + 1
                            && worldPositionY < WorldManager.Instance.MaxWorldHeight / 2.25f)
                        {
                            if (WorldManager.Instance.ContainsGrassBlock(GameObject.transform, localPosition))
                            {
                                NewLocalBlock(BlockType.Dirt, localPosition);
                                continue;
                            }

                            NewLocalBlock(BlockType.Water, localPosition);
                            continue;
                        }

                        // Air
                        if (worldPositionY >= noise2D)
                        {
                            NewLocalBlock(BlockType.Air, localPosition);
                            continue;
                        }

                        // Underground layer (stone, diamond, etc)
                        if (worldPositionY <= undergroundLayerStart)
                        {
                            float noise3D = NoiseUtils.FBM3D(worldPositionX, worldPositionY, worldPositionZ);
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
                            WorldManager.Instance.AddGrassBlock(GameObject.transform, localPosition);
                            NewLocalBlock(BlockType.Grass, localPosition);
                            surfaceBlockAlreadyPlaced = true;
                        }
                    }
                }
            }
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

        private void NewLocalBlock(BlockType type, Vector3Int position)
        {
            if (type == BlockType.Water)
            {
                chunkData[position.x, position.y, position.z] = new Block(type, position, FluidGameObject, this);
            }
            else
            {
                chunkData[position.x, position.y, position.z] = new Block(type, position, GameObject, this);
            }

            blockTypeData[position.x, position.y, position.z] = type;
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
                        chunkData[x, y, z].BuildBlock();
                    }
                }
            }

            FinalizeChunk();
        }

        private void FinalizeChunk()
        {
            MeshComponents dataChunk = MeshUtils.CombineMesh<MeshCollider>(GameObject, ReferenceManager.Instance.BlockAtlas);
            MeshFilters[0] = dataChunk.MeshFilter;
            MeshRenderers[0] = dataChunk.MeshRenderer;
            Collider = dataChunk.Collider;

            MeshComponents dataFluidChunk = MeshUtils.CombineMesh(FluidGameObject, ReferenceManager.Instance.BlockAtlasTransparent);
            MeshFilters[1] = dataFluidChunk.MeshFilter;
            MeshRenderers[1] = dataFluidChunk.MeshRenderer;

            ChunkStatus = ChunkStatus.Keep;
        }
    }
}