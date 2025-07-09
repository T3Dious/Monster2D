using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace MonsterAI2D
{
    public class Monster : MonoBehaviour
    {
        protected internal Transform player;
        [SerializeField]
        protected internal Transform firePoint;         // Assign a child transform as the fire point
        [SerializeField]
        protected internal float alertRadius = 8f; // Set in Inspector for how far the alert spreads
        protected internal bool hasBeenAlertedThisFrame = false;
        protected internal Rigidbody2D rb;
        protected internal Vector2 movementDirection;
        protected internal float lastPathUpdateTime;
        protected internal bool isAvoiding = false;
        protected internal Vector2 currentTarget;
        protected internal float lastAttackTime;
        protected internal bool isAttacking = false;
        protected internal float currentHealth; // Tracks the current health of the monster
        protected internal float lastShotTime = 0f;
        [SerializeField]
        protected internal MonsterStats monsterStats; // Reference to the MonsterStats scriptable object
        protected internal PlayerFunctionalities playerFunctionalities; // Reference to the PlayerFunctionalities script
        MonsterSkills skills; // Reference to the MonsterSkills script
        MonsterBehavoiur monsterBehavoiur; // Reference to the MonsterBehavoiur script
        protected internal Drop drop; // Reference to the Drop script for handling loot drops


        void Start()
        {
            rb = GetComponent<Rigidbody2D>();

            // Find the player using GameObject.FindGameObjectWithTag()
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerFunctionalities = playerObject.GetComponent<PlayerFunctionalities>();
            }
            else
            {
                Debug.LogError("No GameObject with tag 'Player' found in the scene!");
            }

            // Find the player using FindFirstObjectByType<PlayerFunctionalities>()
            playerFunctionalities = FindFirstObjectByType<PlayerFunctionalities>();
            if (playerFunctionalities == null)
            {
                Debug.LogError("No PlayerFunctionalities script found in the scene!");
            }
            else
            {
                player = playerFunctionalities.transform;
            }

            currentTarget = player.position;
            lastPathUpdateTime = Time.time;
            currentHealth = monsterStats.Health; // Initialize current health to max health
            skills = GetComponent<MonsterSkills>();
            monsterBehavoiur = GetComponent<MonsterBehavoiur>();
            drop = GetComponent<Drop>();
        }

        void Update()
        {
            // Stop all monster actions if the player is dead
            if (playerFunctionalities != null && playerFunctionalities.isDead)
                return;

            if (Time.time - lastPathUpdateTime > monsterStats.PathUpdateInterval)
            {
                if (monsterBehavoiur != null)
                    monsterBehavoiur.UpdateTarget();
                lastPathUpdateTime = Time.time;
            }
            if (monsterBehavoiur != null)
                monsterBehavoiur.CheckAttackRange();

            // Only allow dash for melee monsters
            if (skills != null && monsterStats.monsterType == MonsterStats.MonsterType.Melee && skills.ShouldDashAtPlayer())
            {
                skills.TryDashAtPlayer();
            }
            else if (skills != null && !skills.isDashing && monsterBehavoiur != null)
            {
                monsterBehavoiur.TryShootAtPlayer();
            }
        }

        void FixedUpdate()
        {
            if (!isAttacking)
            {
                monsterBehavoiur.DetectingThePlayerPosition();
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

        // Reset the flag at the end of each frame
        void LateUpdate()
        {
            hasBeenAlertedThisFrame = false;
        }


        void OnTriggerEnter2D(Collider2D collision)
        {
            Bullet bullet = collision.gameObject.GetComponent<Bullet>();
            if (bullet != null)
            {
                GameManager.instance.DealDamage(bullet.GetDamage());
                currentHealth -= bullet.GetDamage(); // Reduce monster health by bullet damage
                if (currentHealth <= 0)
                    monsterBehavoiur.Die(); // Call the Die
                // Debug.Log($"Monster hit by bullet! Current health: {currentHealth}/{monsterStats.Health}");
                Destroy(bullet.gameObject);
            }
        }

        public void UpdateMonsterHealth()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, monsterStats.Health);
            // Debug.Log($"Monster health updated: {currentHealth}/{monsterStats.Health}");
        }

        void OnDrawGizmos()
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

}
