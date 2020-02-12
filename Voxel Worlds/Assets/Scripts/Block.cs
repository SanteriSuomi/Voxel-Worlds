using System.Collections.Generic;
using UnityEngine;

namespace Voxel.vWorld
{
    public enum BlockType
    {
        Grass,
        Dirt,
        Stone,
        Air,
        Diamond,
        Bedrock
    }

    public class Block
    {
        private enum CubeSide
        {
            Bottom,
            Top,
            Left,
            Right,
            Front,
            Back
        }

        private float[] voxelCube; // Used for marching cubes
        public float[] GetVoxelCube()
        {
            return voxelCube;
        }

        public bool IsSolid { get; private set; } // Bool for checking if this block is solid material
        private readonly GameObject parentChunk; // Object (chunk) this block is parented to
        private readonly Chunk chunkOwner; // Chunk reference to get chunk data
        private readonly Vector3 blockPosition; // Position relative to the chunk
        private readonly BlockType blockType; // What type this block is (for UV maps)

        // UV coordinates for the material on the UV atlas
        private readonly Vector2[,] uvAtlasMap =
        {
            // Grass Top
            {
                new Vector2(0, 0.9375f),
                new Vector2(0.0625f, 0.9375f),
                new Vector2(0, 1),
                new Vector2(0.0625f, 1)
            },
            // Dirt (also grass sides)
            {
                new Vector2(0.25f, 0.875f),
                new Vector2(0.3125f, 0.875f),
                new Vector2(0.25f, 0.9375f),
                new Vector2(0.3125f, 0.9375f)
            },
            // Stone
            {
                new Vector2(0, 0.875f),
                new Vector2(0.0625f, 0.875f),
                new Vector2(0, 0.9375f),
                new Vector2(0.0625f, 0.9375f)
            },
            // Diamond
            {
                new Vector2(0.125f, 0.75f),
                new Vector2(0.1875f, 0.75f),
                new Vector2(0.125f, 0.8125f),
                new Vector2(0.1875f, 0.8125f)
            },
            // Bedrock
            {
                new Vector2(0.5f, 0.5625f),
                new Vector2(0.5625f, 0.5625f),
                new Vector2(0.5f, 0.625f),
                new Vector2(0.5625f, 0.625f)
            }
        };

        public Block(BlockType type, Vector3 position, GameObject parent, Chunk owner)
        {
            blockType = type;
            blockPosition = position;
            parentChunk = parent;
            chunkOwner = owner;
            if (type == BlockType.Air) // These types of blocks should not be solid
            {
                IsSolid = false;
            }
            else
            {
                IsSolid = true;
            }
        }

        public void BuildBlock()
        {
            bool front = false, back = false,
                 left = false, right = false,
                 top = false, bottom = false;

            int positionX = (int)blockPosition.x;
            int positionY = (int)blockPosition.y;
            int positionZ = (int)blockPosition.z;

            voxelCube = new float[8];
            // If there is no neighbour, create a specified side of the cube
            if (!HasSolidNeighbour(positionX, positionY, positionZ + 1))
            {
                front = true;
                voxelCube[0] = -1;
            }
            else { voxelCube[0] = 1; }
            if (!HasSolidNeighbour(positionX, positionY, positionZ - 1))
            {
                back = true;
                voxelCube[1] = -1;
            }
            else { voxelCube[1] = 1; }
            if (!HasSolidNeighbour(positionX - 1, positionY, positionZ))
            {
                left = true;
                voxelCube[2] = -1;
            }
            else { voxelCube[2] = 1; }
            if (!HasSolidNeighbour(positionX + 1, positionY, positionZ))
            {
                right = true;
                voxelCube[3] = -1;
            }
            else { voxelCube[3] = 1; }
            if (!HasSolidNeighbour(positionX, positionY + 1, positionZ))
            {
                top = true;
                voxelCube[4] = -1;
            }
            else { voxelCube[4] = 1; }
            if (!HasSolidNeighbour(positionX, positionY - 1, positionZ))
            {
                bottom = true;
                voxelCube[5]  = -1;
            }
            else { voxelCube[5] = 1; }
            voxelCube[6] = -1;
            voxelCube[7] = -1;

            //if (blockType != BlockType.Air) // If block is any of these types, do not create it
            //{
            //    if (front)
            //    {
            //        CreateQuad(CubeSide.Front);
            //    }
            //    if (back)
            //    {
            //        CreateQuad(CubeSide.Back);
            //    }
            //    if (left)
            //    {
            //        CreateQuad(CubeSide.Left);
            //    }
            //    if (right)
            //    {
            //        CreateQuad(CubeSide.Right);
            //    }
            //    if (top)
            //    {
            //        CreateQuad(CubeSide.Top);
            //    }
            //    if (bottom)
            //    {
            //        CreateQuad(CubeSide.Bottom);
            //    }
            //}
        }

