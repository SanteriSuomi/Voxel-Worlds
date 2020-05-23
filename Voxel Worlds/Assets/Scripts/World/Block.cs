﻿using System.Collections.Generic;
using UnityEngine;
using System;

namespace Voxel.World
{
    public enum BlockType
    {
        None,
        Grass,
        Dirt,
        Stone,
        Air,
        Diamond,
        Bedrock
    }

    public enum BlockSide
    {
        Bottom,
        Top,
        Left,
        Right,
        Front,
        Back
    }

    public class Block
    {
        #region Static Block Settings
        // UV coordinates for the material on the UV atlas
        private static readonly Vector2[,] uvAtlasMap =
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

        // All the points that make 2 triangles, which in turn makes a quad
        private static readonly int[] triangles = new int[]
        {
            3, 2, 1, 3, 1, 0
        };

        // All possible points on a cube made out of quads with clockwise ordering
        // left/right: position of X (or "left/right")
        // Bottom/Top: position of Y (or "bottom/top")
        // 0/1: position of Z (or "depth")
        // https://cglearn.codelight.eu/files/course/3/cube.png
        private static Vector3 leftBottom0 = new Vector3(-0.5f, -0.5f, 0.5f);
        private static Vector3 rightBottom0 = new Vector3(0.5f, -0.5f, 0.5f);
        private static Vector3 rightBottom1 = new Vector3(0.5f, -0.5f, -0.5f);
        private static Vector3 leftBottom1 = new Vector3(-0.5f, -0.5f, -0.5f);
        private static Vector3 leftTop0 = new Vector3(-0.5f, 0.5f, 0.5f);
        private static Vector3 rightTop0 = new Vector3(0.5f, 0.5f, 0.5f);
        private static Vector3 rightTop1 = new Vector3(0.5f, 0.5f, -0.5f);
        private static Vector3 leftTop1 = new Vector3(-0.5f, 0.5f, -0.5f);
        #endregion

        public bool IsSolid { get; private set; } // Bool for checking if this block is solid material
        private readonly GameObject parentChunk; // Object (chunk) this block is parented to
        private readonly Chunk chunkOwner; // Chunk reference to get chunk data
        private readonly Vector3 blockPosition; // Position relative to the chunk

        public BlockType BlockType { get; set; } // What type this block is (for UV maps)
        public void SetType(BlockType type)
        {
            BlockType = type;
            IsSolid = BlockType != BlockType.Air;
        }

        public Block(BlockType type, Vector3 position, GameObject parent, Chunk owner)
        {
            BlockType = type;
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
            if (BlockType == BlockType.Air)
            {
                return;
            }

            int positionX = (int)blockPosition.x;
            int positionY = (int)blockPosition.y;
            int positionZ = (int)blockPosition.z;

            // If there is no neighbour, create a specified side of the cube
            if (!HasSolidNeighbour(positionX, positionY, positionZ + 1))
            {
                CreateQuad(BlockSide.Front);
            }
            if (!HasSolidNeighbour(positionX, positionY, positionZ - 1))
            {
                CreateQuad(BlockSide.Back);
            }
            if (!HasSolidNeighbour(positionX - 1, positionY, positionZ))
            {
                CreateQuad(BlockSide.Left);
            }
            if (!HasSolidNeighbour(positionX + 1, positionY, positionZ))
            {
                CreateQuad(BlockSide.Right);
            }
            if (!HasSolidNeighbour(positionX, positionY + 1, positionZ))
            {
                CreateQuad(BlockSide.Top);
            }
            if (!HasSolidNeighbour(positionX, positionY - 1, positionZ))
            {
                CreateQuad(BlockSide.Bottom);
            }
        }

        private bool HasSolidNeighbour(int x, int y, int z)
        {
            try
            {
                int chunkSize = WorldManager.Instance.ChunkSize - 1;
                Block[,,] chunkData;
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
                    string chunkID = WorldManager.Instance.GetChunkID(neighbouringChunkPosition);
                    Chunk chunk = WorldManager.Instance.GetChunkFromID(chunkID);
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

                // Then at last check the neighbour that the neighbour is solid
                if (chunkData != null
                    && x <= chunkData.GetUpperBound(0)
                    && x >= chunkData.GetLowerBound(0)
                    && y <= chunkData.GetUpperBound(1)
                    && y >= chunkData.GetLowerBound(1)
                    && z <= chunkData.GetUpperBound(2)
                    && z >= chunkData.GetLowerBound(2)
                    /*&& !chunkData[x, y, z].Equals(null)*/)
                {
                    return chunkData[x, y, z].IsSolid;
                }

                // We've checked everything absolutely isn't a neighbour
                return false;
            }
            catch (NullReferenceException)
            {
                // On any error we return false
                return false;
            }
        }

