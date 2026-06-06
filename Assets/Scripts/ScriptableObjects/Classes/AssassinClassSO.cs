using UnityEngine;

[CreateAssetMenu(fileName = "NewAssassin", menuName = "DungeonCrawler/Classes/Assassin")]
public class AssassinClassSO : CharacterClassSO
{
    [Header("Assassin Stats")]
    public float dashDistance = 8f;
    public float dashDuration = 0.2f;
    public float attackRange = 1.5f;
    public int damage = 25;

    public override void ExecuteAttack(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Attack");

        if (attackVFX != null)
        {
            // Use camera aim for visual alignment
            Vector3 aimDir = player.transform.forward;
            if (player.playerCamera != null) aimDir = player.playerCamera.transform.forward;

            // Spawn directly at the center using the offset
            Vector3 spawnPos = player.transform.position + Vector3.up * vfxHeightOffset;
            Quaternion rotation = Quaternion.LookRotation(aimDir);

            GameObject vfx = Instantiate(attackVFX, spawnPos, rotation);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 2f); // Cleanup after 2 seconds
        }

        PerformMelee(player, attackRange + player.meleeRangeBonus, damage);
    }

    public override void ExecuteAbility(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ability");

        if (abilityVFX != null)
        {
            GameObject vfx = Instantiate(abilityVFX, player.transform.position + Vector3.up * 0.1f, player.transform.rotation);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 3f); // Cleanup dash smoke
        }

        player.PerformDash(dashDistance, dashDuration);
    }

    public override void ExecuteUltimate(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ultimate");

        if (ultimateVFX != null)
        {
            GameObject vfx = Instantiate(ultimateVFX, player.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 5f); // Ultimate usually lasts longer
        }
        
        Debug.Log("[Assassin] GHOST WALK!");
        Collider[] victims = Physics.OverlapSphere(player.transform.position, 6f);
        foreach (var v in victims)
        {
            if (v.CompareTag("Player")) continue;
            Health h = v.GetComponent<Health>();
            if (h == null) h = v.GetComponentInParent<Health>();
            if (h != null) h.TakeDamage(Mathf.RoundToInt(damage * 5 * player.damageMultiplier));
        }
    }

    private void PerformMelee(PlayerController player, float range, int dmg)
    {
        // Use the camera's forward direction to allow pivoting up/down with the crosshair
        Vector3 aimDir = player.transform.forward;
        if (player.playerCamera != null) aimDir = player.playerCamera.transform.forward;

        // Origin at the same height as the VFX
        Vector3 origin = player.transform.position + Vector3.up * vfxHeightOffset; 
        Vector3 strikeCenter = origin + (aimDir * 1.0f); // Reach out 1 meter along the aim line

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
                if (rs != null) rs.AddUltimateCharge(8f);

                if (JuiceManager.Instance != null)
                {
                    JuiceManager.Instance.ShakeCamera(player.playerCamera, 0.15f, 0.1f);
                }
            }
        }
    }
}
