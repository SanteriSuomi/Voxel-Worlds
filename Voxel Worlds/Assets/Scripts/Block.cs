using UnityEngine;

namespace Voxel.World
{
    public enum BlockType
    {
        Grass,
        Dirt,
        Stone,
        Air
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
            if (blockType == BlockType.Air) return; // If block is any of these types, do not create it

            int positionX = (int)blockPosition.x;
            int positionY = (int)blockPosition.y;
            int positionZ = (int)blockPosition.z;

            // If there is no neighbour, create a specified side of the cube
            if (!HasSolidNeighbour(positionX, positionY, positionZ + 1))
            {
                CreateQuad(CubeSide.Front);
            }
            if (!HasSolidNeighbour(positionX, positionY, positionZ - 1))
            {
                CreateQuad(CubeSide.Back);
            }
            if (!HasSolidNeighbour(positionX - 1, positionY, positionZ))
            {
                CreateQuad(CubeSide.Left);
            }
            if (!HasSolidNeighbour(positionX + 1, positionY, positionZ))
            {
                CreateQuad(CubeSide.Right);
            }
            if (!HasSolidNeighbour(positionX, positionY + 1, positionZ))
            {
                CreateQuad(CubeSide.Top);
            }
            if (!HasSolidNeighbour(positionX, positionY - 1, positionZ))
            {
                CreateQuad(CubeSide.Bottom);
            }
        }

        private bool HasSolidNeighbour(int x, int y, int z)
        {
            Block[,,] chunkData;
            int chunkSize = World.Instance.ChunkSize;
            // If the neighbour position is out of bounds of the chunk size, it is in a another chunk
            if (x < 0 || x >= chunkSize
                || y < 0 || y >= chunkSize
                || z < 0 || z >= chunkSize)
            {
                // Since this chunk doesn't contain this X Y and Z neighbour, we must convert it to the other chunk
                Vector3 neighbouringChunkPosition
                    = parentChunk.transform.position
                    + new Vector3((x - (int)blockPosition.x) * chunkSize,
                                  (y - (int)blockPosition.y) * chunkSize,
                                  (z - (int)blockPosition.z) * chunkSize);

                // Retrive the chunk ID for the dictionary
                string chunkID = World.GetChunkID(neighbouringChunkPosition);

                x = CheckBlockForEdge(x);
                y = CheckBlockForEdge(y);
                z = CheckBlockForEdge(z);

                // Finally check if this chunk exists by consulting the chunk dictionary by it's ID
                if (World.Instance.ChunkDictionary.TryGetValue(chunkID, out Chunk chunk))
                {
                    chunkData = chunk.GetChunkData();
                }
                else
                {
                    // There is no such chunk
                    return false;
                }
            }
            else
            {
                // The neighbour position is in this chunk...
                chunkData = chunkOwner.GetChunkData();
            }

            int CheckBlockForEdge(int blockIndex)
            {
                if (blockIndex == -1) // If under this chunk's bounds
                {
                    // We're at the top of a chunk
                    blockIndex = chunkSize - 1;
                }
                else if (blockIndex == chunkSize) // If over the bounds
                {
                    // Were at the start of a chunk
                    blockIndex = 0;
                }

                return blockIndex;
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

            // We've checked everything, there is no neighbour
            return false;
        }

        private void CreateQuad(CubeSide side)
        {
            Vector3[] vertices = new Vector3[4];
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
                    vertices = new Vector3[]
                    {
                        leftBottom0, rightBottom0, rightBottom1, leftBottom1
                    };

                    AssignNormalsTo(Vector3.down);
                    break;
                case CubeSide.Top:
                    vertices = new Vector3[]
                    {
                        leftTop1, rightTop1, rightTop0, leftTop0
                    };

                    AssignNormalsTo(Vector3.up);
                    break;
                case CubeSide.Left:
                    vertices = new Vector3[]
                    {
                        leftTop1, leftTop0, leftBottom0, leftBottom1
                    };

                    AssignNormalsTo(Vector3.left);
                    break;
                case CubeSide.Right:
                    vertices = new Vector3[]
                    {
                        rightTop0, rightTop1, rightBottom1, rightBottom0
                    };

                    AssignNormalsTo(Vector3.right);
                    break;
                case CubeSide.Front:
                    vertices = new Vector3[]
                    {
                        rightBottom0, leftBottom0, leftTop0, rightTop0
                    };

                    AssignNormalsTo(Vector3.forward);
                    break;
                case CubeSide.Back:
                    vertices = new Vector3[]
                    {
                        rightTop1, leftTop1, leftBottom1, rightBottom1
                    };

                    AssignNormalsTo(Vector3.back);
                    break;
            }

            void AssignNormalsTo(Vector3 direction)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = direction;
                }
            }

            Vector2[] uvs = new Vector2[4];
            // Assign UVs from the atlas map
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
            else
            {
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

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();

            GameObject quad = new GameObject($"Quad {side}");
            quad.transform.position = blockPosition;
            quad.transform.SetParent(parentChunk.transform);
            MeshFilter meshFilter = quad.AddComponent(typeof(MeshFilter)) as MeshFilter;
            meshFilter.mesh = mesh;
        }
    }
}