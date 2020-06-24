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
                OnHealthChanged(health);
            }
        }

        protected virtual void OnHealthChanged(float health){}

        private void Awake() => health = startingHealth;
    }
}