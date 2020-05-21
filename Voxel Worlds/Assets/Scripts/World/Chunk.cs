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

    public class Chunk
    {
        public ChunkStatus ChunkStatus { get; set; }

        public GameObject GameObject { get; } // This is the chunk's gameobject in the world
        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public Collider Collider { get; private set; }

        private readonly Material chunkMaterial; // This is the world texture atlas, the block uses it to get the texture using the UV map coordinates (set in block)
        private readonly Block[,,] chunkData; // The 3D voxel data array for this chunk, contains the data for all this chunk's blocks
        public Block[,,] GetChunkData()
        {
            return chunkData;
        }

        private readonly BlockType[,,] blockMatrixData;
        public BlockType[,,] GetBlockTypeData()
        {
            return blockMatrixData;
        }

        public Chunk(Vector3 position, Material material, Transform parent, bool emptyChunk)
        {
            if (!emptyChunk)
            {
                // Create a new gameobject for the chunk and set it's name to it's position in the gameworld
                GameObject = new GameObject
                {
                    name = position.ToString(),
                };

                GameObject.transform.position = position; // Chunk position in the world
                GameObject.transform.SetParent(parent); // Set this chunk to be the parent of the world object
            }
            else
            {
                GameObject = null;
            }

            chunkMaterial = material; // Chunk texture (world atlas texture from world)
            int chunkSize = WorldManager.Instance.ChunkSize;
            chunkData = new Block[chunkSize, chunkSize, chunkSize]; // Initialize the voxel data for this chunk
            blockMatrixData = new BlockType[chunkSize, chunkSize, chunkSize];
            ChunkStatus = ChunkStatus.None;
        }

        // Build all the blocks for this chunk object
        public void BuildChunk()
        {
            (bool saveExists, ChunkData chunkData) = SaveManager.Instance.Load(this);
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int chunkTopIndex = chunkSize;
                    bool surfaceBlockAlreadyPlaced = false; // Bool to determine is the top block of a certain column has been placed in this Y loop
                    for (int y = chunkTopIndex; y >= 0; y--) // Start height Y from top so we can easily place the top block
                    {
                        Vector3 localPosition = new Vector3(x, y, z);
                        int worldPositionY = (int)(y + GameObject.transform.position.y);

                        if (saveExists)
                        {
                            NewBlock(chunkData.BlockTypeData[x, y, z], x, z, y, localPosition);
                            continue;
                        }

                        // Bedrock
                        if (worldPositionY == 0)
                        {
                            NewBlock(BlockType.Bedrock, x, z, y, localPosition);
                            continue;
                        }

                        int worldPositionX = (int)(x + GameObject.transform.position.x);
                        int worldPositionZ = (int)(z + GameObject.transform.position.z);
                        int noise2D = (int)(Utils.FBM2D(worldPositionX, worldPositionZ)
                            * (WorldManager.Instance.MaxWorldHeight * 2)); // Multiply to match noise scale to world height scale
                        // Air
                        if (worldPositionY >= noise2D)
                        {
                            NewBlock(BlockType.Air, x, z, y, localPosition);
                            continue;
                        }

                        // Underground (stone, diamond, etc)
                        int undergroundLayerStart = noise2D - Random.Range(4, 8);
                        if (worldPositionY <= undergroundLayerStart) // If we're certain range below the surface
                        {
                            float noise3D = Utils.FBM3D(worldPositionX, worldPositionY, worldPositionZ);
                            if (noise3D >= 0.135f && noise3D <= Random.Range(0.135f, 0.1355f))
                            {
                                NewBlock(BlockType.Diamond, x, z, y, localPosition);
                            }
                            // Caves are applied below this noise level but must be above certain range from the bottom
                            else if (worldPositionY >= Random.Range(3, 5) && noise3D < Random.Range(0.125f, 0.135f))
                            {
                                NewBlock(BlockType.Air, x, z, y, localPosition);
                            }
                            else
                            {
                                NewBlock(BlockType.Stone, x, z, y, localPosition);
                            }

                            continue;
                        }

                        // Surface (grass, dirt, etc)
                        if (surfaceBlockAlreadyPlaced)
                        {
                            NewBlock(BlockType.Dirt, x, z, y, localPosition);
                        }
                        else
                        {
                            NewBlock(BlockType.Grass, x, z, y, localPosition);
                            surfaceBlockAlreadyPlaced = true;
                        }
                    }
                }
            }
        }

        private void NewBlock(BlockType type, int x, int z, int y, Vector3 localPosition)
        {
            chunkData[x, y, z] = new Block(type, localPosition, GameObject, this);
            blockMatrixData[x, y, z] = chunkData[x, y, z].BlockType;
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
                        if (chunkData[x, y, z] == null)
                        {
                            Debug.LogWarning($"chunkData {(x, y, z)}");
                            return;
                        }

                        chunkData[x, y, z].BuildBlock();
                    }
                }
            }

            CombineBlocks();
            AddCollider();
            ChunkStatus = ChunkStatus.Keep;
        }

        // Use Unity API CombineInstance to combine all the chunk's cubes in to one to save draw batches
        private void CombineBlocks()
        {
            int childCount = GameObject.transform.childCount;
            CombineInstance[] combinedMeshes = new CombineInstance[childCount];
            for (int i = 0; i < childCount; i++)
            {
                MeshFilter childMeshFilter = GameObject.transform.GetChild(i).GetComponent<MeshFilter>();
                combinedMeshes[i].mesh = childMeshFilter.sharedMesh;
                combinedMeshes[i].transform = childMeshFilter.transform.localToWorldMatrix;
                Object.Destroy(GameObject.transform.GetChild(i).gameObject); // Get rid of redundant children
            }

            MeshFilter parentMeshFilter = GameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            MeshFilter = parentMeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes, true, true);
            MeshRenderer parentMeshRenderer = GameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            MeshRenderer = parentMeshRenderer;
            parentMeshRenderer.material = chunkMaterial;
        }

        private void AddCollider()
        {
            Collider = GameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        }
    }
}