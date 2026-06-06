using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWizard", menuName = "DungeonCrawler/Classes/Wizard")]
public class WizardClassSO : CharacterClassSO
{
    [Header("Wizard Specific Prefabs")]
    public GameObject fireballPrefab;
    public GameObject meteorRainPrefab;
    public GameObject meteorImpactPrefab;

    private Dictionary<PlayerController, GameObject> activeLightnings = new Dictionary<PlayerController, GameObject>();
    private Dictionary<PlayerController, float> nextDamageTime = new Dictionary<PlayerController, float>();

    public override void ExecuteAttack(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Attack");

        // Wizard basic attack is Fireball
        SpawnProjectile(fireballPrefab, player, 30f);
    }

    private Vector3 GetMagicSpawnPoint(PlayerController player, out Vector3 forwardDir)
    {
        forwardDir = player.transform.forward;
        if (player.playerCamera != null) forwardDir = player.playerCamera.transform.forward;

        if (player.activeWeapon != null)
        {
            Transform tip = player.activeWeapon.transform.Find("Tip");
            if (tip == null) tip = player.activeWeapon.transform.Find("Muzzle");
            if (tip == null) tip = player.activeWeapon.transform.Find("SpawnPoint");

            if (tip != null) return tip.position;

            // Fallback to the weapon's location (usually the hand)
            return player.activeWeapon.transform.position + (Vector3.up * 0.2f);
        }

        // Default fallback if no weapon is equipped
        return player.transform.position + (Vector3.up * vfxHeightOffset) + (forwardDir * 1.2f);
    }

    public override void ExecuteAbility(PlayerController player, Animator animator)
    {
        // Start Channeled Lightning
        if (animator != null && HasParameter(animator, "IsCasting")) 
            animator.SetBool("IsCasting", true); 

        // Use abilityVFX slot for the Lightning Cone (VFX 01)
        if (abilityVFX != null)
        {
            Debug.Log($"[Wizard] Spawning Cone VFX: {abilityVFX.name}");

            Vector3 spawnPos = GetMagicSpawnPoint(player, out Vector3 forwardDir);

            // Instantiate WITHOUT parent for custom transform control
            GameObject vfx = Instantiate(abilityVFX, spawnPos, Quaternion.LookRotation(forwardDir));
            
            // CRITICAL: Fix materials so they aren't pink/orange
            MaterialFixer.Fix(vfx);
            
            activeLightnings[player] = vfx;
            nextDamageTime[player] = 0f;
        }
    }

    public override void OnAbilityHold(PlayerController player, Animator animator)
    {
        var rs = player.GetComponent<ResourceSystem>();
        var combat = player.GetComponent<CombatSystem>();
        if (rs == null || combat == null) return;

        // 1. Continuous Mana Drain
        if (!rs.UseMana(manaDrainPerSecond * Time.deltaTime))
        {
            combat.ForceStopAbility();
            return;
        }

        // 2. Align the Cone with the player's aim
        Vector3 origin = GetMagicSpawnPoint(player, out Vector3 forwardDir);

        if (activeLightnings.TryGetValue(player, out GameObject vfx) && vfx != null)
        {
            // Position exactly at the wand tip/spawn point
            vfx.transform.position = origin;
            vfx.transform.forward = forwardDir;
        }

        // 3. Cone Damage logic (Area of Effect)
        float lastDamageTime = 0f;
        nextDamageTime.TryGetValue(player, out lastDamageTime);

        if (Time.time >= lastDamageTime)
        {
            nextDamageTime[player] = Time.time + 0.2f; // 5 ticks per second for area damage

            // Define the detection zone (15m sphere)
            Collider[] hits = Physics.OverlapSphere(origin + (forwardDir * 1f), 15f);

            foreach (var hit in hits)
            {
                // Ignore self and friendly players
                if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) continue;

                Vector3 toTarget = (hit.transform.position - origin).normalized;
                
                // Cone check: Dot product > 0.45 means enemies within ~63 degrees are hit
                if (Vector3.Dot(forwardDir, toTarget) > 0.45f)
                {
                    Health h = hit.GetComponent<Health>();
                    if (h == null) h = hit.GetComponentInParent<Health>();
                    if (h != null && !h.isPlayer)
                    {
                        h.TakeDamage(Mathf.RoundToInt(12 * player.damageMultiplier));
                    }
                }
            }
        }
    }

    public override void OnAbilityReleased(PlayerController player, Animator animator)
    {
        if (animator != null && HasParameter(animator, "IsCasting")) 
            animator.SetBool("IsCasting", false);
        
        if (activeLightnings.TryGetValue(player, out GameObject vfx))
        {
            if (vfx != null) Destroy(vfx);
            activeLightnings.Remove(player);
        }
        nextDamageTime.Remove(player);
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    public override void ExecuteUltimate(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ultimate");

        Vector3 targetPoint = player.GetAimPoint();

        if (ultimateVFX != null)
        {
            GameObject vfx = Instantiate(ultimateVFX, targetPoint + Vector3.up * 0.1f, Quaternion.identity);
            Destroy(vfx, 10f); // Clean up the big storm cloud after 10s
        }
        
        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.HitStop(0.1f);
            JuiceManager.Instance.ShakeCamera(player.playerCamera, 0.6f, 0.8f);
        }

        for (int i = 0; i < 10; i++) 
        {
            Vector3 offset = new Vector3(Random.Range(-6f, 6f), 0, Random.Range(-6f, 6f));
            Vector3 finalPoint = targetPoint + offset;
            
            if (meteorImpactPrefab != null)
            {
                GameObject impact = Instantiate(meteorImpactPrefab, finalPoint + Vector3.up * 0.1f, Quaternion.identity);
                Destroy(impact, 3f); // Clean up each explosion
                
                Collider[] victims = Physics.OverlapSphere(finalPoint, 4f);
                foreach (var v in victims)
                {
                    Health h = v.GetComponent<Health>();
                    if (h != null && !h.isPlayer) h.TakeDamage(Mathf.RoundToInt(40 * player.damageMultiplier));
                }
            }
        }
    }

    private void SpawnProjectile(GameObject prefab, PlayerController player, float speed)
    {
        if (prefab == null) return;
        
        Vector3 spawnPos = GetMagicSpawnPoint(player, out Vector3 forwardDir);
        Vector3 aimPoint = player.GetAimPoint();
        Vector3 fireDir = (aimPoint - spawnPos).normalized;

        GameObject proj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(fireDir));
        
        var projScript = proj.GetComponent<Projectile>();
        if (projScript == null) projScript = proj.AddComponent<Projectile>();
        projScript.SetShooter(player);

        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = fireDir * speed;
        }
    }
}
