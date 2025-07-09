using UnityEngine;
using MonsterAI2D; // Ensure you have the correct namespace for MonsterAI2D
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

using UnityEngine;
using MonsterAI2D; // Ensure you have the correct namespace for MonsterAI2D
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class MonsterBehavoiur : Monster
{
    [Header("Patrolling")]
    public List<Transform> patrolPoints; // Assign patrol points in the editor
    private int currentPatrolIndex = 0;
    public float patrolPointReachedThreshold = 1f; // Distance to consider a patrol point reached

    // Make all methods 'protected internal' so they are accessible only to MonsterAI2D and its derived classes
    protected internal void DetectingThePlayerPosition()
    {
        if (player == null) // Add null check here
            return; // Exit the method if the player is null

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Only move and look at the player if within detection (scope) area
        if (distanceToPlayer <= monsterStats.DetectionRange)
        {
            Vector2 avoidance = CalculateAvoidanceForce();

            // Use firePoint as the reference for direction
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 rawDirection = ((Vector2)player.position - origin).normalized;
            float distance = Vector2.Distance(origin, player.position);

            RaycastHit2D hit = Physics2D.Raycast(origin, rawDirection, distance, ~LayerMask.GetMask("Monster"));
            Vector2 directionToPlayer;

            if (hit.collider == null || hit.collider.transform == player)
            {
                // Direct line of sight to player
                directionToPlayer = rawDirection;
            }
            else
            {
                // Obstacle in the way, steer towards the hit point (edge of obstacle)
                directionToPlayer = ((Vector2)hit.point - origin).normalized;
            }

            Vector2 combinedDirection = (directionToPlayer + avoidance).normalized;
            rb.linearVelocity = combinedDirection * monsterStats.MoveSpeed;

            // Rotate the monster so that the firePoint looks towards the player
            if (firePoint != null)
            {
                Vector2 lookDir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                SmoothRotateTowardsMovement();
            }
            AlertNearbyMonsters(); // Alert others when player is detected
        }
        else
        {
            // Player is outside detection range: patrol around the map
            Patrol();
        }
    }

    protected internal void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            // No patrol points assigned, stand still
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Get current patrol point
        Transform currentPatrolPoint = patrolPoints[currentPatrolIndex];

        // Calculate direction to patrol point
        Vector2 directionToPatrolPoint = ((Vector2)currentPatrolPoint.position - (Vector2)transform.position).normalized;

        // Move towards patrol point
        rb.linearVelocity = directionToPatrolPoint * monsterStats.MoveSpeed;

        // Rotate towards movement direction
        SmoothRotateTowardsMovement();

        // Check if patrol point is reached
        float distanceToPatrolPoint = Vector2.Distance(transform.position, currentPatrolPoint.position);
        if (distanceToPatrolPoint <= patrolPointReachedThreshold)
        {
            // Switch to the next patrol point
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }
    }

    /// <summary>
    /// Call this when this monster detects the player. It will alert nearby monsters to also move towards the player.
    /// </summary>
    protected internal void AlertNearbyMonsters()
    {
        if (hasBeenAlertedThisFrame) return; // Prevent infinite recursion
        hasBeenAlertedThisFrame = true;

        Collider2D[] nearbyMonsters = Physics2D.OverlapCircleAll(transform.position, alertRadius);
        foreach (var col in nearbyMonsters)
        {
            if (col.CompareTag("Monster") && col.gameObject != this.gameObject)
            {
                Monster otherMonster = col.GetComponent<Monster>();
                if (otherMonster != null)
                {
                    float distance = Vector2.Distance(transform.position, otherMonster.transform.position);
                    float combinedDetectionRange = monsterStats.DetectionRange + otherMonster.monsterStats.DetectionRange;
                    if (distance <= combinedDetectionRange)
                    {
                        // Alert the other monster and set its target to the player
                        otherMonster.currentTarget = player.position;
                        otherMonster.isAttacking = false; // Ensure it starts moving
                        AlertNearbyMonsters(); // Propagate alert
                    }
                }
            }
        }
        // Set self to move towards the player
        currentTarget = player.position;
        isAttacking = false;
        DetectingThePlayerPosition();
    }

    protected internal void CheckAttackRange()
    {
        Transform currentPlayer = player; // Cache the player reference

        if (currentPlayer == null) // Add null check here
        {
            return; // Exit the method if the player is null
        }
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float effectiveAttackRange = monsterStats.AttackRange;

        // For Melee monsters, use a fixed close range (e.g., 1.2f units)
        if (monsterStats.monsterType == MonsterStats.MonsterType.Melee)
        {
            effectiveAttackRange = 1.2f;
        }

        // For ranged/magic monsters, handle shooting logic
        if (monsterStats.monsterType == MonsterStats.MonsterType.Ranged ||
            monsterStats.monsterType == MonsterStats.MonsterType.Magic)
        {
            if (distanceToPlayer <= effectiveAttackRange)
            {
                // TryShootAtPlayer will handle shooting and cooldown
                isAttacking = true;
            }
            else if (distanceToPlayer > effectiveAttackRange + 0.5f)
            {
                // Move towards player if out of range by a margin
                isAttacking = false;
            }
            else
            {
                // Stay idle if just outside range
                isAttacking = false;
            }
        }
        else // Melee monsters
        {
            if (distanceToPlayer <= effectiveAttackRange)
            {
                if (Time.time - lastAttackTime >= monsterStats.AttackCooldown)
                {
                    AttackPlayer();
                    playerFunctionalities.MonsterMeleeAttack(monsterStats.AttackDamage); // Call the PlayerFunctionalities to deal damage
                    lastAttackTime = Time.time;
                }
                isAttacking = true;
            }
            else if (distanceToPlayer > effectiveAttackRange + 0.5f)
            {
                // Move towards player if out of range by a margin
                isAttacking = false;
            }
            else
            {
                // Stay idle if just outside range
                isAttacking = false;
            }
        }
    }

    protected internal void AttackPlayer()
    {
        PlayerFunctionalities playerHealth = player.GetComponent<PlayerFunctionalities>();
        if (playerHealth != null)
        {
            GameManager.instance.DealDamage(monsterStats.AttackDamage);
            // Debug.Log($"Monster attacked player for {monsterStats.AttackDamage} damage! by {monsterStats.name}");
            // Optional: Add visual/audio feedback
            // PlayAttackAnimation();
            // SoundManager.PlaySound("MonsterAttack");
        }
    }

    protected internal void TryShootAtPlayer()
    {
        if (player == null) // Add null check here
            return; // Exit the method if the player is null
        // Only allow shooting for Ranged or Magic monsters
        if (monsterStats.monsterType != MonsterStats.MonsterType.Ranged &&
            monsterStats.monsterType != MonsterStats.MonsterType.Magic)
            return;

        if (monsterStats.projectilePrefab == null || firePoint == null) return;

        // Detect player within a circle range (detection radius)
        float detectionRadius = monsterStats.DetectionRange; // Use your desired detection radius
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Rotate towards the player
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, monsterStats.RotationSpeed * Time.deltaTime * 100f);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);

            // Only shoot if within attack range and facing the player
            float firePointToPlayer = Vector2.Distance(firePoint.position, player.position);
            if (firePointToPlayer <= monsterStats.AttackRange && Time.time - lastShotTime >= 1f / monsterStats.AttackCooldown)
            {
                float angleDiff = Mathf.DeltaAngle(transform.eulerAngles.z, targetAngle);
                float rotationThreshold = 5f; // degrees

                if (Mathf.Abs(angleDiff) <= rotationThreshold)
                {
                    // Raycast to check for direct line of sight
                    Vector2 origin = firePoint.position;
                    Vector2 toPlayer = ((Vector2)player.position - origin).normalized;
                    float maxDistance = Vector2.Distance(origin, player.position);

                    RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer, maxDistance, ~LayerMask.GetMask("Monster"));
                    Vector2 shootDirection;

                    if (hit.collider == null || hit.collider.transform == player)
                    {
                        shootDirection = toPlayer;
                    }
                    else
                    {
                        shootDirection = ((Vector2)hit.point - origin).normalized;
                    }

                    GameObject projectile = Instantiate(monsterStats.projectilePrefab, firePoint.position, Quaternion.identity);
                    if (projectile.TryGetComponent<Rigidbody2D>(out Rigidbody2D projRb))
                    {
                        float projectileSpeed = 10f; // Expose as needed
                        projRb.linearVelocity = shootDirection * projectileSpeed;
                        AttackPlayer();
                        UpdateMonsterHealth();
                    }
                    lastShotTime = Time.time;
                    // Optionally: Play shooting animation/sound here
                }
            }
        }
    }

    protected internal Vector2 CalculateAvoidanceForce()
    {
        Vector2 avoidanceForce = Vector2.zero;
        Collider2D[] nearbyMonsters = Physics2D.OverlapCircleAll(transform.position, monsterStats.MinMonsterDistance);
        int avoidCount = 0;

        foreach (Collider2D monster in nearbyMonsters)
        {
            if (monster.CompareTag("Monster") && monster.gameObject != this.gameObject)
            {
                Vector2 dirToMonster = transform.position - monster.transform.position;
                float distance = dirToMonster.magnitude;

                // The closer the monster, the stronger the avoidance
                if (distance < monsterStats.MinMonsterDistance)
                {
                    avoidCount++;
                    avoidanceForce += dirToMonster.normalized * (1f - distance / monsterStats.MinMonsterDistance);
                }
            }
        }

        if (avoidCount > 0)
        {
            avoidanceForce /= avoidCount;
            return avoidanceForce * monsterStats.AvoidanceForce;
        }

        return Vector2.zero;
    }

    protected internal void SmoothRotateTowardsMovement()
    {
        Vector2 direction = rb.linearVelocity;
        if (direction == Vector2.zero) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float newAngle = Mathf.LerpAngle(
            transform.eulerAngles.z,
            targetAngle,
            monsterStats.RotationSpeed * Time.fixedDeltaTime
        );
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    protected internal virtual void UpdateTarget()
    {
        // Stop updating if the player is dead or missing
        if (player == null || player.GetComponent<PlayerFunctionalities>()?.isDead == true)
            return;

        if (!isAvoiding && !isAttacking)
        {
            currentTarget = player.position;
        }
    }

    protected internal void Die()
    {
        if (currentHealth <= 0f)
        {
            Debug.Log("Monster defeated!");

            ExperienceManager.instance.AddExperience(monsterStats.ExperiencePoints); // Add experience to the player or monster

            Destroy(gameObject);
            drop.DropItem();
        }
    }
}