using UnityEngine;
using UnityEngine.AI;

public class RangedEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState = State.Idle;

    private NavMeshAgent agent;
    private Transform targetPlayer; 
    private Animator animator;

    [Header("AI Parameters")]
    public float detectionRadius = 20f;
    public float shootingRadius = 12f;
    public float tooCloseRadius = 5f; // Distance at which the enemy tries to back away
    public int attackDamage = 15;
    public float fireRate = 2.0f;
    
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform launchPoint;

    private float stateTimer = 0f;
    private float lastFireTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (agent != null && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.enabled = false;
                agent.enabled = true;
            }
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                if (animator != null) animator.SetBool("IsMoving", false);
                stateTimer += Time.deltaTime;
                if (stateTimer > 2f) 
                {
                    stateTimer = 0f;
                    currentState = State.Patrol;
                }
                CheckForPlayer();
                break;

            case State.Patrol:
                if (animator != null) animator.SetBool("IsMoving", true);
                if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    Vector3 randomDir = Random.insideUnitSphere * 10f;
                    randomDir += transform.position;
                    NavMeshHit hit;
                    if(NavMesh.SamplePosition(randomDir, out hit, 10f, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                CheckForPlayer();
                break;

            case State.Chase:
                if (targetPlayer != null)
                {
                    float dist = Vector3.Distance(transform.position, targetPlayer.position);
                    
                    if (dist <= shootingRadius)
                    {
                        currentState = State.Attack;
                        return;
                    }

                    if (animator != null) animator.SetBool("IsMoving", true);
                    agent.SetDestination(targetPlayer.position);

                    if (dist > detectionRadius + 10f)
                    {
                        targetPlayer = null;
                        currentState = State.Patrol;
                    }
                }
                break;

            case State.Attack:
                if (targetPlayer == null) { currentState = State.Idle; return; }

                float currentDist = Vector3.Distance(transform.position, targetPlayer.position);
                
                // 1. Look at Player
                transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));

                // 2. Kiting Logic (Maintain distance)
                if (currentDist < tooCloseRadius)
                {
                    // Too close! Back away
                    Vector3 backDir = (transform.position - targetPlayer.position).normalized;
                    Vector3 retreatPos = transform.position + backDir * 4f;
                    
                    if (animator != null) animator.SetBool("IsMoving", true);
                    agent.SetDestination(retreatPos);
                }
                else if (currentDist > shootingRadius)
                {
                    // Too far! Go back to chasing
                    currentState = State.Chase;
                }
                else
                {
                    // Perfect distance - Stop and shoot
                    if (agent.isOnNavMesh) agent.SetDestination(transform.position);
                    if (animator != null) animator.SetBool("IsMoving", false);

                    // 3. Shooting Logic
                    if (Time.time >= lastFireTime + fireRate)
                    {
                        Shoot();
                        lastFireTime = Time.time;
                    }
                }
                break;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null) return;
        if (animator != null) animator.SetTrigger("Attack");

        Vector3 spawnPos = launchPoint != null ? launchPoint.position : transform.position + transform.forward + Vector3.up * 1.5f;
        Vector3 shootDir = (targetPlayer.position + Vector3.up - spawnPos).normalized;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(shootDir));
        
        var projScript = proj.GetComponent<Projectile>();
        if (projScript == null) projScript = proj.AddComponent<Projectile>();
        
        // Setup projectile stats
        // We set shooter to null so it doesn't try to add ultimate charge to an enemy
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = shootDir * 15f; // Slower than player fireball
        }
    }

    private void CheckForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                targetPlayer = hit.transform;
                currentState = State.Chase;
                break;
            }
        }
    }
}
