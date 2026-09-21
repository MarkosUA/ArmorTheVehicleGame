using UnityEngine;

namespace ArmorTheVehicle.Config
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Armor The Vehicle/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Level")]
        public float levelLength = 200f;
        public int enemyCount = 12;
        public float minEnemySpacing = 8f;
        public float roadHalfWidth = 4f;
        public float spawnMargin = 15f;
        public float groundTileOverlap = 0.05f;

        [Header("Car")]
        public float carMaxHealth = 100f;
        public float carForwardSpeed = 10f;
        public float carStartEaseDuration = 0.5f;

        [Header("Turret")]
        public float turretRotationSpeed = 180f;
        public float turretMinAngle = -60f;
        public float turretMaxAngle = 60f;

        [Header("Weapon")]
        public float fireRate = 3f;
        public float projectileSpeed = 40f;
        public float projectileDamage = 10f;
        public float projectileLifetime = 3f;

        [Header("Enemy")]
        public float enemyMaxHealth = 30f;
        public float enemyAggroRadius = 15f;
        public float enemyAttackRange = 2f;
        public float enemyMoveSpeed = 4f;
        public float enemyAttackDamage = 5f;
        public float enemyAttackAnimationDuration = 0.5f;
        public float enemyDeathDespawnDelay = 3.2f;

        [Header("Enemy Wandering")]
        public float enemyWalkSpeed = 1.8f;
        public float enemyWanderRadius = 3f;
        public float enemyWanderIdleMinTime = 2f;
        public float enemyWanderIdleMaxTime = 5f;
        public float enemyWanderChance = 0.5f;

        [Header("Flow")]
        public float resultFadeDuration = 0.4f;
    }
}
