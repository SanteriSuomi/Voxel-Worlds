using UnityEngine;
using Voxel.Noise;

public class noiseutilstest : MonoBehaviour
{
    Terrain terrain;
    NoiseUtils noise;

    private void Awake()
    {
        noise = new NoiseUtils();

        terrain = GetComponent<Terrain>();
        float[,] terrainheightmap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        for (int i = 0; i < terrainheightmap.GetLength(0); i++)
        {
            for (int j = 0; j < terrainheightmap.GetLength(1); j++)
            {
                terrainheightmap[i, j] = NoiseUtils.Instance.fBm2D(i, j);
            }
        }

        terrain.terrainData.SetHeights(0, 0, terrainheightmap);
    }
}