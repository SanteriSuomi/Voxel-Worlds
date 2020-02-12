using System.Collections.Generic;
using UnityEngine;

namespace Voxel.Utility
{
    public interface IMarching
    {
        float Surface { get; set; }
<<<<<<< HEAD
        void Generate(float[,,][] voxels, int size, IList<Vector3> verts, IList<int> indices);
=======
        void Generate(IList<float> voxels, int size, IList<Vector3> verts, IList<int> indices);
>>>>>>> branchtemp
    }
}