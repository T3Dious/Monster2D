using UnityEngine;

[CreateAssetMenu(fileName = "MonsterStats", menuName = "Scriptable Objects/MonsterStats")]
public class MonsterStats : ScriptableObject
{
    public string MonsterName; // Name of the monster
    public string MonsterDescription; // Description of the monster
    [Header("Monster Stats")]
    public float Health = 50f; // Health of the monster
    public int Level = 1; // Level of the monster
    public int ExperiencePoints = 3; // Tracks the current experience points
    public MonsterType monsterType; // Type of the monster (e.g., melee, ranged, magic)
    [Header("Rotation Settings")]
    public float RotationSpeed = 5f; // Controls how quickly the monster rotates
    public float RotationThreshold = 5f; // Minimum angle difference before rotating

    [Header("Movement Smoothing")]
    public float Acceleration = 5f;
    public float Deceleration = 7f;
    public float MoveSpeed = 2f; // Speed of the monster
    public float DetectionRange = 2f;
    public float AvoidanceAngle = 45f;
    public float ObstacleCheckDistance = 1f;
    public float PathUpdateInterval = 0.5f;
    public float MinMonsterDistance = 1.5f; // Minimum distance to maintain from other monsters
    public float AvoidanceForce = 2f; // Strength of avoidance push

    [Header("Combat Settings")]
    public float AttackDamage = 10; // Damage dealt by the monster's attack
    public float AttackCooldown = 1.0f; // Time between attacks

    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab; // Assign your projectile prefab in the Inspector
    public float AttackRange = 1.5f; // Range of the monster's attack

    public enum MonsterType
    {
        Melee,
        Ranged,
        Magic
    }
}


