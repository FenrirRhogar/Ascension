using UnityEngine;

[CreateAssetMenu(fileName = "NewCleric", menuName = "DungeonCrawler/Classes/Cleric")]
public class ClericClassSO : CharacterClassSO
{
    [Header("Cleric Stats")]
    public int healAmount = 25;
    public float healRadius = 6f;
    public int damage = 15;
    public float attackRange = 2f;

    public override void ExecuteAttack(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Attack");

        // Use camera aim for vertical pivot
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
        
        // Melee logic using eye-level height and camera aim
        float finalRange = attackRange + player.meleeRangeBonus;
        Vector3 origin = player.transform.position + Vector3.up * vfxHeightOffset;
        Vector3 strikeCenter = origin + (aimDir * (finalRange * 0.5f));

        Collider[] hits = Physics.OverlapSphere(strikeCenter, finalRange);
        foreach (var hit in hits)
        {
            if (hit.transform == player.transform) continue;
            
            Health h = hit.GetComponent<Health>();
            if (h == null) h = hit.GetComponentInParent<Health>();
            if (h == null) h = hit.GetComponentInChildren<Health>();

            if (h != null && !h.isPlayer)
            {
                h.TakeDamage(Mathf.RoundToInt(damage * player.damageMultiplier));
                var rs = player.GetComponent<ResourceSystem>();
                if (rs != null) rs.AddUltimateCharge(12f);
            }
        }
    }

    public override void ExecuteAbility(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ability");

        if (abilityVFX != null)
        {
            GameObject vfx = Instantiate(abilityVFX, player.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 4f);
        }

        Collider[] nearbyPlayers = Physics.OverlapSphere(player.transform.position, healRadius);
        foreach (var col in nearbyPlayers)
        {
            if (col.CompareTag("Player"))
            {
                var h = col.GetComponent<Health>();
                if (h != null) h.Heal(healAmount);
            }
        }
    }

    public override void ExecuteUltimate(PlayerController player, Animator animator)
    {
        if (animator != null) animator.SetTrigger("Ultimate");

        if (ultimateVFX != null)
        {
            GameObject vfx = Instantiate(ultimateVFX, player.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            vfx.transform.SetParent(player.transform);
            Destroy(vfx, 10f);
        }
        
        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.HitStop(0.1f);
            JuiceManager.Instance.ShakeCamera(player.playerCamera, 0.4f, 0.5f);
        }

        Debug.Log("[Cleric] HOLY SANCTUARY!");
        Collider[] victims = Physics.OverlapSphere(player.transform.position, 12f);
        foreach (var v in victims)
        {
            Health h = v.GetComponent<Health>();
            if (h == null) h = v.GetComponentInParent<Health>();
            if (h != null)
            {
                if (h.isPlayer) h.Heal(100);
                else h.TakeDamage(Mathf.RoundToInt(damage * 4 * player.damageMultiplier));
            }
        }
    }
}
