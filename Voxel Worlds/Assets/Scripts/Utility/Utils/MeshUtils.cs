using UnityEngine;
using Object = UnityEngine.Object;

namespace Voxel.Utility
{
    public struct MeshComponents
    {
        public MeshFilter MeshFilter { get; set; }
        public MeshRenderer MeshRenderer { get; set; }
        public Collider Collider { get; set; }
    }

    public static class MeshUtils
    {
        /// <summary>
        /// Combine meshes children to one mesh. Add MeshFilter, MeshRenderer and a Collider and return them.
        /// </summary>
        /// <typeparam name="T">Type of collider to apply to the mesh.</typeparam>
        /// <param name="gameObject">GameObject whose children will get combined.</param>
        /// <param name="material">Material to apply to the finished mesh.</param>
        /// <returns>Data that contains components of the mesh.</returns>
        public static MeshComponents CombineMesh<T>(GameObject gameObject, Material material) where T: Collider
        {
            int childCount = gameObject.transform.childCount;
            CombineInstance[] combinedMeshes = new CombineInstance[childCount];
            for (int i = 0; i < childCount; i++)
            {
                Transform child = gameObject.transform.GetChild(i);
                MeshFilter childMeshFilter = child.GetComponent<MeshFilter>();
                combinedMeshes[i].mesh = childMeshFilter.sharedMesh;
                combinedMeshes[i].transform = childMeshFilter.transform.localToWorldMatrix;
                Object.Destroy(child.gameObject); // Get rid of redundant children
            }

            MeshComponents data = new MeshComponents();
            MeshFilter parentMeshFilter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            data.MeshFilter = parentMeshFilter;
            parentMeshFilter.mesh = new Mesh();
            parentMeshFilter.mesh.CombineMeshes(combinedMeshes, true, true);
            MeshRenderer parentMeshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            data.MeshRenderer = parentMeshRenderer;
            parentMeshRenderer.material = material;
            data.Collider = gameObject.AddComponent(typeof(T)) as T;
            return data;
        }
    }
}