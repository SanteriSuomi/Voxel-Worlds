using UnityEngine;
using UnityEngine.UI;
using Voxel.World;

namespace Voxel.Items.Inventory
{
    public class Slot : MonoBehaviour
    {
        [SerializeField]
        private int index = 0;
        public int Index => index;

        [SerializeField]
        private int maxAmount = 30;
        public int MaxAmount => maxAmount;

        /// <summary>
        /// The slot image is a mesh quad of the block.
        /// </summary>
        public GameObject BlockImage { get; set; }

        public BlockType BlockType { get; set; }
        public int Amount { get; set; }
        public bool Empty => Amount == 0;
    }
}