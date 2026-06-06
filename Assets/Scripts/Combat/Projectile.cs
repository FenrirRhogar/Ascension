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

    public void SetShooter(PlayerController p) => shooter = p;

    void Start()
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
            sc.radius = 0.5f;
            col = sc;
        }
        col.isTrigger = true;

        // Default AOE for Fireballs
        if (gameObject.name.ToLower().Contains("fireball") && explosionRadius < 0.1f)
        {
            explosionRadius = 5f;
        }

        // Force Velocity
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;

        if (other.CompareTag("Player") || other.GetComponent<Projectile>() != null) return;
        if (other.isTrigger && !other.CompareTag("Enemy")) return;

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
            if (health.isPlayer) return;

            int finalDmg = damage;
            if (shooter != null) finalDmg = Mathf.RoundToInt(damage * shooter.damageMultiplier);
            health.TakeDamage(finalDmg);
            affectedHealths.Add(health);

            // Add Ultimate Charge to the shooter
            if (shooter != null)
            {
                var rs = shooter.GetComponent<ResourceSystem>();
                if (rs != null) rs.AddUltimateCharge(5f); 
            }
        }
    }
}
