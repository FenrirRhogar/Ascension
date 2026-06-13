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
    public float detectionRadius = 20f;
    public float attackRadius = 2.5f;
    public int attackDamage = 25;
    
    private float stateTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Snapping logic for stability
        if (agent != null && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

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
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    Vector3 randomDir = Random.insideUnitSphere * 10f;
                    randomDir += transform.position;
                    NavMeshHit navHit;
                    if(NavMesh.SamplePosition(randomDir, out navHit, 10f, 1))
                    {
                        agent.SetDestination(navHit.position);
                    }
                }
                CheckForPlayer();
                break;

            case State.Chase:
                if (targetPlayer != null)
                {
                    if (animator != null) animator.SetBool("IsMoving", true);
                    agent.SetDestination(targetPlayer.position);
                    
                    float dist = Vector3.Distance(transform.position, targetPlayer.position);
                    if (dist <= attackRadius)
                    {
                        currentState = State.Attack;
                        stateTimer = 0f;
                    }
                    else if (dist > detectionRadius + 5f)
                    {
                        targetPlayer = null;
                        currentState = State.Patrol;
                    }
                }
                break;

            case State.Attack:
                agent.SetDestination(transform.position); 
                if (animator != null) animator.SetBool("IsMoving", false);
                
                if (targetPlayer != null)
                {
                    transform.LookAt(new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z));
                    
                    stateTimer += Time.deltaTime;
                    if (stateTimer > 1.2f) 
                    {
                        if (animator != null) animator.SetTrigger("Attack");
                        var health = targetPlayer.GetComponent<Health>();
                        if (health != null) health.TakeDamage(attackDamage);
                        stateTimer = 0f;
                    }

                    if (Vector3.Distance(transform.position, targetPlayer.position) > attackRadius)
                        currentState = State.Chase;
                }
                else currentState = State.Idle;
                break;
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
