using UnityEngine;

[CreateAssetMenu(fileName = "NewPotion", menuName = "DungeonCrawler/Potions/Potion")]
public class PotionSO : ConsumableSO
{
    [Header("Instant Restore Stats")]
    public int healthRestore = 0;
    public float manaRestore = 0f;
    public float ultimateCharge = 0f;

    [Header("Temporary Buff Stats")]
    public float speedMultiplier = 1f; // 1 means no boost, 1.5 means +50% speed
    public bool grantInfiniteStamina = false;

    [Header("Temporary Regeneration Buffs")]
    public float healthRegenBonus = 0f;  // HP regenerated per second
    public float manaRegenBonus = 0f;    // Mana regenerated per second
    public float staminaRegenBonus = 0f; // Stamina regenerated per second
    public bool grantInfiniteMana = false;

    public override void UseItem(PlayerController player)
    {
        // 1. Health Restore
        if (healthRestore > 0)
        {
            var health = player.GetComponent<Health>();
            if (health != null) health.Heal(healthRestore);
        }

        // 2. Mana Restore
        if (manaRestore > 0)
        {
            var resources = player.GetComponent<ResourceSystem>();
            if (resources != null) resources.RestoreMana(manaRestore);
        }

        // 3. Ultimate Charge
        if (ultimateCharge > 0)
        {
            var resources = player.GetComponent<ResourceSystem>();
            if (resources != null) resources.AddUltimateCharge(ultimateCharge);
        }

        // 4. Temporary Speed Boost
        if (speedMultiplier > 1f && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplySpeedBoost(speedMultiplier, buffDuration);
        }

        // 5. Temporary Infinite Stamina
        if (grantInfiniteStamina && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplyInfiniteStamina(buffDuration);
        }

        // 6. Temporary Health Regen
        if (healthRegenBonus > 0f && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplyHealthRegen(healthRegenBonus, buffDuration);
        }

        // 7. Temporary Mana Regen
        if (manaRegenBonus > 0f && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplyManaRegen(manaRegenBonus, buffDuration);
        }

        // 8. Temporary Stamina Regen
        if (staminaRegenBonus > 0f && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplyStaminaRegen(staminaRegenBonus, buffDuration);
        }

        // 9. Temporary Infinite Mana
        if (grantInfiniteMana && buffDuration > 0f)
        {
            var buffs = player.GetComponent<BuffManager>();
            if (buffs != null) buffs.ApplyInfiniteMana(buffDuration);
        }

        Debug.Log($"[Potion] {player.name} consumed potion: {itemName}");
    }
}
