using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Utility
{
    public abstract class Marching : IMarching
    {
        public float Surface { get; set; }
        private float[] Cube { get; set; }

        /// <summary>
        /// Winding order of triangles use 2,1,0 or 0,1,2
        /// </summary>
        private int[] windingOrder;
        public int[] GetWindingOrder()
        {
            return windingOrder;
        }

        protected Marching(float surface = 0.5f)
        {
            Surface = surface;
            Cube = new float[8];
            windingOrder = new int[] { 0, 1, 2 };
        }

<<<<<<< HEAD
        public virtual void Generate(IList<float> voxels, int size, IList<Vector3> verts, IList<int> indices)
        {
             windingOrder = new int[3];
=======
        public virtual void Generate(float[,,][] voxels, int size, IList<Vector3> verts, IList<int> indices)
        {
            Debug.Log(voxels[0, 0, 0]);
>>>>>>> master
            if (Surface > 0.0f)
            {
                windingOrder[0] = 0;
                windingOrder[1] = 1;
                windingOrder[2] = 2;
            }
            else
            {
                windingOrder[0] = 2;
                windingOrder[1] = 1;
                windingOrder[2] = 0;
            }

            int x, y, z, i;
            int ix, iy, iz;
<<<<<<< HEAD
            for (x = 0; x < size - 1; x++)
            {
                for (y = 0; y < size - 1; y++)
                {
                    for (z = 0; z < size - 1; z++)
                    {
                        // Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++)
                        {
                            ix = x + VertexOffset[i, 0];
                            iy = y + VertexOffset[i, 1];
                            iz = z + VertexOffset[i, 2];
                            Cube[i] = voxels[ix + iy * size + iz * size * size];
                        }
=======
            for (x = 0; x < size; x++)
            {
                for (y = 0; y < size; y++)
                {
                    for (z = 0; z < size; z++)
                    {
                        //Get the values in the 8 neighbours which make up a cube
                        //for (i = 0; i < 8; i++)
                        //{
                        //    ix = x + VertexOffset[i, 0];
                        //    iy = y + VertexOffset[i, 1];
                        //    iz = z + VertexOffset[i, 2];
>>>>>>> master

                        //    Cube = voxels[ix + iy * width + iz * width * height];
                        //}
                        Cube = voxels[x, y, z];
                        //Perform algorithm
                        March(x, y, z, Cube, verts, indices);
                    }
                }
            }
        }

        /// <summary>
        /// MarchCube performs the Marching algorithm on a single cube
        /// </summary>
        protected abstract void March(float x, float y, float z, float[] cube, IList<Vector3> vertList, IList<int> indexList);

        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        protected virtual float GetOffset(float v1, float v2)
        {
            float delta = v2 - v1;
            return (delta == 0.0f) ? Surface : (Surface - v1) / delta;
        }

        /// <summary>
        /// VertexOffset lists the positions, relative to vertex0, 
        /// of each of the 8 vertices of a cube.
        /// vertexOffset[8][3]
        /// </summary>
        protected static readonly int[,] VertexOffset = new int[,]
        {
            {0, 0, 0},{1, 0, 0},{1, 1, 0},{0, 1, 0},
            {0, 0, 1},{1, 0, 1},{1, 1, 1},{0, 1, 1}
        };
    }
}
