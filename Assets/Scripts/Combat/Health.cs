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
    public Vector3 hitTextOffset = new Vector3(0, 2.5f, 0);

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
        
        if (isPlayer)
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RespawnPlayer(gameObject);
            }
        }
        else
        {
            // Drop Loot before destroying the enemy
            if (Random.value <= dropChance)
            {
                GameObject droppedPrefab = null;
                bool isArtifact = false;

                bool allowArtifacts = LevelManager.Instance != null && 
                                      LevelManager.Instance.ArtifactPrefabs != null && 
                                      LevelManager.Instance.ArtifactPrefabs.Length > 0;

                if (LevelManager.Instance != null && 
                    LevelManager.Instance.PotionPrefabs != null && LevelManager.Instance.PotionPrefabs.Length > 0)
                {
                    // Global pool mode configured on LevelManager
                    float ratio = LevelManager.Instance.ArtifactDropRatio;
                    if (allowArtifacts && Random.value <= ratio)
                    {
                        isArtifact = true;
                        droppedPrefab = LevelManager.GetWeightedRandomItem(LevelManager.Instance.ArtifactPrefabs);
                    }
                    else
                    {
                        // Filter PotionPrefabs in case an artifact crept in
                        var validPotions = new System.Collections.Generic.List<GameObject>();
                        foreach (var prefab in LevelManager.Instance.PotionPrefabs)
                        {
                            if (prefab == null) continue;
                            PickupItem pickup = prefab.GetComponent<PickupItem>();
                            bool isArt = pickup != null && pickup.itemData is ArtifactSO;
                            if (!isArt || allowArtifacts)
                            {
                                validPotions.Add(prefab);
                            }
                        }

                        if (validPotions.Count > 0)
                        {
                            droppedPrefab = LevelManager.GetWeightedRandomItem(validPotions.ToArray());
                        }
                    }
                }
                else if (lootPrefabs != null && lootPrefabs.Length > 0)
                {
                    // Fallback to local enemy drop list
                    var validDrops = new System.Collections.Generic.List<GameObject>();
                    foreach (var prefab in lootPrefabs)
                    {
                        if (prefab == null) continue;
                        PickupItem pickup = prefab.GetComponent<PickupItem>();
                        bool isArt = pickup != null && pickup.itemData is ArtifactSO;
                        if (!isArt || allowArtifacts)
                        {
                            validDrops.Add(prefab);
                        }
                    }

                    if (validDrops.Count > 0)
                    {
                        droppedPrefab = LevelManager.GetWeightedRandomItem(validDrops.ToArray());
                    }
                }

                if (droppedPrefab != null)
                {
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
                        foreach(var c in existingCols) c.isTrigger = true;
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
                        go.name = "Artifact_" + (spawnedPickup.itemData != null ? spawnedPickup.itemData.itemName : "Item");
                    }
                    else if (LevelManager.Instance == null || LevelManager.Instance.ArtifactPrefabs == null || LevelManager.Instance.ArtifactPrefabs.Length == 0)
                    {
                        // Fallback conversion for local lootPrefabs when global pools are unconfigured
                        if (Random.value <= 0.30f)
                        {
                            ArtifactSO artifact = ArtifactManager.CreateRandomArtifact(icon);
                            spawnedPickup.itemData = artifact;
                            go.name = "Artifact_" + artifact.itemName;
                        }
                    }

                    Debug.Log($"[Loot] {gameObject.name} dropped {droppedPrefab.name}!");
                }
            }

            Destroy(gameObject);
        }
    }
}
