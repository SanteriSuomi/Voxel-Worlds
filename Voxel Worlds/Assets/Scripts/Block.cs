using UnityEngine;

namespace Voxel.World
{
    public enum BlockType
    {
        Grass,
        Dirt,
        Stone
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

        public bool IsSolid { get; set; }
        private readonly Material material;
        private readonly GameObject parent;
        private readonly Chunk chunk;
        private readonly Vector3 position;
        private readonly BlockType type;

        // UV coordinates on the UV atlas
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

        public Block(BlockType type, Vector3 position, GameObject parent, Material material)
        {
            this.type = type;
            this.position = position;
            this.parent = parent;
            chunk = parent.GetComponentInChildren<Chunk>();
            this.material = material;
            IsSolid = true;
        }

        private bool HasSolidNeighbour(int x, int y, int z)
        {
            Block[,,] chunks = chunk.GetChunkData();
            if (x <= chunks.GetUpperBound(0)
                && x >= chunks.GetLowerBound(0)
                && y <= chunks.GetUpperBound(1)
                && y >= chunks.GetLowerBound(1)
                && z <= chunks.GetUpperBound(2)
                && z >= chunks.GetLowerBound(2)
                && chunks[x, y, z] != null)
            {
                return chunks[x, y, z].IsSolid;
            }

            return false;
        }

        public void CreateCube()
        {
            int posX = (int)position.x;
            int posY = (int)position.y;
            int posZ = (int)position.z;

            // Create all sides of a cube
            if (!HasSolidNeighbour(posX, posY, posZ + 1))
            {
                CreateQuad(CubeSide.Front);
            }
            if (!HasSolidNeighbour(posX, posY, posZ - 1))
            {
                CreateQuad(CubeSide.Back);
            }
            if (!HasSolidNeighbour(posX - 1, posY, posZ))
            {
                CreateQuad(CubeSide.Left);
            }
            if (!HasSolidNeighbour(posX + 1, posY, posZ))
            {
                CreateQuad(CubeSide.Right);
            }
            if (!HasSolidNeighbour(posX, posY + 1, posZ))
            {
                CreateQuad(CubeSide.Top);
            }
            if (!HasSolidNeighbour(posX, posY - 1, posZ))
            {
                CreateQuad(CubeSide.Bottom);
            }
        }

        private void CreateQuad(CubeSide side)
        {
            Vector3[] vertices = new Vector3[4];
            Vector3[] normals = new Vector3[4];

            // All possible points on a cube made out of quads
            Vector3 vert00 = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 vert01 = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 vert02 = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 vert03 = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 vert04 = new Vector3(-0.5f, 0.5f, 0.5f);
            Vector3 vert05 = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 vert06 = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 vert07 = new Vector3(-0.5f, 0.5f, -0.5f);

            // Build the quad and assign it's normals
            switch (side)
            {
                case CubeSide.Bottom:
                    vertices = new Vector3[]
                    {
                        vert00, vert01, vert02, vert03
                    };

                    AssignNormals(Vector3.down);
                    break;
                case CubeSide.Top:
                    vertices = new Vector3[]
                    {
                        vert07, vert06, vert05, vert04
                    };

                    AssignNormals(Vector3.up);
                    break;
                case CubeSide.Left:
                    vertices = new Vector3[]
                    {
                        vert07, vert04, vert00, vert03
                    };

                    AssignNormals(Vector3.left);
                    break;
                case CubeSide.Right:
                    vertices = new Vector3[]
                    {
                        vert05, vert06, vert02, vert01
                    };

                    AssignNormals(Vector3.right);
                    break;
                case CubeSide.Front:
                    vertices = new Vector3[]
                    {
                        vert01, vert00, vert04, vert05
                    };

                    AssignNormals(Vector3.forward);
                    break;
                case CubeSide.Back:
                    vertices = new Vector3[]
                    {
                        vert06, vert07, vert03, vert02
                    };

                    AssignNormals(Vector3.back);
                    break;
            }

            void AssignNormals(Vector3 direction)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = direction;
                }
            }

            Vector2[] uvs = new Vector2[4];
            // Assign UVs from the atlas
            if (type == BlockType.Grass && side == CubeSide.Top)
            {
                uvs[0] = uvAtlasMap[0, 0];
                uvs[1] = uvAtlasMap[0, 1];
                uvs[2] = uvAtlasMap[0, 2];
                uvs[3] = uvAtlasMap[0, 3];
            }
            else if (type == BlockType.Dirt || type == BlockType.Grass)
            {
                uvs[0] = uvAtlasMap[1, 0];
                uvs[1] = uvAtlasMap[1, 1];
                uvs[2] = uvAtlasMap[1, 2];
                uvs[3] = uvAtlasMap[1, 3];
            }
            else if (type == BlockType.Stone)
            {
                uvs[0] = uvAtlasMap[2, 0];
                uvs[1] = uvAtlasMap[2, 1];
                uvs[2] = uvAtlasMap[2, 2];
                uvs[3] = uvAtlasMap[2, 3];
            }
            else
            {
                int typeToInt = (int)type;
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
            quad.transform.position = position;
            quad.transform.SetParent(parent.transform);
            MeshFilter meshFilter = quad.AddComponent(typeof(MeshFilter)) as MeshFilter;
            meshFilter.mesh = mesh;
            MeshRenderer renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material = material;
        }
    }
}