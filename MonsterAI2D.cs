using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MonsterAI2D : MonoBehaviour
{
    public Transform player;
    public Transform firePoint;         // Assign a child transform as the fire point
    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private float lastPathUpdateTime;
    private bool isAvoiding = false;
    private Vector2 currentTarget;
    private float lastAttackTime;
    private bool isAttacking = false;
    [HideInInspector]
    public float currentHealth; // Tracks the current health of the monster
    private float lastShotTime = 0f;
    public MonsterStats monsterStats; // Reference to the MonsterStats scriptable object
    PlayerFunctionalities playerFunctionalities; // Reference to the PlayerFunctionalities script
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerFunctionalities = FindFirstObjectByType<PlayerFunctionalities>().gameObject.GetComponent<PlayerFunctionalities>();
        currentTarget = player.position;
        lastPathUpdateTime = Time.time;
        currentHealth = monsterStats.Health; // Initialize current health to max health
    }

    void Update()
    {
        if (Time.time - lastPathUpdateTime > monsterStats.PathUpdateInterval)
        {
            UpdateTarget();
            lastPathUpdateTime = Time.time;
        }
        CheckAttackRange();

        TryShootAtPlayer();
    }

    void FixedUpdate()
    {
        if (!isAttacking)
        {
            DetectingThePlayerPosition();
        }
        else
        {
            // Move randomly when attacking
            if (rb.linearVelocity == Vector2.zero || Random.value < 0.02f)
            {
                // Pick a random direction
                float angle = Random.Range(0f, 2f * Mathf.PI);
                Vector2 randomDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                rb.linearVelocity = randomDir * monsterStats.MoveSpeed * 0.5f; // Move at half speed while attacking
            }
        }
    }

    void DetectingThePlayerPosition()
    {
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
        }
        else
        {
            // Player is outside detection range: stop moving, but look at player
            rb.linearVelocity = Vector2.zero;

            if (firePoint != null)
            {
                Vector2 lookDir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // Optionally, rotate towards player even without firePoint
                Vector2 lookDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                if (lookDir != Vector2.zero)
                {
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
    }

    void CheckAttackRange()
    {
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

    void AttackPlayer()
    {
        PlayerFunctionalities playerHealth = player.GetComponent<PlayerFunctionalities>();
        if (playerHealth != null)
        {
            GameManager.instance.DealDamage(monsterStats.AttackDamage);
            Debug.Log($"Monster attacked player for {monsterStats.AttackDamage} damage! by {monsterStats.name}");
            // Optional: Add visual/audio feedback
            // PlayAttackAnimation();
            // SoundManager.PlaySound("MonsterAttack");
        }
    }

    void TryShootAtPlayer()
    {
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

    Vector2 CalculateAvoidanceForce()
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

    void SmoothRotateTowardsMovement()
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

    void UpdateTarget()
    {
        if (!isAvoiding && !isAttacking)
        {
            currentTarget = player.position;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Bullet bullet = collision.gameObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            GameManager.instance.DealDamage(bullet.GetDamage());
            currentHealth -= bullet.GetDamage(); // Reduce monster health by bullet damage
            if (currentHealth <= 0)
                Die(); // Call the Die
            Debug.Log($"Monster hit by bullet! Current health: {currentHealth}/{monsterStats.Health}");
            Destroy(bullet.gameObject);
        }
    }

    // Example drop chances (adjust as needed)
    private static readonly Dictionary<Items.Rarity, float> rarityDropChances = new Dictionary<Items.Rarity, float>
    {
        { Items.Rarity.Common, 0.6f },      // 60%
        { Items.Rarity.Uncommon, 0.2f },    // 20%
        { Items.Rarity.Rare, 0.12f },       // 12%
        { Items.Rarity.Epic, 0.06f },       // 6%
        { Items.Rarity.Legendary, 0.02f }   // 2%
    };

    public List<Items> possibleDrops; // Assign in Inspector

    private List<Items> GetRandomDropByRarity(int maxItemsToDrop)
    {
        List<Items> drops = new List<Items>();
        if (possibleDrops == null || possibleDrops.Count == 0 || maxItemsToDrop <= 0)
            return drops;

        // Group items by rarity
        var grouped = new Dictionary<Items.Rarity, List<Items>>();
        foreach (var item in possibleDrops)
        {
            if (!grouped.ContainsKey(item.rarity))
                grouped[item.rarity] = new List<Items>();
            grouped[item.rarity].Add(item);
        }

        int attempts = 0;
        int maxAttempts = maxItemsToDrop * 5; // Prevent infinite loop

        while (drops.Count < maxItemsToDrop && attempts < maxAttempts)
        {
            attempts++;

            // Randomly pick a rarity based on drop chances
            float rand = Random.value;
            float cumulative = 0f;
            Items.Rarity? selectedRarity = null;
            foreach (var kvp in rarityDropChances.OrderBy(r => (int)r.Key)) // Common first
            {
                cumulative += kvp.Value;
                if (rand <= cumulative)
                {
                    selectedRarity = kvp.Key;
                    break;
                }
            }

            // If we have items of that rarity, pick one
            if (selectedRarity.HasValue && grouped.ContainsKey(selectedRarity.Value) && grouped[selectedRarity.Value].Count > 0)
            {
                var itemsOfRarity = grouped[selectedRarity.Value];
                Items selected = itemsOfRarity[Random.Range(0, itemsOfRarity.Count)];
                if (!drops.Contains(selected))
                    drops.Add(selected);
            }
            // If no item of that rarity, skip this attempt
        }
        return drops;
    }

    void Die()
    {
        InventoryBag bag = FindObjectOfType<InventoryBag>();
        int maxItemsToDrop = 3; // Set your desired max here or expose as a public variable

        if (currentHealth <= 0f)
        {
            Debug.Log("Monster defeated!");
            Destroy(gameObject);
            ExperienceManager.instance.AddExperience(monsterStats.ExperiencePoints); // Add experience to the player or monster
            if (bag != null && possibleDrops != null && possibleDrops.Count > 0)
            {
                var dropItems = GetRandomDropByRarity(maxItemsToDrop);
                foreach (var dropItem in dropItems)
                {
                    int dropAmount = 1; // Or randomize as needed
                    bool added = bag.AddItem(dropItem, dropAmount);
                    if (added)
                        Debug.Log($"Dropped {dropAmount}x {dropItem.itemName} (Rarity: {dropItem.rarity}) and added to bag.");
                    else
                        Debug.Log("Bag is full, could not add dropped item.");
                }
            }
        }
    }

    public void UpdateMonsterHealth()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, monsterStats.Health);
        Debug.Log($"Monster health updated: {currentHealth}/{monsterStats.Health}");
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, monsterStats.DetectionRange);

        // Draw minimum monster distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, monsterStats.MinMonsterDistance);

        // Draw attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, monsterStats.AttackRange);

        // Draw current target
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentTarget, 0.2f);
    }
}
