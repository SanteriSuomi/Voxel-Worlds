using UnityEngine;

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

        public string ChunkName { get { return chunkGameObject.name; } }

        public Chunk(Vector3 position, Material material, Transform parent)
        {
            // Create a new gameobject for the chunk and set it's name to it's position in the gameworld
            chunkGameObject = new GameObject
            {
                name = World.GetChunkID(position)
            };
            chunkGameObject.transform.position = position; // Chunk position in the world
            chunkGameObject.transform.SetParent(parent); // Set this chunk to be the parent of the world object
            this.chunkMaterial = material; // Chunk texture (world atlas texture from world)
        }

        // Build all the blocks for this chunk object
        public void BuildChunk()
        {
            int worldChunkSize = World.Instance.ChunkSize;
            chunkData = new Block[worldChunkSize, worldChunkSize, worldChunkSize]; // Initialize the voxel data for this chunk
            // Populate the voxel chunk data
            for (int x = 0; x < worldChunkSize; x++)
            {
                for (int y = 0; y < worldChunkSize; y++)
                {
                    for (int z = 0; z < worldChunkSize; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        if (Random.Range(0, 100) < 33)
                        {
                            chunkData[x, y, z] = new Block(BlockType.Air, position, chunkGameObject, this);
                        }
                        else
                        {
                            chunkData[x, y, z] = new Block(BlockType.Grass, position, chunkGameObject, this);
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
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes);
            MeshRenderer parentMeshRenderer = chunkGameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            parentMeshRenderer.material = chunkMaterial;

            for (int i = 0; i < chunkGameObject.transform.childCount; i++)
            {
                Object.Destroy(chunkGameObject.transform.GetChild(i).gameObject); 
            }
        }
    }
}