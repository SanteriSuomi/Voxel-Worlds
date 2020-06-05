using System;
using UnityEngine;
using Voxel.Saving;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Items.Inventory
{
    [Serializable]
    public class InventoryData
    {
        public SlotData[] SlotData { get; set; }

        public InventoryData(int slotDataLength)
        {
            SlotData = new SlotData[slotDataLength];
        }

        public SlotData this[int i]
        {
            get => SlotData[i];
            set => SlotData[i] = value;
        }
    }

    [Serializable]
    public class SlotData
    {
        public BlockType BlockType { get; }
        public int Amount { get; }

        public SlotData(BlockType blockType, int amount)
        {
            BlockType = blockType;
            Amount = amount;
        }
    }

    public class InventoryManager : Singleton<InventoryManager>
    {
        [SerializeField]
        private Slot[] slots = default;
        [SerializeField]
        private Vector3 quadBlockImageLocalScale = new Vector3(90, 90, 1);

        [SerializeField]
        private string inventorySaveFileName = "InventoryData.dat";

        protected override void Awake()
        {
            base.Awake();
            SetSlotIndexes();
        }

        private void SetSlotIndexes()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Index = i;
            }
        }

        public void Save()
        {
            InventoryData invData = new InventoryData(slots.Length + 1);
            for (int i = 0; i < slots.Length; i++)
            {
                invData[i] = new SlotData(slots[i].BlockType, slots[i].Amount);
            }

            SaveManager.Instance.Save(invData, SaveManager.Instance.BuildFilePath(inventorySaveFileName));
        }

        public void Load()
        {
            (bool hasSave, InventoryData invData) = SaveManager.Instance.Load<InventoryData>(SaveManager.Instance.BuildFilePath(inventorySaveFileName));
            if (hasSave)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i].BlockType = invData[i].BlockType;
                    slots[i].Amount = invData[i].Amount;
                    if (slots[i].Amount > 0)
                    {
                        slots[i].BlockImage = ApplySlotBlockImage(slots[i], slots[i].BlockType);
                    }
                }
            }
        }

        public void Add(BlockType addBlockType)
        {
            addBlockType = ChangeBlockType(addBlockType);
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (slot.IsEmpty)
                {
                    slot.Amount++;
                    slot.BlockType = addBlockType;
                    slot.BlockImage = ApplySlotBlockImage(slot, addBlockType);
                    break;
                }
                else if (!slot.IsEmpty && slot.BlockType == addBlockType)
                {
                    slot.Amount++;
                    break;
                }
            }
        }

        public void Remove(BlockType removeBlockType)
        {
            removeBlockType = ChangeBlockType(removeBlockType);
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (!slot.IsEmpty && slot.BlockType == removeBlockType)
                {
                    slot.Amount--;
                    if (slot.IsEmpty)
                    {
                        slot.Deactivate();
                        slot.InvokeOnSelectedItemChanged(new SelectedItemData(false, removeBlockType));
                    }

                    break;
                }
            }
        }

        public void ChangeSelectedImage(int toIndex)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i == toIndex)
                {
                    slots[i].SlotSelectedImage.SetActive(true);
                    continue;
                }

                slots[i].SlotSelectedImage.SetActive(false);
            }
        }

        private static BlockType ChangeBlockType(BlockType addBlockType)
        {
            BlockType newBlockType = addBlockType;
            if (newBlockType == BlockType.Grass)
            {
                newBlockType = BlockType.Dirt;
            }

            return newBlockType;
        }

        private GameObject ApplySlotBlockImage(Slot slot, BlockType blockType)
        {
            GameObject quadBlockTexture = Block.CreateQuad(new BlockCreationData(null, blockType, BlockSide.Back));
            MeshRenderer meshRenderer = quadBlockTexture.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            meshRenderer.material = ReferenceManager.Instance.BlockAtlas;
            quadBlockTexture.transform.parent = slot.transform;
            quadBlockTexture.transform.localPosition = Vector3.zero;
            quadBlockTexture.transform.localRotation = Quaternion.identity;
            quadBlockTexture.transform.localScale = quadBlockImageLocalScale;
            return quadBlockTexture;
        }
    }
}