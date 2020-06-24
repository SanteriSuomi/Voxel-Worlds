using UnityEngine;

namespace Voxel.Characters
{
    public class Character : MonoBehaviour
    {
        [SerializeField]
        private float startingHealth = 100;
        public float StartingHealth => startingHealth;

        private float health;
        public float Health
        {
            get => health;
            set
            {
                health = value;
                OnHealthChanged();
            }
        }

        protected virtual void OnHealthChanged(){}

        private void Awake() => health = startingHealth;
    }
}