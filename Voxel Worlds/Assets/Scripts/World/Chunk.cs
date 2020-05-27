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

        private readonly BlockType[,,] blockTypeData;
        public BlockType[,,] GetBlockTypeData()
        {
            return blockTypeData;
        }

        public Chunk(Vector3 position, Material material, Transform parent)
        {
            // Create a new gameobject for the chunk and set it's name to it's position in the gameworld
            GameObject = new GameObject
            {
                name = position.ToString(),
                tag = "Chunk"
            };

            GameObject.transform.position = position; // Chunk position in the world
            GameObject.transform.SetParent(parent); // Set this chunk to be the parent of the world object

            chunkMaterial = material; // Chunk texture (world atlas texture from world)
            int chunkSize = WorldManager.Instance.ChunkSize;
            chunkData = new Block[chunkSize, chunkSize, chunkSize];
            blockTypeData = new BlockType[chunkSize, chunkSize, chunkSize];
            ChunkStatus = ChunkStatus.None;
        }

        // Build all the blocks for this chunk object
        public void BuildChunk()
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            (bool saveExists, ChunkData chunkData) = SaveManager.Instance.Load(this);
            if (saveExists)
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

                return;
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int chunkTopIndex = chunkSize;
                    bool surfaceBlockAlreadyPlaced = false; // Bool to determine is the top block of a certain column has been placed in this Y loop
                    for (int y = chunkTopIndex; y >= 0; y--) // Start height Y from top so we can easily place the top block
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

                        // Underground (stone, diamond, etc)
                        int undergroundLayerStart = noise2D - 6;
                        if (worldPositionY <= undergroundLayerStart) // If we're certain range below the surface
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
            parentMeshRenderer.material = chunkMaterial;
        }

        private void AddCollider() => Collider = GameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;

        /// <summary>
        /// Destroy chunk's mesh filter, mesh renderer and collider.
        /// </summary>
        public void DestroyChunkMesh()
        {
            Object.DestroyImmediate(MeshFilter);
            Object.DestroyImmediate(MeshRenderer);
            Object.DestroyImmediate(Collider);
        }
    }
}