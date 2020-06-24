using UnityEngine;
using UnityEngine.UI;
using Voxel.Characters;
using Voxel.Characters.Interfaces;
using Voxel.Utility;

namespace Voxel.Player
{
    public class Player : Character, IDamageable
    {
        private Slider healthbar;

        private void Awake()
        {
            healthbar = ReferenceManager.Instance.PlayerHealthbar;
            healthbar.maxValue = StartingHealth;
            healthbar.value = StartingHealth;
        }

        protected override void OnHealthChanged(float health) => healthbar.value = health;

        public void Damage(float damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                PlayerManager.Instance.RespawnAndResetPlayer();
            }
        }
    }
}