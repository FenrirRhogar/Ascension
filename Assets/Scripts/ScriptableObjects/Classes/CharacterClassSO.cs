using UnityEngine;

public abstract class CharacterClassSO : ScriptableObject
{
    public string className;
    
    [Header("Base Stats")]
    public int maxHealth = 100;
    public float baseMovementSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpHeight = 1.5f;
    public float attackCooldown = 0.5f;
    public float referenceAnimSpeed = 5.0f; // Speed at which animation is 1.0x

    [Header("Resource Stats")]
    public float maxMana = 100f;
    public float manaRegenRate = 5f;
    public float abilityManaCost = 30f;
    public float abilityCooldown = 2.0f;
    
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float staminaSprintCost = 20f; // per second

    public float manaDrainPerSecond = 10f; // for channeled abilities
    public float healthRegenRate = 2f; // HP per second outside combat
    public float outOfCombatDelay = 5f; // Seconds before regen starts after taking damage

    [Header("Visual Assets")]
    public GameObject characterModelPrefab;
    public float modelScale = 1f;
    public Vector3 modelOffset = Vector3.zero;
    public float vfxHeightOffset = 1.2f; // Base height for spawning combat effects
    public Sprite classIcon;

    [Header("Weapon Settings")]
    public GameObject weaponPrefab;
    public HumanBodyBones handBone = HumanBodyBones.RightHand;
    public Vector3 weaponPositionOffset;
    public Vector3 weaponRotationOffset;

    [Header("VFX Prefabs")]
    public GameObject attackVFX;
    public GameObject abilityVFX;
    public GameObject ultimateVFX;

    [Header("SFX Clips")]
    public AudioClip attackSFX;
    public AudioClip abilitySFX;
    public AudioClip ultimateSFX;
    
    // Primary Attack
    public abstract void ExecuteAttack(PlayerController player, Animator animator);

    // Special Ability
    public abstract void ExecuteAbility(PlayerController player, Animator animator);

    // Ultimate Ability
    public abstract void ExecuteUltimate(PlayerController player, Animator animator);

    // Channeled Ability Support
    public virtual void OnAbilityHold(PlayerController player, Animator animator) { }
    public virtual void OnAbilityReleased(PlayerController player, Animator animator) { }

    // Legacy support
    public virtual void ExecuteAction(PlayerController player, Animator animator)
    {
        ExecuteAbility(player, animator);
    }
}
