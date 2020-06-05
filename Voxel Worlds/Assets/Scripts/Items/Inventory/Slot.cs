using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.World;

namespace Voxel.Items.Inventory
{
    public enum InputDigit
    {
        Key1,
        Key2,
        Key3,
        Key4,
        Key5
    }

    public struct SelectedItemData
    {
        public bool ValidItemSelected { get; }
        public BlockType SelectedBlockType { get; }

        public SelectedItemData(bool hasSelectedItem, BlockType selectedBlockType)
        {
            ValidItemSelected = hasSelectedItem;
            SelectedBlockType = selectedBlockType;
        }
    }

    public class Slot : MonoBehaviour
    {
        public delegate void OnSelectedItemChanged(SelectedItemData selectedItemData);
        /// <summary>
        /// Event that gets called when the selected block type gets changed (inventory).
        /// </summary>
        public static event OnSelectedItemChanged OnSelectedItemChangedEvent;

        [SerializeField]
        private GameObject slotSelectedImage = default;
        public GameObject SlotSelectedImage => slotSelectedImage;

        [SerializeField]
        private InputDigit inputKey = default;

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
        public bool IsEmpty => Amount == 0;

        public void InvokeOnSelectedItemChanged(SelectedItemData selectedItemData) 
            => OnSelectedItemChangedEvent?.Invoke(selectedItemData);

        public void Deactivate()
        {
            SlotSelectedImage.SetActive(false);
            Destroy(BlockImage);
        }

        private void Update() => UpdateInput();

        private void UpdateInput()
        {
            switch (inputKey)
            {
                case InputDigit.Key1:
                    CheckInput(() => Keyboard.current.digit1Key.wasPressedThisFrame);
                    break;

                case InputDigit.Key2:
                    CheckInput(() => Keyboard.current.digit2Key.wasPressedThisFrame);
                    break;

                case InputDigit.Key3:
                    CheckInput(() => Keyboard.current.digit3Key.wasPressedThisFrame);
                    break;

                case InputDigit.Key4:
                    CheckInput(() => Keyboard.current.digit4Key.wasPressedThisFrame);
                    break;

                case InputDigit.Key5:
                    CheckInput(() => Keyboard.current.digit5Key.wasPressedThisFrame);
                    break;
            }
        }

        private void CheckInput(Func<bool> input)
        {
            if (input() && !IsEmpty)
            {
                SlotSelectedImage.SetActive(true);
                OnSelectedItemChangedEvent?.Invoke(new SelectedItemData(true, BlockType));
            }
        }
    }
}