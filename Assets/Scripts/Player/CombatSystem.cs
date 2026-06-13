using UnityEngine;
using UnityEngine.InputSystem;

public class CombatSystem : MonoBehaviour
{
    private PlayerController playerController;
    private ResourceSystem resourceSystem;
    private float lastAttackTime;
    private float lastAbilityTime;
    private bool isHoldingAbility = false;
    private bool isAbilityInputHeld = false; // Tracks the physical button state

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        resourceSystem = GetComponent<ResourceSystem>();
    }

    void Update()
    {
        // If the button is physically held but the ability hasn't officially started (e.g. was on cooldown)
        if (isAbilityInputHeld && !isHoldingAbility && playerController.currentClass != null && resourceSystem != null)
        {
            if (Time.time >= lastAbilityTime + playerController.currentClass.abilityCooldown)
            {
                if (resourceSystem.UseMana(playerController.currentClass.abilityManaCost))
                {
                    isHoldingAbility = true;
                    playerController.currentClass.ExecuteAbility(playerController, playerController.animator);
                    lastAbilityTime = Time.time;

                    if (SoundManager.Instance != null && playerController.currentClass.abilitySFX != null)
                        SoundManager.Instance.PlaySound(playerController.currentClass.abilitySFX);
                }
            }
        }

        // If the ability is actively channeling, run the hold logic EVERY frame
        if (isHoldingAbility && playerController.currentClass != null)
        {
            playerController.currentClass.OnAbilityHold(playerController, playerController.animator);
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && playerController.currentClass != null)
        {
            if (Time.time >= lastAttackTime + playerController.currentClass.attackCooldown)
            {
                playerController.currentClass.ExecuteAttack(playerController, playerController.animator);
                lastAttackTime = Time.time;

                if (SoundManager.Instance != null && playerController.currentClass.attackSFX != null)
                    SoundManager.Instance.PlaySound(playerController.currentClass.attackSFX);
            }
        }
    }

    public void OnAbility(InputValue value)
    {
        isAbilityInputHeld = value.isPressed;

        if (!isAbilityInputHeld && isHoldingAbility)
        {
            // RELEASE
            isHoldingAbility = false;
            if (playerController.currentClass != null)
            {
                playerController.currentClass.OnAbilityReleased(playerController, playerController.animator);
            }
        }
    }

    public bool IsHoldingAbility()
    {
        return isHoldingAbility;
    }

    // This helps the class script force-stop the ability if mana runs out
    public void ForceStopAbility()
    {
        if (isHoldingAbility)
        {
            isHoldingAbility = false;
            if (playerController.currentClass != null)
            {
                playerController.currentClass.OnAbilityReleased(playerController, playerController.animator);
            }
        }
    }

    public void OnUltimate(InputValue value)
    {
        if (value.isPressed && playerController.currentClass != null && resourceSystem != null)
        {
            if (resourceSystem.UseUltimate())
            {
                playerController.currentClass.ExecuteUltimate(playerController, playerController.animator);
                Debug.Log($"[Combat] {playerController.name} UNLEASHED ULTIMATE!");

                if (SoundManager.Instance != null && playerController.currentClass.ultimateSFX != null)
                    SoundManager.Instance.PlaySound(playerController.currentClass.ultimateSFX);
            }
            else
            {
                Debug.Log($"[Combat] Ultimate not ready! ({resourceSystem.currentUltimate:F0}%)");
            }
        }
    }
}
