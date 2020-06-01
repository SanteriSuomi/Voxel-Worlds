using System;
using System.Collections.Generic;
using UnityEngine;
using Voxel.Utility;

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

    public struct BlockCreationData
    {
        public Transform Parent { get; }
        public Vector3Int Position { get; }
        public BlockType BlockType { get; }
        public BlockSide BlockSide { get; }

        public BlockCreationData(Transform parent, Vector3Int position, BlockType blockType, BlockSide blockSide)
        {
            Parent = parent;
            Position = position;
            BlockType = blockType;
            BlockSide = blockSide;
        }
    }

    public struct InstantiateBlockData
    {
        public BlockType BlockType { get; }
        public Vector3 Position { get; }
        public Vector3 LocalScale { get; }
        public bool IsTrigger { get; }

        public InstantiateBlockData(BlockType type, Vector3 position, Vector3 localScale, bool isTrigger)
        {
            BlockType = type;
            Position = position;
            LocalScale = localScale;
            IsTrigger = isTrigger;
        }
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

        #region Block Health Properties
        public int BlockHealth { get; private set; }
        public int MaxBlockHealth => blockHealthMap[(int)BlockType];
        public int MidBlockHealth => MaxBlockHealth / 2;
        public int MinBlockHealth => 1;
        #endregion

        /// <summary>
        /// The average position of block's all quads (the middle of the block).
        /// </summary>
        public Vector3 WorldPositionAverage { get; private set; }

        private readonly GameObject chunkGameObject;
        private readonly Chunk chunkOwner;

        /// <summary>
        /// Position relative to the owner chunk's position.
        /// </summary>
        public Vector3Int Position { get; }

        public Block(BlockType type, Vector3Int position, GameObject parent, Chunk owner)
        {
            BlockType = type;
            Position = position;
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
                chunkOwner.RebuildChunk(new ChunkResetData(true, Position));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Replace block with a specific type. Performs health reset and chunk rebuild.
        /// </summary>
        /// <param name="type">Type to replace the current block with.</param>
        public void ReplaceBlock(BlockType type)
        {
            UpdateBlockType(type);
            ResetBlockHealth();
            chunkOwner.RebuildChunk(new ChunkResetData(false, Position));
        }

        public void ResetBlockHealth() => BlockHealth = blockHealthMap[(int)BlockType];

        public void UpdateBlockType(BlockType type)
        {
            BlockType = type;
            IsSolid = BlockType != BlockType.Air;
            chunkOwner.GetBlockTypeData()[Position.x, Position.y, Position.z] = type;
        }

        /// <returns>Block as a GameObject</returns>
        /// <summary>
        /// Create a block GameObject (not a voxel mesh) of type and spawn it at the specified position in world space.
        /// </summary>
        /// <typeparam name="T1">Type of Collider to apply.</typeparam>
        /// <typeparam name="T2">GameObject script to apply.</typeparam>
        /// <param name="data">Data to construct the block with.</param>
        /// <returns>The block GameObject.</returns>
        public static T2 InstantiateBlock<T1, T2>(InstantiateBlockData data)
            where T1: Collider
            where T2: MonoBehaviour
        {
            if (data.BlockType == BlockType.Air)
            {
                Debug.LogWarning("Attempted to create an air block.");
                return null;
            }

            GameObject block = new GameObject($"{data.BlockType}_{data.Position}");
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Left));
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Right));
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Bottom));
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Top));
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Back));
            CreateQuad(new BlockCreationData(block.transform, Vector3Int.zero, data.BlockType, BlockSide.Front));
            MeshComponents components = MeshUtils.CombineMesh<T1>(block, ReferenceManager.Instance.BlockAtlas);
            components.Collider.isTrigger = data.IsTrigger;
            block.transform.position = data.Position;
            block.transform.localScale = data.LocalScale;
            return block.AddComponent(typeof(T2)) as T2;
        }

        // TODO: Fix GetBlockNeighbour to return blocks correctly.
        public Block GetBlockNeighbour(Neighbour neighbour)
        {
            switch (neighbour)
            {
                case Neighbour.Left:
                    return GetBlockAt(new Vector3Int(Position.x - 1, Position.y, Position.z));

                case Neighbour.Right:
                    return GetBlockAt(new Vector3Int(Position.x + 1, Position.y, Position.z));

                case Neighbour.Bottom:
                    return GetBlockAt(new Vector3Int(Position.x, Position.y - 1, Position.z));

                case Neighbour.Top:
                    return GetBlockAt(new Vector3Int(Position.x, Position.y + 1, Position.z));

                case Neighbour.Back:
                    return GetBlockAt(new Vector3Int(Position.x, Position.y, Position.z - 1));

                case Neighbour.Front:
                    return GetBlockAt(new Vector3Int(Position.x, Position.y, Position.z + 1));
            }

            return null;
        }

        private Block GetBlockAt(Vector3Int position)
        {
            int chunkEdge = WorldManager.Instance.ChunkEdge + 1;
            Block[,,] chunkData = chunkOwner.GetChunkData();
            if (position.x == -1)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Left).GetChunkData();
                position.x += chunkEdge;
            }
            else if (position.x == chunkEdge)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Right).GetChunkData();
                position.x += -chunkEdge;
            }
            else if (position.y == -1)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Bottom).GetChunkData();
                position.y += chunkEdge;
            }
            else if (position.y == chunkEdge)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Top).GetChunkData();
                position.y += -chunkEdge;
            }
            else if (position.z == -1)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Back).GetChunkData();
                position.z += chunkEdge;
            }
            else if (position.z == chunkEdge)
            {
                chunkData = chunkOwner.GetChunkNeighbour(Neighbour.Front).GetChunkData();
                position.z += -chunkEdge;
            }

            return chunkData[position.x, position.x, position.z];
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
            Vector3Int quadPos = new Vector3Int(Position.x, Position.y, Position.z + 1);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Front));
            }
            quadPositions.Add(quadPos);

            // Back quad
            quadPos = new Vector3Int(Position.x, Position.y, Position.z - 1);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Back));
            }
            quadPositions.Add(quadPos);

            // Left quad
            quadPos = new Vector3Int(Position.x - 1, Position.y, Position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Left));
            }
            quadPositions.Add(quadPos);

            // Right quad
            quadPos = new Vector3Int(Position.x + 1, Position.y, Position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Right));
            }
            quadPositions.Add(quadPos);

            // Top quad
            quadPos = new Vector3Int(Position.x, Position.y + 1, Position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Top));
            }
            quadPositions.Add(quadPos);

            // Bottom quad
            quadPos = new Vector3Int(Position.x, Position.y - 1, Position.z);
            if (!HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z))
            {
                CreateQuad(new BlockCreationData(chunkGameObject.transform, Position, BlockType, BlockSide.Bottom));
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
            WorldPositionAverage = blockPositionAverageTemp;
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
                                                      + new Vector3((x - Position.x) * chunkSize,
                                                                    (y - Position.y) * chunkSize,
                                                                    (z - Position.z) * chunkSize);
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

        private static void CreateQuad(BlockCreationData data)
        {
            try
            {
                List<Vector3> vertices = new List<Vector3>();
                Vector3[] normals = new Vector3[4];

                switch (data.BlockSide)
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

                    case BlockSide.Front: /*rightBottom0, leftBottom0, leftTop0, rightTop0*/
                        AssignVertices(vertices, new Vector3[] { leftTop0, rightTop0, rightBottom0, leftBottom0 });
                        AssignNormals(normals, Vector3.forward);
                        break;

                    case BlockSide.Back:
                        AssignVertices(vertices, new Vector3[] { rightTop1, leftTop1, leftBottom1, rightBottom1 });
                        AssignNormals(normals, Vector3.back);
                        break;
                }

                Vector2[] uvs = new Vector2[4];
                AssignUVs(data.BlockType, data.BlockSide, uvs);

                Mesh mesh = new Mesh
                {
                    name = $"Quad {data.BlockSide} Mesh"
                };

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);

                GameObject quad = new GameObject($"Quad {data.BlockSide}");
                quad.transform.position = data.Position;
                quad.transform.SetParent(data.Parent);
                MeshFilter meshFilter = quad.AddComponent(typeof(MeshFilter)) as MeshFilter;
                meshFilter.mesh = mesh;
            }
            catch (NullReferenceException e)
            {
                Debug.LogWarning(e);
            }
        }

        private static void AssignVertices(List<Vector3> vertices, Vector3[] verticesToAssign)
        {
            for (int i = 0; i < verticesToAssign.Length; i++)
            {
                vertices.Add(verticesToAssign[i]);
            }
        }

        private static void AssignNormals(Vector3[] normals, Vector3 direction)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = direction;
            }
        }

        private static void AssignUVs(BlockType blockType, BlockSide side, Vector2[] uvs)
        {
            if (blockType == BlockType.Grass && (side == BlockSide.Back
                                             || side == BlockSide.Front
                                             || side == BlockSide.Left
                                             || side == BlockSide.Right))
            {
                uvs[0] = uvAtlasMap[5, 0];
                uvs[1] = uvAtlasMap[5, 1];
                uvs[2] = uvAtlasMap[5, 2];
                uvs[3] = uvAtlasMap[5, 3];
            }
            else if (blockType == BlockType.Grass && side == BlockSide.Top)
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
                Debug.LogWarning("No blocktype assigned, probably shouldn't be here.");
                int blockTypeAsInt = (int)blockType;
                uvs[0] = uvAtlasMap[blockTypeAsInt, 0];
                uvs[1] = uvAtlasMap[blockTypeAsInt, 1];
                uvs[2] = uvAtlasMap[blockTypeAsInt, 2];
                uvs[3] = uvAtlasMap[blockTypeAsInt, 3];
            }
        }
    }
}