using System.Collections;
using UnityEngine;

namespace Voxel.World
{
    public class World : MonoBehaviour
    {
        [SerializeField]
        private GameObject block = default;
        [SerializeField]
        private int worldSize = 2;

        private void Start()
        {
            StartCoroutine(BuildWorld());
        }

        public IEnumerator BuildWorld()
        {
            for (int z = 0; z < worldSize; z++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    for (int x = 0; x < worldSize; x++)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        GameObject worldBlock = Instantiate(block, position, Quaternion.identity);
                        worldBlock.name = $"{x} {y} {z}";
                    }

                    yield return null;
                }
            }
        }
    }
}