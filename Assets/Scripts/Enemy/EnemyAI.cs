using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState = State.Idle;

    private NavMeshAgent agent;
    private Transform targetPlayer; 
    private Animator animator;

    [Header("AI Parameters")]
    public float detectionRadius = 15f;
    public float attackRadius = 2.5f;
    public int attackDamage = 25;
    
    private float stateTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed *= 1.2f; // Make them faster
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Ensure the agent is snapped to the NavMesh at start
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
                    // Generate a random Cartesian coordinate for the patrol route
                    Vector3 randomDir = Random.insideUnitSphere * 8f;
                    randomDir += transform.position;
                    
                    NavMeshHit hit;
                    if(NavMesh.SamplePosition(randomDir, out hit, 8f, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                CheckForPlayer();
                break;

            case State.Chase:
                if (targetPlayer != null)
                {
                    if (animator != null) animator.SetBool("IsMoving", true);
                    agent.SetDestination(targetPlayer.position);
                    
                    if (Vector3.Distance(transform.position, targetPlayer.position) <= attackRadius)
                    {
                        currentState = State.Attack;
                    }
                    else if (Vector3.Distance(transform.position, targetPlayer.position) > detectionRadius + 5f)
                    {
                        targetPlayer = null;
                        currentState = State.Patrol;
                    }
                }
                else
                {
                    currentState = State.Patrol;
                }
                break;

            case State.Attack:
                if (agent.isOnNavMesh) agent.SetDestination(transform.position); // Halt agent translation
                if (animator != null) animator.SetBool("IsMoving", false);
                
                if (targetPlayer != null)
                {
                    transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                    
                    stateTimer += Time.deltaTime;
                    if (stateTimer > 1.0f) // Faster strikes (was 1.5f)
                    {
                        if (animator != null) animator.SetTrigger("Attack");
                        
                        var playerHealth = targetPlayer.GetComponent<Health>();
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(attackDamage);
                        }
                        stateTimer = 0f;
                    }

                    if (Vector3.Distance(transform.position, targetPlayer.position) > attackRadius)
                    {
                        currentState = State.Chase;
                    }
                }
                else
                {
                    currentState = State.Idle;
                }
                break;
        }
    }

    private void CheckForPlayer()
    {
        // Query the physics engine for all potential targets
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    targetPlayer = hit.transform;
                    currentState = State.Chase;
                }
            }
        }
    }
}
