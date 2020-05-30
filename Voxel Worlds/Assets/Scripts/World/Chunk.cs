using UnityEngine;
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

    public enum ChunkNeighbour
    {
        Left,
        Right,
        Bottom,
        Top,
        Back,
        Front
    }

    public class Chunk
    {
        public ChunkStatus ChunkStatus { get; set; }

        public GameObject GameObject { get; }
        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
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

            int chunkSize = WorldManager.Instance.ChunkSize;
            chunkData = new Block[chunkSize, chunkSize, chunkSize];
            blockTypeData = new BlockType[chunkSize, chunkSize, chunkSize];
            ChunkStatus = ChunkStatus.None;
        }

        public void RebuildChunk((bool resetBlock, Vector3Int blockPosition) resetData)
        {
            if (resetData.resetBlock)
            {
                ResetBlockType(resetData.blockPosition);
            }

            DestroyChunkMesh();
            BuildBlocks();
            SaveManager.Instance.Save(this);
        }

        private void ResetBlockType(Vector3Int position) => GetChunkData()[position.x, position.y, position.z].UpdateBlockType(BlockType.Air);

        private void DestroyChunkMesh()
        {
            Object.DestroyImmediate(MeshFilter);
            Object.DestroyImmediate(MeshRenderer);
            Object.DestroyImmediate(Collider);
        }

        public Chunk GetChunkNeighbour(ChunkNeighbour neighbour)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            Vector3 chunkPosition = GameObject.transform.position;
            switch (neighbour)
            {
                case ChunkNeighbour.Left:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x - chunkSize, chunkPosition.y, chunkPosition.z));

                case ChunkNeighbour.Right:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x + chunkSize, chunkPosition.y, chunkPosition.z));

                case ChunkNeighbour.Bottom:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y - chunkSize, chunkPosition.z));

                case ChunkNeighbour.Top:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y + chunkSize, chunkPosition.z));

                case ChunkNeighbour.Back:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z - chunkSize));

                case ChunkNeighbour.Front:
                    return WorldManager.Instance.GetChunk(new Vector3(chunkPosition.x, chunkPosition.y, chunkPosition.z + chunkSize));
            }

            return null;
        }

        public void BuildChunk()
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            (bool saveExists, ChunkData chunkData) = SaveManager.Instance.Load(this);
            if (saveExists)
            {
                LoadChunk(chunkSize, chunkData);
                return;
            }

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
                            NewBlock(BlockType.Bedrock, localPosition);
                            continue;
                        }

                        int worldPositionX = (int)(x + GameObject.transform.position.x);
                        int worldPositionZ = (int)(z + GameObject.transform.position.z);
                        int noise2D = (int)(Utils.FBM2D(worldPositionX, worldPositionZ)
                            * (WorldManager.Instance.MaxWorldHeight * 2)); // Multiply to match noise scale to world height scale

                        // Air
                        if (worldPositionY >= noise2D)
                        {
                            NewBlock(BlockType.Air, localPosition);
                            continue;
                        }

                        // Underground layer (stone, diamond, etc)
                        int undergroundLayerStart = noise2D - 6;
                        if (worldPositionY <= undergroundLayerStart)
                        {
                            float noise3D = Utils.FBM3D(worldPositionX, worldPositionY, worldPositionZ);
                            if (noise3D >= 0.135f && noise3D <= 0.1325f)
                            {
                                NewBlock(BlockType.Diamond, localPosition);
                            }
                            // Caves are applied below this noise level but must be above certain range from the bottom
                            else if (worldPositionY >= 4 && noise3D < 0.13f)
                            {
                                NewBlock(BlockType.Air, localPosition);
                            }
                            else
                            {
                                NewBlock(BlockType.Stone, localPosition);
                            }

                            continue;
                        }

                        // Surface (grass, dirt, etc)
                        if (surfaceBlockAlreadyPlaced)
                        {
                            NewBlock(BlockType.Dirt, localPosition);
                        }
                        else
                        {
                            NewBlock(BlockType.Grass, localPosition);
                            surfaceBlockAlreadyPlaced = true;
                        }
                    }
                }
            }
        }

        private void LoadChunk(int chunkSize, ChunkData chunkData)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        Vector3Int localPosition = new Vector3Int(x, y, z);
                        NewBlock(chunkData.BlockTypeData[x, y, z], localPosition);
                    }
                }
            }
        }

        private void NewBlock(BlockType type, Vector3Int pos)
        {
            chunkData[pos.x, pos.y, pos.z] = new Block(type, pos, GameObject, this);
            blockTypeData[pos.x, pos.y, pos.z] = type;
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
            CombineBlocks();
            Collider = GameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            ChunkStatus = ChunkStatus.Keep;
        }

        // Combine all the chunk's voxels into one mesh to save draw batches
        private void CombineBlocks()
        {
            int childCount = GameObject.transform.childCount;
            CombineInstance[] combinedMeshes = new CombineInstance[childCount];
            for (int i = 0; i < childCount; i++)
            {
                Transform child = GameObject.transform.GetChild(i);
                MeshFilter childMeshFilter = child.GetComponent<MeshFilter>();
                combinedMeshes[i].mesh = childMeshFilter.sharedMesh;
                combinedMeshes[i].transform = childMeshFilter.transform.localToWorldMatrix;
                Object.Destroy(child.gameObject); // Get rid of redundant children
            }

            MeshFilter parentMeshFilter = GameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            MeshFilter = parentMeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes, true, true);
            MeshRenderer parentMeshRenderer = GameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            MeshRenderer = parentMeshRenderer;
            parentMeshRenderer.material = ReferenceManager.Instance.BlockAtlas;
        }
    }
}