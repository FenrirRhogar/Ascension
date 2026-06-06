using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFighter", menuName = "DungeonCrawler/Classes/Fighter")]
public class FighterClassSO : CharacterClassSO
{
    [Header("Fighter Stats")]
    public float attackRange = 2f;
    public float cleaveRange = 4f;
    public int damage = 20;
    
    [Header("Whirlwind (Ability)")]
    public float spinSpeed = 1500f;
    public float whirlwindRadius = 5f;
    public int whirlwindDamage = 15;

    private Dictionary<PlayerController, GameObject> activeWhirlwinds = new Dictionary<PlayerController, GameObject>();
    private Dictionary<PlayerController, float> nextDamageTime = new Dictionary<PlayerController, float>();

    public override void ExecuteAttack(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Attack");
        
        // Use camera aim for visual alignment (vertical pivot)
        Vector3 aimDir = player.transform.forward;
        if (player.playerCamera != null) aimDir = player.playerCamera.transform.forward;

        if (attackVFX != null)
        {
            Vector3 spawnPos = player.transform.position + Vector3.up * vfxHeightOffset;
            Quaternion rotation = Quaternion.LookRotation(aimDir);

            GameObject vfx = Instantiate(attackVFX, spawnPos, rotation);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 2f);
        }
        
        PerformMelee(player, attackRange + player.meleeRangeBonus, damage, aimDir);
    }

    public override void ExecuteAbility(PlayerController player, Animator animator)
    {
        // Start Channeled Whirlwind (Beyblade)
        if (animator != null && HasParameter(animator, "IsCasting")) 
            animator.SetBool("IsCasting", true); 
            
        if (abilityVFX != null)
        {
            GameObject vfx = Instantiate(abilityVFX, player.transform.position + Vector3.up * vfxHeightOffset, Quaternion.identity);
            vfx.transform.SetParent(player.transform);
            MaterialFixer.Fix(vfx);
            
            activeWhirlwinds[player] = vfx;
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

        // 2. Spin the Player Model!
        player.externalModelRotationY -= spinSpeed * Time.deltaTime;

        // 3. AoE Damage Logic
        float lastDamageTime = 0f;
        nextDamageTime.TryGetValue(player, out lastDamageTime);

        if (Time.time >= lastDamageTime)
        {
            nextDamageTime[player] = Time.time + 0.25f; // 4 ticks per second

            Collider[] hits = Physics.OverlapSphere(player.transform.position + Vector3.up * vfxHeightOffset, whirlwindRadius);

            foreach (var hit in hits)
            {
                // Ignore self
                if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) continue;
                
                Health h = hit.GetComponent<Health>();
                if (h == null) h = hit.GetComponentInParent<Health>();
                if (h != null && !h.isPlayer)
                {
                    h.TakeDamage(Mathf.RoundToInt(whirlwindDamage * player.damageMultiplier));
                }
            }
        }
    }

    public override void OnAbilityReleased(PlayerController player, Animator animator)
    {
        if (animator != null && HasParameter(animator, "IsCasting")) 
            animator.SetBool("IsCasting", false);
        
        if (activeWhirlwinds.TryGetValue(player, out GameObject vfx))
        {
            if (vfx != null) Destroy(vfx);
            activeWhirlwinds.Remove(player);
        }
        nextDamageTime.Remove(player);
        
        // Reset the spin when they stop holding the button
        player.externalModelRotationY = 0f;
    }

    public override void ExecuteUltimate(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ultimate");
        
        if (ultimateVFX != null)
        {
            GameObject vfx = Instantiate(ultimateVFX, player.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            vfx.transform.SetParent(player.transform); // Follow the player
            Destroy(vfx, 6f);
        }

        Debug.Log("[Fighter] IRON WHIRLWIND!");
        Collider[] victims = Physics.OverlapSphere(player.transform.position, 8f);
        foreach (var v in victims)
        {
            if (v.CompareTag("Player")) continue;
            Health h = v.GetComponent<Health>();
            if (h == null) h = v.GetComponentInParent<Health>();
             if (h != null) h.TakeDamage(Mathf.RoundToInt(damage * 3 * player.damageMultiplier));
        }
    }

    private void PerformMelee(PlayerController player, float range, int dmg, Vector3 dir)
    {
        // Origin at the custom offset height
        Vector3 origin = player.transform.position + Vector3.up * vfxHeightOffset;
        Vector3 strikeCenter = origin + (dir * (range * 0.5f)); 

        Collider[] hits = Physics.OverlapSphere(strikeCenter, range);
        foreach (var hit in hits)
        {
            if (hit.transform == player.transform) continue;
            
            Health h = hit.GetComponent<Health>();
            if (h == null) h = hit.GetComponentInParent<Health>();
            if (h == null) h = hit.GetComponentInChildren<Health>();

             if (h != null && !h.isPlayer)
             {
                 h.TakeDamage(Mathf.RoundToInt(dmg * player.damageMultiplier));
                var rs = player.GetComponent<ResourceSystem>();
                if (rs != null) rs.AddUltimateCharge(10f);

                if (JuiceManager.Instance != null)
                {
                    JuiceManager.Instance.ShakeCamera(player.playerCamera, 0.2f, range > 3f ? 0.4f : 0.15f);
                    if (range > 3f) JuiceManager.Instance.HitStop(0.05f);
                }
            }
        }
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
