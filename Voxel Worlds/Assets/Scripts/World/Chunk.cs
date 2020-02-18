using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        private readonly GameObject chunkGameObject; // This is the chunk's gameobject in the world
        private readonly Material chunkMaterial; // This is the world texture atlas, the block uses it to get the texture using the UV map coordinates (set in block)

        private float[] chunkVoxelValues; // For marching cubes
        private readonly Block[,,] chunkData; // The 3D voxel data array for this chunk, contains the data for all this chunk's blocks
        public Block[,,] GetChunkData()
        {
            return chunkData;
        }

        private readonly int chunkSize;

        public ChunkStatus ChunkStatus { get; set; }

        public Chunk(Vector3 position, Material material, Transform parent, int chunkSize)
        {
            // Create a new gameobject for the chunk and set it's name to it's position in the gameworld
            chunkGameObject = new GameObject
            {
                name = position.ToString(),
            };

            chunkGameObject.transform.position = position; // Chunk position in the world
            chunkGameObject.transform.SetParent(parent); // Set this chunk to be the parent of the world object
            chunkMaterial = material; // Chunk texture (world atlas texture from world)
            this.chunkSize = chunkSize - 1; // - 1 because of array index
            chunkData = new Block[chunkSize, chunkSize, chunkSize]; // Initialize the voxel data for this chunk
        }

        // Build all the blocks for this chunk object
        public void BuildChunk()
        {
            // Populate the voxel chunk data
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int chunkTopIndex = chunkSize;
                    bool surfaceBlockAlreadyPlaced = false; // Bool to determine is the top block of a certain column has been placed in this Y loop
                    for (int y = chunkTopIndex; y >= 0; y--) // Start height Y from top so we can easily place the top block
                    {
                        Vector3 localPosition = new Vector3(x, y, z);
                        int worldPositionY = (int)(y + chunkGameObject.transform.position.y);

                        // Bedrock
                        if (worldPositionY == 0)
                        {
                            NewBlock(BlockType.Bedrock);
                            continue;
                        }

                        int worldPositionX = (int)(x + chunkGameObject.transform.position.x);
                        int worldPositionZ = (int)(z + chunkGameObject.transform.position.z);
                        int noise2D = (int)(Utils.fBm2D(worldPositionX, worldPositionZ)
                            * (WorldManager.Instance.MaxWorldHeight * 2)); // Multiply to match noise scale to world height scale
                        // Air
                        if (worldPositionY >= noise2D)
                        {
                            NewBlock(BlockType.Air);
                            continue;
                        }

                        // Underground (stone, diamond, etc)
                        int undergroundLayerStart = noise2D - Random.Range(4, 8);
                        if (worldPositionY <= undergroundLayerStart) // If we're certain range below the surface
                        {
                            float noise3D = Utils.fBm3D(worldPositionX, worldPositionY, worldPositionZ);
                            if (noise3D >= 0.135f && noise3D <= Random.Range(0.135f, 0.1355f))
                            {
                                NewBlock(BlockType.Diamond);
                            }
                            // Caves are applied below this noise level but must be above certain range from the bottom
                            else if (worldPositionY >= Random.Range(3, 5) && noise3D < Random.Range(0.125f, 0.135f))
                            {
                                NewBlock(BlockType.Air);
                            }
                            else
                            {
                                NewBlock(BlockType.Stone);
                            }

                            continue;
                        }

                        // Surface (grass, dirt, etc)
                        if (surfaceBlockAlreadyPlaced)
                        {
                            NewBlock(BlockType.Dirt);
                        }
                        else
                        {
                            NewBlock(BlockType.Grass);
                            surfaceBlockAlreadyPlaced = true;
                        }

                        void NewBlock(BlockType type)
                        {
                            chunkData[x, y, z] = new Block(type, localPosition, chunkGameObject, this);
                        }
                    }
                }
            }

            ChunkStatus = ChunkStatus.Draw;
            BuildBlocks();
        }

        private void BuildBlocks()
        {
            //chunkVoxelValues = new float[worldChunkSize * worldChunkSize * worldChunkSize]; // Voxel data for marching cubes
            // Draw the cubes; must be done after populating chunk array with blocks, since we need it to be full of data, 
            // so we can use the HasSolidNeighbour check (to discard quads that are not visible).
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if (chunkData[x, y, z] != null)
                        {
                            chunkData[x, y, z].BuildBlock();
                        }
                        //chunkData[x, y, z].BuildBlock();
                        //int chunkVoxelIndex = x + y * worldChunkSize + z * worldChunkSize * worldChunkSize;
                        //SetChunkVoxelValues(x, y, z, chunkVoxelIndex);
                    }
                }
            }

            //void SetChunkVoxelValues(int x, int y, int z, int index) // Set the voxel array values for marching 
            //{
            //    if (chunkData[x, y, z].IsSolid)
            //    {
            //        chunkVoxelValues[index] = -1;
            //    }
            //    else
            //    {
            //        chunkVoxelValues[index] = 1;
            //    }
            //}

            // Lets finally combine these cubes in to one mesh to "complete" the chunk
            MeshFilter chunkMeshFilter = CombineBlocks();
            //MarchBlocks(worldChunkSize, chunkMeshFilter);
            AddCollider();
        }

        // Use Unity API CombineInstance to combine all the chunk's cubes in to one to save draw batches
        private MeshFilter CombineBlocks()
        {
            int childCount = chunkGameObject.transform.childCount;
            CombineInstance[] combinedMeshes = new CombineInstance[childCount];
            for (int i = 0; i < childCount; i++)
            {
                MeshFilter childMeshFilter = chunkGameObject.transform.GetChild(i).GetComponent<MeshFilter>();
                combinedMeshes[i].mesh = childMeshFilter.sharedMesh;
                combinedMeshes[i].transform = childMeshFilter.transform.localToWorldMatrix;
                Object.Destroy(chunkGameObject.transform.GetChild(i).gameObject); // Get rid of redundant children
            }

            MeshFilter parentMeshFilter = chunkGameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes, true, true);
            MeshRenderer parentMeshRenderer = chunkGameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            parentMeshRenderer.material = chunkMaterial;
            return parentMeshFilter; // Return chunk mesh filter for further processing
        }

        private void MarchBlocks(int worldChunkSize, MeshFilter chunkMeshFilter)
        {
            List<Vector3> vertices = chunkMeshFilter.mesh.vertices.ToList();
            List<int> indices = chunkMeshFilter.mesh.triangles.ToList();
            Utils.MarchingTertrahedron(chunkVoxelValues, worldChunkSize, vertices, indices);
            chunkMeshFilter.mesh.SetVertices(vertices);
            chunkMeshFilter.mesh.SetTriangles(indices, 0);
            chunkMeshFilter.mesh.RecalculateNormals();
            chunkMeshFilter.mesh.RecalculateBounds();
        }

        private void AddCollider()
        {
            chunkGameObject.AddComponent(typeof(MeshCollider));
        }
    }
}