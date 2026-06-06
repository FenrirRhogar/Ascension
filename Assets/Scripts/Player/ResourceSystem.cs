using UnityEngine;

public class ResourceSystem : MonoBehaviour
{
    private PlayerController playerController;
    
    public float currentMana { get; private set; }
    public float currentStamina { get; private set; }
    public float currentUltimate { get; private set; }
    
    public float maxMana { get; private set; }
    public float maxStamina { get; private set; }
    public float maxUltimate { get; } = 100f;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Called when class is initialized
    public void InitializeResources(CharacterClassSO classData)
    {
        maxMana = classData.maxMana;
        maxStamina = classData.maxStamina;
        
        currentMana = maxMana;
        currentStamina = maxStamina;
        currentUltimate = 0f;
    }

    void Update()
    {
        if (playerController.currentClass == null) return;

        // --- Mana Regeneration ---
        // Only regenerate if not actively holding an ability, OR if we have temporary mana regen active
        bool isHoldingAbility = GetComponent<CombatSystem>() != null && GetComponent<CombatSystem>().IsHoldingAbility();
        var buffs = GetComponent<BuffManager>();

        float currentManaRegen = playerController.currentClass.manaRegenRate;
        if (buffs != null && buffs.manaRegenBuffTimeLeft > 0)
        {
            currentManaRegen += buffs.manaRegenBuffAmount;
        }

        if ((!isHoldingAbility || (buffs != null && buffs.manaRegenBuffTimeLeft > 0)) && currentMana < maxMana)
        {
            float rate = isHoldingAbility ? buffs.manaRegenBuffAmount : currentManaRegen;
            currentMana += rate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
        }

        // --- Stamina Regeneration ---
        // Restore stamina if we are not actively consuming it (e.g., standing still or out of stamina)
        if (!playerController.isActuallySprinting && currentStamina < maxStamina)
        {
            float rate = playerController.currentClass.staminaRegenRate;
            if (buffs != null && buffs.staminaRegenBuffTimeLeft > 0)
            {
                rate += buffs.staminaRegenBuffAmount;
            }
            currentStamina += rate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    public bool UseMana(float amount)
    {
        var buffs = GetComponent<BuffManager>();
        if (buffs != null && buffs.isInfiniteManaActive)
        {
            return true;
        }
        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }
        return false;
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        return false;
    }

    public void RestoreMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }

    public void AddUltimateCharge(float amount)
    {
        currentUltimate += amount;
        currentUltimate = Mathf.Clamp(currentUltimate, 0, maxUltimate);
    }

    public bool UseUltimate()
    {
        if (currentUltimate >= maxUltimate)
        {
            currentUltimate = 0;
            return true;
        }
        return false;
    }
}
