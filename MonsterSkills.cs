using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MonsterAI2D;
public class MonsterSkills : Monster
{
    [Header("Melee Dash Settings")]
    protected float dashSpeed = 10f;           // Maximum dash speed (melee only)
    public float dashDeceleration = 20f;    // How quickly the dash slows down (melee only)
    public float dashCooldown = 2f;         // Time between dashes (melee only)
    private float lastDashTime = -10f;
    protected internal bool isDashing = false;
    private float currentDashSpeed = 0f;


    // Determines if the melee monster should dash
    protected internal bool ShouldDashAtPlayer()
    {
        if (player == null) // Add null check here
        {
            return false; // Exit the method if the player is null
        }
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        return distanceToPlayer <= monsterStats.AttackRange
            && Time.time - lastDashTime >= dashCooldown
            && !isDashing;
    }

    // Call this to start a dash towards the player (melee only)
    protected internal void TryDashAtPlayer()
    {
        isDashing = true;
        currentDashSpeed = dashSpeed;
        lastDashTime = Time.time;
    }


}