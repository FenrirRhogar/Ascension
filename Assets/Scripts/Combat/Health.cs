using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int currentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    public bool isPlayer = false;
    private PlayerController playerController;
    private float timeSinceLastDamage = 0f;
    private float healthRegenAccumulator = 0f;

    [Header("Loot Settings")]
    public GameObject[] lootPrefabs;
    [Range(0f, 1f)] public float dropChance = 0.3f; // 30% chance to drop an item

    [Header("Visual Effects")]
    public GameObject[] hitTextPrefabs;
    public Vector3 hitTextOffset = new Vector3(0, 2.2f, 0);

    [Header("SFX")]
    public AudioClip hurtSFX;
    public AudioClip deathSFX;

    void Start()
    {
        currentHealth = maxHealth;
        if (isPlayer) playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (isPlayer && playerController != null && playerController.currentClass != null)
        {
            timeSinceLastDamage += Time.deltaTime;

            // Start regenerating if we've been out of combat long enough or if we have a health regeneration buff active
            var buffs = GetComponent<BuffManager>();
            float temporaryRegenRate = 0f;
            if (buffs != null && buffs.healthRegenBuffTimeLeft > 0f)
            {
                temporaryRegenRate = buffs.healthRegenBuffAmount;
            }

            bool isOutOfCombat = timeSinceLastDamage >= playerController.currentClass.outOfCombatDelay;
            if ((isOutOfCombat || temporaryRegenRate > 0f) && currentHealth < maxHealth)
            {
                float totalRegenRate = temporaryRegenRate;
                if (isOutOfCombat)
                {
                    totalRegenRate += playerController.currentClass.healthRegenRate;
                }
                
                healthRegenAccumulator += totalRegenRate * Time.deltaTime;
                
                if (healthRegenAccumulator >= 1f)
                {
                    int regenAmount = Mathf.FloorToInt(healthRegenAccumulator);
                    Heal(regenAmount);
                    healthRegenAccumulator -= regenAmount;
                }
            }
        }
    }

    public void SetMaxHealth(int amount)
    {
        maxHealth = amount;
        currentHealth = maxHealth;
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        healthRegenAccumulator = 0f;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        timeSinceLastDamage = 0f; // Reset combat timer

        if (SoundManager.Instance != null && hurtSFX != null && currentHealth > 0)
            SoundManager.Instance.PlaySound(hurtSFX);
        
        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}");

        // Spawn Random Hit Text (for enemies/non-players)
        if (!isPlayer && hitTextPrefabs != null && hitTextPrefabs.Length > 0)
        {
            GameObject prefab = hitTextPrefabs[Random.Range(0, hitTextPrefabs.Length)];
            if (prefab != null)
            {
                Instantiate(prefab, transform.position + hitTextOffset, Quaternion.identity);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}");
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");

        if (SoundManager.Instance != null && deathSFX != null)
            SoundManager.Instance.PlaySound(deathSFX);
        
        if (isPlayer)
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnPlayerDeath(gameObject);
            }
            
            // Hide player but keep object alive for revive logic
            // (Setting active false might break scripts, better to move far away or disable renderers)
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            
            // Move player under the map temporarily
            transform.position += Vector3.down * 50f;
        }
        else
        {
            // If this is the Boss, trigger the victory sequence immediately
            if (gameObject.name == "MOUNTAIN_TITAN_BOSS")
            {
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.CompleteLevel();
                }
            }

            // Drop Loot before destroying the enemy
            if (LevelManager.Instance != null)
            {
                // 1. Independent Potion Drop
                if (Random.value <= LevelManager.Instance.globalPotionDropRatio && LevelManager.Instance.PotionPrefabs != null && LevelManager.Instance.PotionPrefabs.Length > 0)
                {
                    SpawnLoot(LevelManager.GetWeightedRandomItem(LevelManager.Instance.PotionPrefabs), false);
                }

                // 2. Independent Artifact Drop
                if (Random.value <= LevelManager.Instance.globalArtifactDropRatio && LevelManager.Instance.ArtifactPrefabs != null && LevelManager.Instance.ArtifactPrefabs.Length > 0)
                {
                    SpawnLoot(LevelManager.GetWeightedRandomItem(LevelManager.Instance.ArtifactPrefabs), true);
                }
            }
            else if (lootPrefabs != null && lootPrefabs.Length > 0 && Random.value <= dropChance)
            {
                // Fallback to local enemy drop list
                SpawnLoot(LevelManager.GetWeightedRandomItem(lootPrefabs), false);
            }

            Destroy(gameObject);
        }
    }

    private void SpawnLoot(GameObject droppedPrefab, bool isArtifact)
    {
        if (droppedPrefab == null) return;

        // Spawn slightly above ground with a tiny random offset
        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
        GameObject go = Instantiate(droppedPrefab, spawnPos, Quaternion.identity);
        go.tag = "Consumable";
        go.transform.localScale = droppedPrefab.transform.localScale;

        // CRITICAL: Force a prominent trigger collider for easy raycasting
        if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
        {
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.0f; // Generous pickup radius
        }
        else
        {
            // Ensure existing colliders don't block movement but can be raycasted
            var existingCols = go.GetComponentsInChildren<Collider>();
            foreach (var c in existingCols) c.isTrigger = true;
        }

        PickupItem spawnedPickup = go.GetComponent<PickupItem>();
        if (spawnedPickup == null)
        {
            spawnedPickup = go.AddComponent<PickupItem>();
        }

        // Copy the Item Data from the original prefab to the clone
        PickupItem originalPickup = droppedPrefab.GetComponent<PickupItem>();
        Sprite icon = null;
        if (originalPickup != null && originalPickup.itemData != null)
        {
            icon = originalPickup.itemData.inventoryIcon;
            spawnedPickup.itemData = originalPickup.itemData;
        }

        if (isArtifact)
        {
            // If it's a generic artifact prefab, it might not have itemData yet, generate one
            if (spawnedPickup.itemData == null || !(spawnedPickup.itemData is ArtifactSO))
            {
                ArtifactSO artifact = ArtifactManager.CreateRandomArtifact(icon);
                spawnedPickup.itemData = artifact;
            }
            go.name = "Artifact_" + (spawnedPickup.itemData != null ? spawnedPickup.itemData.itemName : "Item");
        }
        else
        {
            go.name = "Potion_" + (spawnedPickup.itemData != null ? spawnedPickup.itemData.itemName : "Item");
        }

        Debug.Log($"[Loot] {gameObject.name} dropped {go.name}!");
    }
}