        private bool HasSolidNeighbour(int x, int y, int z)
        {
            Block[,,] chunkData;
            int chunkSize = World.Instance.ChunkSize;
            // If the neighbour position we're checking isn't in the bounds of this chunk, we must be in another one
            if (x < 0 || x >= chunkSize
                || y < 0 || y >= chunkSize
                || z < 0 || z >= chunkSize)
            {
                // Convert the X Y and Z position to the neigbouring chunk
                Vector3 neighbouringChunkPosition
                    = parentChunk.transform.position
                    + new Vector3((x - (int)blockPosition.x) * chunkSize,
                                  (y - (int)blockPosition.y) * chunkSize,
                                  (z - (int)blockPosition.z) * chunkSize);

                x = CheckBlockEdgeCase(x);
                y = CheckBlockEdgeCase(y);
                z = CheckBlockEdgeCase(z);

                // Finally check if this chunk exists by consulting the chunk dictionary from it's ID
                Chunk chunk = World.Instance.GetChunk(World.GetChunkID(neighbouringChunkPosition));
                if (chunk != null)
                {
                    chunkData = chunk.GetChunkData();
                }
                else
                {
                    // Since we couldn't find a the chunk in the dictionary, there is no chunk at this position
                    return false;
                }
            }
            else
            {
                // The neighbour position is in this chunk...
                chunkData = chunkOwner.GetChunkData();
            }

            int CheckBlockEdgeCase(int index)
            {
                if (index <= -1) // We must be at the end of another chunk as there is no index -1
                {
                    index = chunkSize - 1;
                }
                else if (index >= chunkSize) // We must be at the start of another chunk as there is no index at ChunkSize, only ChunkSize - 1
                {
                    index = 0;
                }

                return index;
            }

            // Then at last check the neighbour that the neighbour is solid
            if (chunkData != null
                && x <= chunkData.GetUpperBound(0)
                && x >= chunkData.GetLowerBound(0)
                && y <= chunkData.GetUpperBound(1)
                && y >= chunkData.GetLowerBound(1)
                && z <= chunkData.GetUpperBound(2)
                && z >= chunkData.GetLowerBound(2)
                && chunkData[x, y, z] != null)
            {
                return chunkData[x, y, z].IsSolid;
            }

            // We've checked everything absolutely isn't a neighbour
            return false;
        }

