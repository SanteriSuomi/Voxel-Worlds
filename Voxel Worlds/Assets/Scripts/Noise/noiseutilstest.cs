using UnityEngine;
using Voxel.Noise;

public class noiseutilstest : MonoBehaviour
{
    Terrain terrain;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        float[,] terrainheightmap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        for (int i = 0; i < terrainheightmap.GetLength(0); i++)
        {
            for (int j = 0; j < terrainheightmap.GetLength(1); j++)
            {
                //float xPos = i / (float)terrain.terrainData.heightmapResolution;
                //float yPos = j / (float)terrain.terrainData.heightmapResolution;
                terrainheightmap[i, j] = NoiseUtils.fBm2D(i, j);
                //Debug.Log(terrainheightmap[i, j]);
            }
        }

        terrain.terrainData.SetHeights(0, 0, terrainheightmap);
    }
}
