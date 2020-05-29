using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel.World
{
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
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
        // Amount of times a block needs to be until it gets destroyed. Same order as BlockType enum.
        private static readonly int[] blockHealthMap =
        {
            0, // Air
            4, // Grass
            4, // Dirt
            7, // Stone
            9, // Diamond
            0 // Bedrock
        };

        // UV coordinates for different blocks on the UV atlas
        private static readonly Vector2[,] uvAtlasMap =
        {
            // Grass Top
            {
                new Vector2(0.125f, 0.375f),
                new Vector2(0.1875f, 0.375f),
                new Vector2(0.125f, 0.4375f),
                new Vector2(0.1875f, 0.4375f)
            },
            // Dirt
            {
                new Vector2(0.125f, 0.9375f),
                new Vector2(0.1875f, 0.9375f),
                new Vector2(0.125f, 1),
                new Vector2(0.1875f, 1)
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
            },
            // Grass Side
            {
                new Vector2(0.1875f, 0.9375f),
                new Vector2(0.25f, 0.9375f),
                new Vector2(0.1875f, 1),
                new Vector2(0.25f, 1)
            }
        };

        // All the points that make 2 triangles, which in turn makes a quad
        private static readonly int[] triangles = new int[]
        {
            3, 2, 1, 3, 1, 0
        };

        // All possible points on a cube made out of quads with clockwise ordering.
        // left/right: position of X (or "left/right")
        // Bottom/Top: position of Y (or "bottom/top")
        // 0/1: position of Z (or "depth")
        // https://cglearn.codelight.eu/files/course/3/cube.png
        private static Vector3 leftBottom0 = new Vector3(-0.5f, -0.5f, 0.5f);
        private static Vector3 leftBottom1 = new Vector3(-0.5f, -0.5f, -0.5f);
        private static Vector3 rightBottom0 = new Vector3(0.5f, -0.5f, 0.5f);
        private static Vector3 rightBottom1 = new Vector3(0.5f, -0.5f, -0.5f);
        private static Vector3 rightTop0 = new Vector3(0.5f, 0.5f, 0.5f);
        private static Vector3 rightTop1 = new Vector3(0.5f, 0.5f, -0.5f);
        private static Vector3 leftTop0 = new Vector3(-0.5f, 0.5f, 0.5f);
        private static Vector3 leftTop1 = new Vector3(-0.5f, 0.5f, -0.5f);

        private const int maxQuadCount = 6;
        #endregion

        public bool IsSolid { get; private set; }
        public BlockType BlockType { get; private set; }

        public int BlockHealth { get; private set; }
        public int MaxBlockHealth => blockHealthMap[(int)BlockType];
        public int MidBlockHealth => MaxBlockHealth / 2;
        public int MinBlockHealth => 1;

        /// <summary>
        /// The average position of block's all quads (the middle of the block).
        /// </summary>
        public Vector3 BlockPositionAverage { get; private set; }

        private readonly GameObject chunkGameObject;
        private readonly Chunk chunkOwner;
        private readonly Vector3Int position; // Position relative to the chunk

        public Block(BlockType type, Vector3 position, GameObject parent, Chunk owner)
        {
            BlockType = type;
            this.position = new Vector3Int((int)position.x, (int)position.y, (int)position.z);
            chunkGameObject = parent;
            chunkOwner = owner;
            IsSolid = BlockType != BlockType.Air;
            ResetBlockHealth();
        }

        /// <summary>
        /// Damage the block. Return true if block was destroyed.
        /// </summary>
        public bool DamageBlock()
        {
            BlockHealth--;
            if (BlockHealth <= 0)
            {
                chunkOwner.RebuildChunk((true, position));
                return true;
            }

            return false;
        }

        public void ResetBlockHealth() => BlockHealth = blockHealthMap[(int)BlockType];

        public void UpdateBlockType(BlockType type)
        {
            BlockType = type;
            IsSolid = BlockType != BlockType.Air;
            chunkOwner.GetBlockTypeData()[position.x, position.y, position.z] = type;
        }

        public void BuildBlock()
        {
            if (BlockType == BlockType.Air)
            {
                IsSolid = BlockType != BlockType.Air;
                return;
            }

            CheckNeighbours();
        }

        private void CheckNeighbours()
        {
            List<Vector3> quadPositions = new List<Vector3>(maxQuadCount);

            // Front quad
            Vector3Int quadPos = new Vector3Int(position.x, position.y, position.z + 1);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Front);
            }
            quadPositions.Add(quadPos);

            // Back quad
            quadPos = new Vector3Int(position.x, position.y, position.z - 1);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Back);
            }
            quadPositions.Add(quadPos);

            // Left quad
            quadPos = new Vector3Int(position.x - 1, position.y, position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Left);
            }
            quadPositions.Add(quadPos);

            // Right quad
            quadPos = new Vector3Int(position.x + 1, position.y, position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Right);
            }
            quadPositions.Add(quadPos);

            // Top quad
            quadPos = new Vector3Int(position.x, position.y + 1, position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Top);
            }
            quadPositions.Add(quadPos);

            // Bottom quad
            quadPos = new Vector3Int(position.x, position.y - 1, position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(BlockSide.Bottom);
            }
            quadPositions.Add(quadPos);

            CalculateBlockPositionAverage(quadPositions);
        }

        private void CalculateBlockPositionAverage(List<Vector3> quadPositions)
        {
            Vector3 blockPositionAverageTemp = Vector3.zero;
            for (int i = 0; i < quadPositions.Count; i++)
            {
                blockPositionAverageTemp.x += chunkOwner.GameObject.transform.position.x + quadPositions[i].x;
                blockPositionAverageTemp.y += chunkOwner.GameObject.transform.position.y + quadPositions[i].y;
                blockPositionAverageTemp.z += chunkOwner.GameObject.transform.position.z + quadPositions[i].z;
            }

            blockPositionAverageTemp.x /= maxQuadCount;
            blockPositionAverageTemp.y /= maxQuadCount;
            blockPositionAverageTemp.z /= maxQuadCount;
            BlockPositionAverage = blockPositionAverageTemp;
        }

        private bool HasSolidNeighbour(int x, int y, int z)
        {
            try
            {
                int chunkSize = WorldManager.Instance.ChunkSize - 1;
                Block[,,] chunkData;

                // Check if the position we're checking is in a neighbouring chunk
                if (x < 0 || x >= chunkSize
                    || y < 0 || y >= chunkSize
                    || z < 0 || z >= chunkSize)
                {
                    Vector3 neighbouringChunkPosition = chunkGameObject.transform.position
                                                      + new Vector3((x - position.x) * chunkSize,
                                                                    (y - position.y) * chunkSize,
                                                                    (z - position.z) * chunkSize);
                    x = CheckBlockEdge(x);
                    y = CheckBlockEdge(y);
                    z = CheckBlockEdge(z);

                    Chunk chunk = WorldManager.Instance.GetChunk(neighbouringChunkPosition);
                    if (chunk != null)
                    {
                        chunkData = chunk.GetChunkData();
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    chunkData = chunkOwner.GetChunkData();
                }

                if (chunkData != null
                    && x <= chunkData.GetUpperBound(0)
                    && x >= chunkData.GetLowerBound(0)
                    && y <= chunkData.GetUpperBound(1)
                    && y >= chunkData.GetLowerBound(1)
                    && z <= chunkData.GetUpperBound(2)
                    && z >= chunkData.GetLowerBound(2))
                {
                    return chunkData[x, y, z].IsSolid;
                }

                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        // Checks if a given axis is not a local coordinate, but a neighbouring one (chunk). 
        // Axis must be in between 0 and ChunkSize for it to be a local chunk.
        private int CheckBlockEdge(int index)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            if (index <= -1)
            {
                return chunkSize - 1;
            }
            else if (index >= chunkSize)
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

                switch (side)
                {
                    case BlockSide.Bottom:
                        AssignVertices(vertices, new Vector3[] { leftBottom0, rightBottom0, rightBottom1, leftBottom1 });
                        AssignNormals(normals, Vector3.down);
                        break;

                    case BlockSide.Top:
                        AssignVertices(vertices, new Vector3[] { leftTop1, rightTop1, rightTop0, leftTop0 });
                        AssignNormals(normals, Vector3.up);
                        break;

                    case BlockSide.Left:
                        AssignVertices(vertices, new Vector3[] { leftTop1, leftTop0, leftBottom0, leftBottom1 });
                        AssignNormals(normals, Vector3.left);
                        break;

                    case BlockSide.Right:
                        AssignVertices(vertices, new Vector3[] { rightTop0, rightTop1, rightBottom1, rightBottom0 });
                        AssignNormals(normals, Vector3.right);
                        break;

                    case BlockSide.Front:
                        AssignVertices(vertices, new Vector3[] { rightBottom0, leftBottom0, leftTop0, rightTop0 });
                        AssignNormals(normals, Vector3.forward);
                        break;

                    case BlockSide.Back:
                        AssignVertices(vertices, new Vector3[] { rightTop1, leftTop1, leftBottom1, rightBottom1 });
                        AssignNormals(normals, Vector3.back);
                        break;
                }

                Vector2[] uvs = new Vector2[4];
                AssignUVs(side, uvs);

                Mesh mesh = new Mesh
                {
                    name = $"Quad {side} Mesh"
                };

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);

                GameObject quad = new GameObject($"Quad {side}");
                quad.transform.position = position;
                quad.transform.SetParent(chunkGameObject.transform);
                MeshFilter meshFilter = quad.AddComponent(typeof(MeshFilter)) as MeshFilter;
                meshFilter.mesh = mesh;
            }
            catch (NullReferenceException e)
            {
                Debug.LogWarning(e);
            }
        }

        private void AssignVertices(List<Vector3> vertices, Vector3[] verticesToAssign)
        {
            for (int i = 0; i < verticesToAssign.Length; i++)
            {
                vertices.Add(verticesToAssign[i]);
            }
        }

        private void AssignNormals(Vector3[] normals, Vector3 direction)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = direction;
            }
        }

        private void AssignUVs(BlockSide side, Vector2[] uvs)
        {
            if (BlockType == BlockType.Grass && (side == BlockSide.Back
                                             || side == BlockSide.Front
                                             || side == BlockSide.Left
                                             || side == BlockSide.Right))
            {
                uvs[0] = uvAtlasMap[5, 0];
                uvs[1] = uvAtlasMap[5, 1];
                uvs[2] = uvAtlasMap[5, 2];
                uvs[3] = uvAtlasMap[5, 3];
            }
            else if (BlockType == BlockType.Grass && side == BlockSide.Top)
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
                Debug.LogWarning("No blocktype assigned, probably shouldn't be here.");
                int blockType = (int)BlockType;
                uvs[0] = uvAtlasMap[blockType, 0];
                uvs[1] = uvAtlasMap[blockType, 1];
                uvs[2] = uvAtlasMap[blockType, 2];
                uvs[3] = uvAtlasMap[blockType, 3];
            }
        }
    }
}