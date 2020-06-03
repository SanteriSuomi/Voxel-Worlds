using UnityEngine;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Items.Inventory
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        [SerializeField]
        private Slot[] slots = default;

        [SerializeField]
        private Vector3 quadBlockImageLocalScale = new Vector3(90, 90, 1);

        public void Add(BlockType addBlockType)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (slot.Empty)
                {
                    slot.BlockType = addBlockType;
                    slot.Amount++;
                    slot.BlockImage = AddBlockSlotImage(addBlockType, slot);
                    break;
                }
                else if (!slot.Empty && slot.BlockType == addBlockType)
                {
                    slot.Amount++;
                    break;
                }
            }
        }

        private GameObject AddBlockSlotImage(BlockType addBlockType, Slot slot)
        {
            GameObject quadBlockTexture = Block.CreateQuad(new BlockCreationData(null, addBlockType, BlockSide.Back));
            MeshRenderer meshRenderer = quadBlockTexture.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            meshRenderer.material = ReferenceManager.Instance.BlockAtlas;
            quadBlockTexture.transform.parent = slot.transform;
            quadBlockTexture.transform.localPosition = Vector3.zero;
            quadBlockTexture.transform.localRotation = Quaternion.identity;
            quadBlockTexture.transform.localScale = quadBlockImageLocalScale;
            return quadBlockTexture;
        }

        public void Remove(BlockType removeBlockType)
        {
            // TODO: implement
        }
    }
}