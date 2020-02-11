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
        protected int[] WindingOrder { get; private set; }

        protected Marching(float surface = 0.5f)
        {
            Surface = surface;
            Cube = new float[8];
            WindingOrder = new int[] { 0, 1, 2 };
        }

        public virtual void Generate(float[,,][] voxels, int size, IList<Vector3> verts, IList<int> indices)
        {
            Debug.Log(voxels[0, 0, 0]);
            if (Surface > 0.0f)
            {
                WindingOrder[0] = 0;
                WindingOrder[1] = 1;
                WindingOrder[2] = 2;
            }
            else
            {
                WindingOrder[0] = 2;
                WindingOrder[1] = 1;
                WindingOrder[2] = 0;
            }

            int x, y, z, i;
            int ix, iy, iz;
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