        private int CheckBlockEdgeCase(int index)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            if (index <= -1) // We must be at the end of another chunk as there is no index -1
            {
                return chunkSize - 1;
            }
            else if (index >= chunkSize) // We must be at the start of another chunk as there is no index at ChunkSize, only ChunkSize - 1
            {
                return 0;
            }

            return index;
        }

        private void CreateQuad(BlockSide side)
        {
            try
            {
                List<Vector3> vertices = new List<Vector3>();
                Vector3[] normals = new Vector3[4];

                // Build a quad side and assign it's normals
                switch (side)
                {
                    case BlockSide.Bottom:
                        AssignVertices(new Vector3[] { leftBottom0, rightBottom0, rightBottom1, leftBottom1 });
                        AssignNormals(Vector3.down);
                        break;

                    case BlockSide.Top:
                        AssignVertices(new Vector3[] { leftTop1, rightTop1, rightTop0, leftTop0 });
                        AssignNormals(Vector3.up);
                        break;

                    case BlockSide.Left:
                        AssignVertices(new Vector3[] { leftTop1, leftTop0, leftBottom0, leftBottom1 });
                        AssignNormals(Vector3.left);
                        break;

                    case BlockSide.Right:
                        AssignVertices(new Vector3[] { rightTop0, rightTop1, rightBottom1, rightBottom0 });
                        AssignNormals(Vector3.right);
                        break;

                    case BlockSide.Front:
                        AssignVertices(new Vector3[] { rightBottom0, leftBottom0, leftTop0, rightTop0 });
                        AssignNormals(Vector3.forward);
                        break;

                    case BlockSide.Back:
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
                if (BlockType == BlockType.Grass && side == BlockSide.Top)
                {
                    uvs[0] = uvAtlasMap[0, 0];
                    uvs[1] = uvAtlasMap[0, 1];
                    uvs[2] = uvAtlasMap[0, 2];
                    uvs[3] = uvAtlasMap[0, 3];
                }
                else if (BlockType == BlockType.Dirt || BlockType == BlockType.Grass)
                {
                    uvs[0] = uvAtlasMap[1, 0];
                    uvs[1] = uvAtlasMap[1, 1];
                    uvs[2] = uvAtlasMap[1, 2];
                    uvs[3] = uvAtlasMap[1, 3];
                }
                else if (BlockType == BlockType.Stone)
                {
                    uvs[0] = uvAtlasMap[2, 0];
                    uvs[1] = uvAtlasMap[2, 1];
                    uvs[2] = uvAtlasMap[2, 2];
                    uvs[3] = uvAtlasMap[2, 3];
                }
                else if (BlockType == BlockType.Diamond)
                {
                    uvs[0] = uvAtlasMap[3, 0];
                    uvs[1] = uvAtlasMap[3, 1];
                    uvs[2] = uvAtlasMap[3, 2];
                    uvs[3] = uvAtlasMap[3, 3];
                }
                else if (BlockType == BlockType.Bedrock)
                {
                    uvs[0] = uvAtlasMap[4, 0];
                    uvs[1] = uvAtlasMap[4, 1];
                    uvs[2] = uvAtlasMap[4, 2];
                    uvs[3] = uvAtlasMap[4, 3];
                }
                else
                {
                    Debug.LogWarning("Probably shouldn't be here.");
                    int typeToInt = (int)BlockType;
                    uvs[0] = uvAtlasMap[typeToInt, 0];
                    uvs[1] = uvAtlasMap[typeToInt, 1];
                    uvs[2] = uvAtlasMap[typeToInt, 2];
                    uvs[3] = uvAtlasMap[typeToInt, 3];
                }

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
            catch (NullReferenceException e)
            {
                Debug.LogWarning(e);
            }
        }
    }
}