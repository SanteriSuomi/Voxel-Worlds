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
        Bedrock,
        Fluid,
        Sand,
        TreeBase,
        Wood,
        Leaf
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
        public Vector3Int Position { get; set; }
        public BlockType BlockType { get; }
        public BlockSide BlockSide { get; }

        public BlockCreationData(Transform parent, BlockType blockType, BlockSide blockSide)
        {
            Parent = parent;
            Position = Vector3Int.zero;
            BlockType = blockType;
            BlockSide = blockSide;
        }
    }

    public struct InstantiateBlockInputData
    {
        public BlockType BlockType { get; }
        public Vector3 Position { get; }
        public Vector3 LocalScale { get; }

        public InstantiateBlockInputData(BlockType type, Vector3 position, Vector3 localScale)
        {
            BlockType = type;
            Position = position;
            LocalScale = localScale;
        }
    }

    public struct InstantiateBlockOutputData<T>
    {
        public MeshComponents MeshComponents { get; }
        public Rigidbody Rigidbody { get; }
        public T Obj { get; }

        public InstantiateBlockOutputData(MeshComponents meshComponents, Rigidbody rigidbody, T obj)
        {
            MeshComponents = meshComponents;
            Rigidbody = rigidbody;
            Obj = obj;
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
            0, // Bedrock
            0, // Water
            3, // Sand
            5, // TreeBase
            5, // Wood
            2 // Leaf
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
            },
            // Water
            {
                new Vector2(0.875f, 0.125f),
                new Vector2(0.9375f, 0.125f),
                new Vector2(0.875f, 0.1875f),
                new Vector2(0.9375f, 0.1875f)
            },
            // Sand
            {
                new Vector2(0, 0.25f),
                new Vector2(0.0625f, 0.25f),
                new Vector2(0, 0.3125f),
                new Vector2(0.0625f, 0.3125f)
            },
            // Wood
            {
                new Vector2(0.25f, 0.875f),
                new Vector2(0.3125f, 0.875f),
                new Vector2(0.25f, 0.9375f),
                new Vector2(0.3125f, 0.9375f)
            },
            // Leaf
            {
                new Vector2(0.25f, 0.75f),
                new Vector2(0.3125f, 0.75f),
                new Vector2(0.25f, 0.8125f),
                new Vector2(0.3125f, 0.8125f)
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

        /// <summary>
        /// If a block is solid, blocks neighbouring the block won't draw faces towards it.
        /// </summary>
        public bool IsSolid { get; set; }
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

        public GameObject ChunkGameObject { get; set; }
        public Chunk ChunkOwner { get; }

        /// <summary>
        /// Position relative to the owner chunk's position.
        /// </summary>
        public Vector3Int Position { get; }

        public Block(BlockType type, Vector3Int position, GameObject parent, Chunk owner)
        {
            BlockType = type;
            Position = position;
            ChunkGameObject = parent;
            ChunkOwner = owner;
            UpdateSolidity();
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
                if (!TryActivateFluidDynamic(true))
                {
                    ChunkOwner.RebuildChunk(new ChunkResetData(true, Position));
                }

                TryActivateFallingBlockDynamic();

                return true;
            }

            return false;
        }

        /// <summary>
        /// See if this block has conditions to activate fluid (water) dynamic ("physics")
        /// </summary>
        /// <param name="updateThisBlockToFluid">Whether or not make update this block to fluid also.</param>
        /// <returns>True if dynamic can be activated.</returns>
        public bool TryActivateFluidDynamic(bool updateThisBlockToFluid)
        {
            if ((BlockType == BlockType.Fluid
                || GetBlockNeighbour(Neighbour.Top).BlockType == BlockType.Fluid)
                && GetBlockNeighbour(Neighbour.Bottom).BlockType == BlockType.Air)
            {
                if (updateThisBlockToFluid)
                {
                    UpdateBlockAndChunk(BlockType.Fluid);
                }

                GlobalChunk.Instance.StartWaterDynamic(this);
                return true;
            }

            return false;
        }

        public bool TryActivateFallingBlockDynamic()
        {
            Block topNeighbour = GetBlockNeighbour(Neighbour.Top);
            if (topNeighbour.BlockType == BlockType.Sand
                && BlockType == BlockType.Air)
            {
                GlobalChunk.Instance.StartBlockFallingDynamic(this, topNeighbour.BlockType);
                return true;
            }
            else if (BlockType == BlockType.Sand)
            {
                GlobalChunk.Instance.StartBlockFallingDynamic(this, BlockType);
            }

            return false;
        }

        /// <summary>
        /// Replace the block with the specific type (update). Also perform health and chunk rebuild.
        /// </summary>
        /// <param name="type">Type to replace the current block with.</param>
        public void UpdateBlockAndChunk(BlockType type)
        {
            UpdateBlockType(type);
            ResetBlockHealth();
            ChunkOwner.RebuildChunk(new ChunkResetData(false, Position));
        }

        /// <summary>
        /// Update the block type.
        /// </summary>
        /// <param name="type">Type to replace the current type with.</param>
        public void UpdateBlockType(BlockType type)
        {
            BlockType = type;
            UpdateSolidity();
            ChunkOwner.GetBlockTypeData()[Position.x, Position.y, Position.z] = type;
            UpdateGameObject(type);
            ResetBlockHealth();
        }

        private void UpdateGameObject(BlockType type)
        {
            if (type == BlockType.Fluid)
            {
                ChunkGameObject = ChunkOwner.FluidGameObject;
            }
            else
            {
                ChunkGameObject = ChunkOwner.GameObject;
            }
        }

        public void ResetBlockHealth() => BlockHealth = blockHealthMap[(int)BlockType];

        private void UpdateSolidity() => IsSolid = BlockType != BlockType.Air && BlockType != BlockType.Leaf;

        /// <returns>Block as a GameObject</returns>
        /// <summary>
        /// Create a world-space block GameObject (not a chunk voxel) of type and spawn it at the specified position.
        /// </summary>
        /// <typeparam name="T1">Type of Collider to apply.</typeparam>
        /// <typeparam name="T2">GameObject script to apply.</typeparam>
        /// <param name="data">Data to construct the block with.</param>
        /// <returns>The block GameObject.</returns>
        public static InstantiateBlockOutputData<T2> InstantiateWorldBlock<T1, T2>(InstantiateBlockInputData data)
            where T1 : Collider
            where T2 : MonoBehaviour
        {
            if (data.BlockType == BlockType.Air)
            {
                Debug.LogWarning("Attempted to create an air block. Are you sure this is intended?");
                return default;
            }

            GameObject block = new GameObject("Block");
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Left));
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Right));
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Bottom));
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Top));
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Back));
            CreateQuad(new BlockCreationData(block.transform, data.BlockType, BlockSide.Front));
            MeshComponents components = MeshUtils.CombineMesh<BoxCollider>(block, ReferenceManager.Instance.BlockAtlas);

            // Block parent
            GameObject blockParent = new GameObject($"Block_{data.BlockType}_{data.Position}");
            SetTransformAndLayer(blockParent.transform, Vector3.one);
            blockParent.AddComponent(typeof(T1));
            Rigidbody rigidbody = blockParent.AddComponent(typeof(Rigidbody)) as Rigidbody;
            blockParent.tag = "Pickup";

            // Block itself
            SetTransformAndLayer(block.transform, data.LocalScale);
            block.transform.SetParent(blockParent.transform);
            T2 obj = block.AddComponent(typeof(T2)) as T2;

            return new InstantiateBlockOutputData<T2>(components, rigidbody, obj);

            void SetTransformAndLayer(Transform blockTransform, Vector3 scale)
            {
                blockTransform.position = data.Position;
                blockTransform.localScale = scale;
                blockTransform.gameObject.layer = LayerMask.NameToLayer("Pickup");
            }
        }

        public Block GetBlockNeighbour(Neighbour neighbour)
        {
            switch (neighbour)
            {
                case Neighbour.Left:
                    return GetBlock(Position.x - 1, Position.y, Position.z);

                case Neighbour.Right:
                    return GetBlock(Position.x + 1, Position.y, Position.z);

                case Neighbour.Bottom:
                    return GetBlock(Position.x, Position.y - 1, Position.z);

                case Neighbour.Top:
                    return GetBlock(Position.x, Position.y + 1, Position.z);

                case Neighbour.Back:
                    return GetBlock(Position.x, Position.y, Position.z - 1);

                case Neighbour.Front:
                    return GetBlock(Position.x, Position.y, Position.z + 1);
            }

            return null;
        }

        public Dictionary<Neighbour, Block> GetAllBlockNeighbours()
               => new Dictionary<Neighbour, Block>
               {
                   { Neighbour.Left, GetBlockNeighbour(Neighbour.Left) },
                   { Neighbour.Right, GetBlockNeighbour(Neighbour.Right) },
                   { Neighbour.Bottom, GetBlockNeighbour(Neighbour.Bottom) },
                   { Neighbour.Top, GetBlockNeighbour(Neighbour.Top) },
                   { Neighbour.Back, GetBlockNeighbour(Neighbour.Back) },
                   { Neighbour.Front, GetBlockNeighbour(Neighbour.Front) }
               };

        public void BuildBlock()
        {
            if (BlockType == BlockType.Air)
            {
                UpdateSolidity();
                return;
            }

            CheckNeighbours();
        }

        private void CheckNeighbours()
        {
            List<Vector3> quadPositions = new List<Vector3>(maxQuadCount);

            // Front quad
            Vector3Int currentQuadPos = new Vector3Int(Position.x, Position.y, Position.z + 1);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Front);

            // Back quad
            currentQuadPos = new Vector3Int(Position.x, Position.y, Position.z - 1);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Back);

            // Left quad
            currentQuadPos = new Vector3Int(Position.x - 1, Position.y, Position.z);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Left);

            // Right quad
            currentQuadPos = new Vector3Int(Position.x + 1, Position.y, Position.z);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Right);

            // Top quad
            currentQuadPos = new Vector3Int(Position.x, Position.y + 1, Position.z);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Top);

            // Bottom quad
            currentQuadPos = new Vector3Int(Position.x, Position.y - 1, Position.z);
            CheckSide(quadPositions, currentQuadPos, BlockSide.Bottom);

            CalculateBlockPositionAverage(quadPositions);
        }

        private void CalculateBlockPositionAverage(List<Vector3> quadPositions)
        {
            Vector3 blockPositionAverageTemp = Vector3.zero;
            for (int i = 0; i < quadPositions.Count; i++)
            {
                blockPositionAverageTemp.x += ChunkOwner.GameObject.transform.position.x + quadPositions[i].x;
                blockPositionAverageTemp.y += ChunkOwner.GameObject.transform.position.y + quadPositions[i].y;
                blockPositionAverageTemp.z += ChunkOwner.GameObject.transform.position.z + quadPositions[i].z;
            }

            blockPositionAverageTemp.x /= maxQuadCount;
            blockPositionAverageTemp.y /= maxQuadCount;
            blockPositionAverageTemp.z /= maxQuadCount;
            WorldPositionAverage = blockPositionAverageTemp;
        }

        private void CheckSide(List<Vector3> quadPositions, Vector3Int quadPos, BlockSide side)
        {
            (bool isSolid, Block neighbourBlock) = HasSolidNeighbour(quadPos.x, quadPos.y, quadPos.z);

            // Conditions for not creating faces at the edge of the world when there is water
            bool fluidWorldEdge = neighbourBlock != null && (!isSolid || (BlockType != BlockType.Fluid && neighbourBlock.BlockType == BlockType.Fluid));
            // Conditions for reating faces at the edge of the world when there is sand (fixes a invisible face bug)
            bool otherWorldEdge = BlockType != BlockType.Fluid && neighbourBlock == null;
            if (fluidWorldEdge || otherWorldEdge)
            {
                CreateQuad(new BlockCreationData(ChunkGameObject.transform, BlockType, side)
                {
                    Position = Position
                });
            }

            quadPositions.Add(quadPos);
        }

        private (bool, Block) HasSolidNeighbour(int x, int y, int z)
        {
            try
            {
                Block neighbourBlock = GetBlock(x, y, z);
                if (neighbourBlock != null)
                {
                    return (neighbourBlock.IsSolid, neighbourBlock);
                }

                return (false, null);
            }
            catch (NullReferenceException)
            {
                return (false, null);
            }
        }

        public Block GetBlock(int x, int y, int z)
        {
            int chunkSize = WorldManager.Instance.ChunkSize - 1;
            Block[,,] chunkData;
            if (x < 0 || x >= chunkSize
                || y < 0 || y >= chunkSize
                || z < 0 || z >= chunkSize)
            {
                Vector3 neighbouringChunkPosition = ChunkGameObject.transform.position
                                                  + new Vector3((x - Position.x) * chunkSize,
                                                                (y - Position.y) * chunkSize,
                                                                (z - Position.z) * chunkSize);
                x = ConvertBlockCoordinates(x, chunkSize);
                y = ConvertBlockCoordinates(y, chunkSize);
                z = ConvertBlockCoordinates(z, chunkSize);

                Chunk chunk = WorldManager.Instance.GetChunk(neighbouringChunkPosition);
                if (chunk != null)
                {
                    chunkData = chunk.GetChunkData();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                chunkData = ChunkOwner.GetChunkData();
            }

            if (chunkData != null
                && x <= chunkData.GetUpperBound(0)
                && x >= chunkData.GetLowerBound(0)
                && y <= chunkData.GetUpperBound(1)
                && y >= chunkData.GetLowerBound(1)
                && z <= chunkData.GetUpperBound(2)
                && z >= chunkData.GetLowerBound(2))
            {
                return chunkData[x, y, z];
            }

            return null;
        }

        /// <summary>
        /// Convert the given block coordinate from local to neighbour if needed.
        /// </summary>
        private int ConvertBlockCoordinates(int index, int chunkSize)
        {
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

        /// <summary>
        /// Create a one-sided quad with the supplied data.
        /// </summary>
        /// <param name="data">Data to build the quad with.</param>
        /// <returns>The quad as a GameObject.</returns>
        public static GameObject CreateQuad(BlockCreationData data)
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

                    case BlockSide.Front:
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
                return quad;
            }
            catch (NullReferenceException e)
            {
                Debug.LogWarning(e);
                return default;
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
            if (blockType == BlockType.Fluid)
            {
                uvs[0] = uvAtlasMap[6, 0];
                uvs[1] = uvAtlasMap[6, 1];
                uvs[2] = uvAtlasMap[6, 2];
                uvs[3] = uvAtlasMap[6, 3];
            }
            else if (blockType == BlockType.Grass && (side == BlockSide.Back
                                                      || side == BlockSide.Front
                                                      || side == BlockSide.Left
                                                      || side == BlockSide.Right))
            {
                uvs[0] = uvAtlasMap[5, 0];
                uvs[1] = uvAtlasMap[5, 1];
                uvs[2] = uvAtlasMap[5, 2];
                uvs[3] = uvAtlasMap[5, 3];
            }
            else if (blockType == BlockType.Grass
                     && side == BlockSide.Top)
            {
                uvs[0] = uvAtlasMap[0, 0];
                uvs[1] = uvAtlasMap[0, 1];
                uvs[2] = uvAtlasMap[0, 2];
                uvs[3] = uvAtlasMap[0, 3];
            }
            else if (blockType == BlockType.Dirt
                     || blockType == BlockType.Grass)
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
            else if (blockType == BlockType.Sand)
            {
                uvs[0] = uvAtlasMap[7, 0];
                uvs[1] = uvAtlasMap[7, 1];
                uvs[2] = uvAtlasMap[7, 2];
                uvs[3] = uvAtlasMap[7, 3];
            }
            else if (blockType == BlockType.TreeBase
                     || blockType == BlockType.Wood)
            {
                uvs[0] = uvAtlasMap[8, 0];
                uvs[1] = uvAtlasMap[8, 1];
                uvs[2] = uvAtlasMap[8, 2];
                uvs[3] = uvAtlasMap[8, 3];
            }
            else if (blockType == BlockType.Leaf)
            {
                uvs[0] = uvAtlasMap[9, 0];
                uvs[1] = uvAtlasMap[9, 1];
                uvs[2] = uvAtlasMap[9, 2];
                uvs[3] = uvAtlasMap[9, 3];
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