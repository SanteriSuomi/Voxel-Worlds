using System;
using UnityEngine;

namespace Voxel.Characters.Saving
{
    [Serializable]
    public class CharacterData
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public CharacterData() { }

        public CharacterData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}