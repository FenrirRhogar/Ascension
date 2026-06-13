using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private int damage = 30;
    [SerializeField] private float speed = 40f; 
    [SerializeField] private float explosionRadius = 0f; 
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody rb;
    private HashSet<Health> affectedHealths = new HashSet<Health>();
    private bool exploded = false;
    private PlayerController shooter;
    private float spawnTime;

    public void SetShooter(PlayerController p) => shooter = p;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // Physics Hardening
        rb.useGravity = false;
        rb.isKinematic = false; 
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Collider Hardening
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.35f; // Slightly smaller collider for better clearance
            col = sc;
        }
        col.isTrigger = true;
    }

    void Start()
    {
        spawnTime = Time.time;

        // Default AOE for Fireballs
        if (gameObject.name.ToLower().Contains("fireball") && explosionRadius < 0.1f)
        {
            explosionRadius = 5f;
        }

        // Force Velocity only if not already set by the shooter script
        if (rb.linearVelocity.sqrMagnitude < 0.1f)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;

        // Grace period to prevent immediate self/ground collision (0.05s)
        if (Time.time < spawnTime + 0.05f) return;

        // Ignore self and other projectiles
        if (other.GetComponent<Projectile>() != null) return;
        if (shooter != null && (other.transform == shooter.transform || other.transform.IsChildOf(shooter.transform))) return;
        
        // If it's a trigger, only impact if it's a valid combat target (Enemy or Player)
        if (other.isTrigger && !other.CompareTag("Enemy") && !other.CompareTag("Player")) return;

        // JUICE: Shake shooter's camera
        if (shooter != null && JuiceManager.Instance != null)
        {
            float shakeMag = (explosionRadius > 0.1f) ? 0.35f : 0.12f;
            JuiceManager.Instance.ShakeCamera(shooter.playerCamera, 0.2f, shakeMag);
        }

        ExecuteImpact(other);
    }

    private void ExecuteImpact(Collider hitCollider)
    {
        exploded = true;
        affectedHealths.Clear();

        if (explosionRadius > 0.1f)
        {
            Explode();
        }
        else
        {
            ApplyDamage(hitCollider.gameObject);
        }

        if (hitEffectPrefab != null)
        {
            GameObject impact = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(impact, 3f); // Cleanup after 3 seconds
        }

        Destroy(gameObject);
    }

    private void Explode()
    {
        Collider[] victims = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var v in victims)
        {
            // Don't hit the shooter during an explosion either
            if (shooter != null && (v.transform == shooter.transform || v.transform.IsChildOf(shooter.transform))) continue;
            ApplyDamage(v.gameObject);
        }
    }

    private void ApplyDamage(GameObject target)
    {
        Health health = target.GetComponent<Health>();
        if (health == null) health = target.GetComponentInParent<Health>();
        if (health == null) health = target.GetComponentInChildren<Health>();

        if (health != null && !affectedHealths.Contains(health))
        {
            // PREVENTION: Prevent Friendly Fire (Players shouldn't hit players)
            if (shooter != null && health.isPlayer) return;

            int finalDmg = damage;
            
            // Player-specific logic
            if (shooter != null) 
            {
                finalDmg = Mathf.RoundToInt(damage * shooter.damageMultiplier);
                
                // Add Ultimate Charge to the shooter
                var rs = shooter.GetComponent<ResourceSystem>();
                if (rs != null) rs.AddUltimateCharge(5f); 
            }
            
            health.TakeDamage(finalDmg);
            affectedHealths.Add(health);
        }
    }
}
