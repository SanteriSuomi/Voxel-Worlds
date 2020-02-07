using UnityEngine;
using Voxel.Noise;

namespace Voxel.World
{
    public class Chunk
    {
        private readonly GameObject chunkGameObject; // This is the chunk's gameobject in the world
        private readonly Material chunkMaterial; // This is the world texture atlas, the block uses it to get the texture using the UV map coordinates (set in block)

        private Block[,,] chunkData; // The 3D voxel data array for this chunk, contains the data for all this chunk's blocks
        public Block[,,] GetChunkData()
        {
            return chunkData;
        }

        public Chunk(Vector3 position, Material material, Transform parent)
        {
            // Create a new gameobject for the chunk and set it's name to it's position in the gameworld
            chunkGameObject = new GameObject
            {
                name = position.ToString()
            };

            chunkGameObject.transform.position = position; // Chunk position in the world
            chunkGameObject.transform.SetParent(parent); // Set this chunk to be the parent of the world object
            chunkMaterial = material; // Chunk texture (world atlas texture from world)
        }

        // Build all the blocks for this chunk object
        public void BuildChunk()
        {
            int worldChunkSize = World.Instance.ChunkSize;
            chunkData = new Block[worldChunkSize, worldChunkSize, worldChunkSize]; // Initialize the voxel data for this chunk
            // Populate the voxel chunk data
            for (int x = 0; x < worldChunkSize; x++)
            {
                for (int z = 0; z < worldChunkSize; z++)
                {
                    bool topBlockPlaced = false;
                    for (int y = worldChunkSize - 1; y >= 0; y--) // Start height Y from top so we can easily place the top block
                    {
                        Vector3 localPosition = new Vector3(x, y, z);

                        int worldPositionX = (int)(x + chunkGameObject.transform.position.x);
                        int worldPositionY = (int)(y + chunkGameObject.transform.position.y);
                        int worldPositionZ = (int)(z + chunkGameObject.transform.position.z);

                        int noise = (int)(Utils.fBm2D(worldPositionX, worldPositionZ) * World.Instance.MaxWorldHeight);

                        if (worldPositionY <= noise)
                        {
                            if (topBlockPlaced)
                            {
                                chunkData[x, y, z] = new Block(BlockType.Dirt, localPosition, chunkGameObject, this);
                            }
                            else
                            {
                                chunkData[x, y, z] = new Block(BlockType.Grass, localPosition, chunkGameObject, this);
                                topBlockPlaced = true;
                            }
                        }
                        else
                        {
                            chunkData[x, y, z] = new Block(BlockType.Air, localPosition, chunkGameObject, this);
                        }
                    }
                }
            }

            // Draw the cubes; must be done after populating chunk array with blocks, since we need it to be full of data, 
            // so we can use the HasSolidNeighbour check (to discard quads that are not visible).
            for (int x = 0; x < worldChunkSize; x++)
            {
                for (int y = 0; y < worldChunkSize; y++)
                {
                    for (int z = 0; z < worldChunkSize; z++)
                    {
                        chunkData[x, y, z].BuildBlock();
                    }
                }
            }

            // Lets finally combine these cubes in to one mesh
            CombineBlocks();
        }

        // Use Unity API CombineInstance to combine all the chunk's cubes in to one to save draw batches
        private void CombineBlocks()
        {
            MeshFilter[] meshFilters = chunkGameObject.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combinedMeshes = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < combinedMeshes.Length; i++)
            {
                combinedMeshes[i].mesh = meshFilters[i].sharedMesh;
                combinedMeshes[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            MeshFilter parentMeshFilter = chunkGameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes, true, true); // Combine meshes with the transform matrix
            MeshRenderer parentMeshRenderer = chunkGameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            parentMeshRenderer.material = chunkMaterial;

            for (int i = 0; i < chunkGameObject.transform.childCount; i++)
            {
                Object.Destroy(chunkGameObject.transform.GetChild(i).gameObject);
            }
        }
    }
}