        private void CreateQuad(CubeSide side)
        {
            List<Vector3> vertices = new List<Vector3>();
            Vector3[] normals = new Vector3[4];

            // All possible points on a cube made out of quads with clockwise ordering
            // left/right: position of X (or "left/right")
            // Bottom/Top: position of Y (or "bottom/top")
            // 0/1: position of Z (or "depth")
            // https://cglearn.codelight.eu/files/course/3/cube.png
            Vector3 leftBottom0 = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 rightBottom0 = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 rightBottom1 = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 leftBottom1 = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 leftTop0 = new Vector3(-0.5f, 0.5f, 0.5f);
            Vector3 rightTop0 = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 rightTop1 = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 leftTop1 = new Vector3(-0.5f, 0.5f, -0.5f);

            // Build a quad side and assign it's normals
            switch (side)
            {
                case CubeSide.Bottom:
                    AssignVertices(new Vector3[] { leftBottom0, rightBottom0, rightBottom1, leftBottom1 });
                    AssignNormals(Vector3.down);
                    break;
                case CubeSide.Top:
                    AssignVertices(new Vector3[] { leftTop1, rightTop1, rightTop0, leftTop0 });
                    AssignNormals(Vector3.up);
                    break;
                case CubeSide.Left:
                    AssignVertices(new Vector3[] { leftTop1, leftTop0, leftBottom0, leftBottom1 });
                    AssignNormals(Vector3.left);
                    break;
                case CubeSide.Right:
                    AssignVertices(new Vector3[] { rightTop0, rightTop1, rightBottom1, rightBottom0 });
                    AssignNormals(Vector3.right);
                    break;
                case CubeSide.Front:
                    AssignVertices(new Vector3[] { rightBottom0, leftBottom0, leftTop0, rightTop0 });
                    AssignNormals(Vector3.forward);
                    break;
                case CubeSide.Back:
                    AssignVertices(new Vector3[] { rightTop1, leftTop1, leftBottom1, rightBottom1 });
                    AssignNormals(Vector3.back);
                    break;
            }

            void AssignVertices(Vector3[] verticesToAssign)
            {
                for (int i = 0; i < verticesToAssign.Length; i++)
                {
                    vertices.Add(verticesToAssign[i]);
                }
            }

            void AssignNormals(Vector3 direction)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = direction;
                }
            }

            Vector2[] uvs = new Vector2[4];
            // Assign UVs from the atlas map depending on the blocktype
            if (blockType == BlockType.Grass && side == CubeSide.Top)
            {
                uvs[0] = uvAtlasMap[0, 0];
                uvs[1] = uvAtlasMap[0, 1];
                uvs[2] = uvAtlasMap[0, 2];
                uvs[3] = uvAtlasMap[0, 3];
            }
            else if (blockType == BlockType.Dirt || blockType == BlockType.Grass)
            {
                uvs[0] = uvAtlasMap[1, 0];
                uvs[1] = uvAtlasMap[1, 1];
                uvs[2] = uvAtlasMap[1, 2];
                uvs[3] = uvAtlasMap[1, 3];
            }
            else if (blockType == BlockType.Stone)
            {
                uvs[0] = uvAtlasMap[2, 0];
                uvs[1] = uvAtlasMap[2, 1];
                uvs[2] = uvAtlasMap[2, 2];
                uvs[3] = uvAtlasMap[2, 3];
            }
            else if (blockType == BlockType.Diamond)
            {
                uvs[0] = uvAtlasMap[3, 0];
                uvs[1] = uvAtlasMap[3, 1];
                uvs[2] = uvAtlasMap[3, 2];
                uvs[3] = uvAtlasMap[3, 3];
            }
            else if (blockType == BlockType.Bedrock)
            {
                uvs[0] = uvAtlasMap[4, 0];
                uvs[1] = uvAtlasMap[4, 1];
                uvs[2] = uvAtlasMap[4, 2];
                uvs[3] = uvAtlasMap[4, 3];
            }
            else
            {
                Debug.LogWarning("Probably shouldn't be here.");
                int typeToInt = (int)blockType;
                uvs[0] = uvAtlasMap[typeToInt, 0];
                uvs[1] = uvAtlasMap[typeToInt, 1];
                uvs[2] = uvAtlasMap[typeToInt, 2];
                uvs[3] = uvAtlasMap[typeToInt, 3];
            }

            // All the points that make 2 triangles, which in turn makes a quad
            int[] triangles = new int[]
            {
                3, 2, 1, 3, 1, 0
            };

            Mesh mesh = new Mesh
            {
                name = $"Quad {side} Mesh"
            };

            mesh.SetVertices(vertices);
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);

            GameObject quad = new GameObject($"Quad {side}");
            quad.transform.position = blockPosition;
            quad.transform.SetParent(parentChunk.transform);
            MeshFilter meshFilter = quad.AddComponent(typeof(MeshFilter)) as MeshFilter;
            meshFilter.mesh = mesh;
        }
    }
}