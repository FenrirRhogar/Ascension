using UnityEngine;
using System.Collections;

public class BuffManager : MonoBehaviour
{
    private PlayerController player;
    private ResourceSystem resources;

    private bool isSpeedBuffActive = false;
    public bool isInfiniteStaminaActive { get; private set; } = false;
    public bool isInfiniteManaActive { get; private set; } = false;

    private float currentSpeedBoostAmount = 0f;
    public float speedBuffTimeLeft { get; private set; }
    public float staminaBuffTimeLeft { get; private set; }
    public float manaBuffTimeLeft { get; private set; }

    public float maxSpeedBuffDuration { get; private set; }
    public float maxStaminaBuffDuration { get; private set; }
    public float maxManaBuffDuration { get; private set; }

    // Regeneration buff timers
    public float manaRegenBuffTimeLeft { get; private set; }
    public float staminaRegenBuffTimeLeft { get; private set; }
    public float healthRegenBuffTimeLeft { get; private set; }

    public float maxManaRegenBuffDuration { get; private set; }
    public float maxStaminaRegenBuffDuration { get; private set; }
    public float maxHealthRegenBuffDuration { get; private set; }

    // Regeneration buff amounts
    public float manaRegenBuffAmount { get; private set; }
    public float staminaRegenBuffAmount { get; private set; }
    public float healthRegenBuffAmount { get; private set; }

    void Awake()
    {
        player = GetComponent<PlayerController>();
        resources = GetComponent<ResourceSystem>();
    }

    void Update()
    {
        if (speedBuffTimeLeft > 0) speedBuffTimeLeft -= Time.deltaTime;
        if (staminaBuffTimeLeft > 0) staminaBuffTimeLeft -= Time.deltaTime;
        if (manaBuffTimeLeft > 0) manaBuffTimeLeft -= Time.deltaTime;

        // Count down regeneration buff timers
        if (manaRegenBuffTimeLeft > 0) manaRegenBuffTimeLeft -= Time.deltaTime;
        if (staminaRegenBuffTimeLeft > 0) staminaRegenBuffTimeLeft -= Time.deltaTime;
        if (healthRegenBuffTimeLeft > 0) healthRegenBuffTimeLeft -= Time.deltaTime;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        maxSpeedBuffDuration = duration;
        speedBuffTimeLeft = duration;
        
        if (isSpeedBuffActive)
        {
            StopCoroutine("SpeedBoostRoutine");
            // Revert previous boost before starting a new one
            if (player.currentClass != null)
                player.currentClass.baseMovementSpeed -= currentSpeedBoostAmount;
        }
        
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        isSpeedBuffActive = true;
        
        // Calculate based on the current (possibly base) speed
        currentSpeedBoostAmount = player.currentClass.baseMovementSpeed * (multiplier - 1f);
        player.currentClass.baseMovementSpeed += currentSpeedBoostAmount;
        
        yield return new WaitForSeconds(duration);
        
        if (player.currentClass != null)
            player.currentClass.baseMovementSpeed -= currentSpeedBoostAmount;
            
        currentSpeedBoostAmount = 0f;
        isSpeedBuffActive = false;
        speedBuffTimeLeft = 0;
    }

    public void ApplyInfiniteStamina(float duration)
    {
        maxStaminaBuffDuration = duration;
        staminaBuffTimeLeft = duration;
        if (isInfiniteStaminaActive) StopCoroutine("StaminaBoostRoutine");
        StartCoroutine(StaminaBoostRoutine(duration));
    }

    private IEnumerator StaminaBoostRoutine(float duration)
    {
        isInfiniteStaminaActive = true;
        yield return new WaitForSeconds(duration);
        isInfiniteStaminaActive = false;
        staminaBuffTimeLeft = 0;
    }

    public void ApplyInfiniteMana(float duration)
    {
        maxManaBuffDuration = duration;
        manaBuffTimeLeft = duration;
        if (isInfiniteManaActive) StopCoroutine("ManaBoostRoutine");
        StartCoroutine(ManaBoostRoutine(duration));
    }

    private IEnumerator ManaBoostRoutine(float duration)
    {
        isInfiniteManaActive = true;
        yield return new WaitForSeconds(duration);
        isInfiniteManaActive = false;
        manaBuffTimeLeft = 0;
    }

    public void ApplyManaRegen(float amount, float duration)
    {
        manaRegenBuffAmount = amount;
        manaRegenBuffTimeLeft = duration;
        maxManaRegenBuffDuration = duration;
    }

    public void ApplyStaminaRegen(float amount, float duration)
    {
        staminaRegenBuffAmount = amount;
        staminaRegenBuffTimeLeft = duration;
        maxStaminaRegenBuffDuration = duration;
    }

    public void ApplyHealthRegen(float amount, float duration)
    {
        healthRegenBuffAmount = amount;
        healthRegenBuffTimeLeft = duration;
        maxHealthRegenBuffDuration = duration;
    }
}
