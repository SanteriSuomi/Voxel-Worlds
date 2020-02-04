using System.Collections;
using UnityEngine;

namespace Voxel.World
{
    public class Chunk : MonoBehaviour
    {
        [SerializeField]
        private Material material;
        [SerializeField]
        private Vector3 chunkSize = new Vector3(2, 2, 2);
        private Block[,,] chunkData;
        public Block[,,] GetChunkData()
        {
            return chunkData;
        }

        private void Awake()
        {
            StartCoroutine(BuildChunk((int)chunkSize.x, (int)chunkSize.y, (int)chunkSize.z));
        }

        private IEnumerator BuildChunk(int sizeX, int sizeY, int sizeZ)
        {
            chunkData = new Block[sizeX, sizeY, sizeZ];
            // Populate 3D chunk array with new block data
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        chunkData[x, y, z] = new Block(BlockType.Dirt, position, gameObject, material);
                    }
                }
            }

            // Draw the cubes; must be done after populating chunk array, since we need it to be full of data, 
            // so we can use solid checking (for only quads that are visible).
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        chunkData[x, y, z].CreateCube();
                        yield return null;
                    }
                }
            }

            CombineQuads();
        }

        // Use Unity API CombineInstance for combining meshes (in this case quads) in to one single mesh
        private void CombineQuads()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combinedMeshes = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < combinedMeshes.Length; i++)
            {
                combinedMeshes[i].mesh = meshFilters[i].sharedMesh;
                combinedMeshes[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            MeshFilter parentMeshFilter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes);
            MeshRenderer parentMeshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            parentMeshRenderer.material = material;

            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}