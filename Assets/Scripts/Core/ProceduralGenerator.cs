using UnityEngine;
using System.Collections.Generic;

public class ProceduralGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] obstaclePrefabs;
    public GameObject[] npcPrefabs;
    public GameObject[] consumablePrefabs;
    public GameObject[] hitTextPrefabs;
    public GameObject levelGatePrefab;
    
    [Header("Counts")]
    public int numberOfObstacles = 20;
    public int numberOfNPCs = 5;
    
    [Header("Map Settings")]
    public Vector2 mapSize = new Vector2(10f, 10f);
    public float overlapCheckRadius = 1.5f;
    public float enemyScale = 0.5f; 
    public LayerMask collisionMask;
    public GameObject groundObject; 

    public void RunGeneration()
    {
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
            GenerateNatureObjects(obstaclePrefabs, numberOfObstacles, "Obstacle");
        else
            GeneratePrimitives(PrimitiveType.Cube, numberOfObstacles, "Obstacle", Color.grey, 0.5f);

        // Scale enemy count: +3 enemies per level
        int currentLevel = LevelManager.CurrentLevel;
        int scaledNPCs = numberOfNPCs + ((currentLevel - 1) * 3);

        if (npcPrefabs != null && npcPrefabs.Length > 0)
            GenerateObjects(npcPrefabs, scaledNPCs, "Enemy", 0.1f);
        else
            GeneratePrimitives(PrimitiveType.Capsule, scaledNPCs, "Enemy", Color.red, 1.0f);

        SpawnLevelGate();
    }

    private void SpawnLevelGate()
    {
        if (levelGatePrefab == null)
        {
            Debug.LogError("[ProceduralGenerator] No Level Gate Prefab assigned in LevelManager!");
            return;
        }

        Vector3 finalSpawnPos = Vector3.zero;
        bool foundSpot = false;

        for (int i = 0; i < 50; i++) // Increased attempts
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float distance = Random.Range(mapSize.x * 0.35f, mapSize.x * 0.48f); 

            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            Vector3 rayStart = new Vector3(x, 150f, z);
            Vector3 tempPos = new Vector3(x, 0, z); 

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f))
            {
                tempPos = hit.point;
            }

            if (IsPositionClear(tempPos)) 
            {
                finalSpawnPos = tempPos;
                foundSpot = true;
                break;
            }
        }

        // Failsafe: If we absolutely can't find a clear spot, force it somewhere
        if (!foundSpot)
        {
            finalSpawnPos = GetRandomSurfacePoint();
            Debug.LogWarning("[ProceduralGenerator] Forced gate spawn (ignoring overlaps).");
        }

        GameObject gate = Instantiate(levelGatePrefab, finalSpawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
        gate.name = "LevelGate_EXIT"; // Give it a clear name in the Hierarchy
        gate.transform.SetParent(this.transform); // Put it under the generator

        // Ensure it can be touched
        var col = gate.GetComponent<Collider>();
        if (col == null) col = gate.AddComponent<BoxCollider>();
        col.isTrigger = true;
        
        if (col is BoxCollider bc)
        {
            bc.size = new Vector3(4f, 4f, 4f); // Make sure the trigger is large enough to hit
        }

        Debug.Log($"[ProceduralGenerator] ➔ SUCCESS: Spawned Level Gate at {finalSpawnPos}");
    }

    void GenerateNatureObjects(GameObject[] prefabs, int targetCount, string tag)
    {
        int objectsSpawned = 0;
        int attempts = 0;

        while (objectsSpawned < targetCount && attempts < 2000)
        {
            attempts++;
            Vector3 surfacePoint = GetRandomSurfacePoint();
            Vector3 spawnPos = surfacePoint + new Vector3(0, -0.2f, 0);

            if (IsPositionClear(spawnPos))
            {
                GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];
                
                Quaternion randomRot = Quaternion.Euler(
                    selectedPrefab.name.Contains("rock") ? Random.Range(-5, 5) : 0, 
                    Random.Range(0, 360), 
                    selectedPrefab.name.Contains("rock") ? Random.Range(-5, 5) : 0
                );

                GameObject go = Instantiate(selectedPrefab, spawnPos, randomRot, this.transform);
                go.tag = tag;
                
                MaterialFixer.Fix(go);
                
                float randomScale = Random.Range(0.8f, 1.5f);
                go.transform.localScale = Vector3.one * randomScale;

                InitializeComponents(go, tag, selectedPrefab);
                objectsSpawned++;
            }
        }
    }

    void GenerateObjects(GameObject[] prefabs, int targetCount, string tag, float verticalOffset)
    {
        int objectsSpawned = 0;
        int attempts = 0;

        while (objectsSpawned < targetCount && attempts < 1000)
        {
            attempts++;
            Vector3 surfacePoint = GetRandomSurfacePoint();
            Vector3 spawnPos = surfacePoint + new Vector3(0, verticalOffset, 0);

            if (IsPositionClear(spawnPos))
            {
                GameObject selectedPrefab = LevelManager.GetWeightedRandomItem(prefabs);
                Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                GameObject go = Instantiate(selectedPrefab, spawnPos, randomRot, this.transform);
                go.tag = tag;
                
                MaterialFixer.Fix(go); 
                
                InitializeComponents(go, tag, selectedPrefab);
                objectsSpawned++;
            }
        }
    }

    bool IsPositionClear(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, overlapCheckRadius, collisionMask);
        foreach (var col in colliders)
        {
            if (groundObject == null || col.gameObject != groundObject)
            {
                return false;
            }
        }
        return true;
    }

    void InitializeComponents(GameObject go, string tag, GameObject sourcePrefab = null)
    {
        if (tag == "Enemy")
        {
            go.layer = LayerMask.NameToLayer("Default");
            
            go.transform.localScale = Vector3.one * enemyScale;

            if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
            {
                var col = go.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 1f, 0);
                col.height = 2f;
                col.radius = 1.0f;
            }

            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            
            // --- AI Logic ---
            // Only add the basic EnemyAI if the prefab doesn't already have its own AI script
            if (go.GetComponent<EnemyAI>() == null && go.GetComponent<RangedEnemyAI>() == null)
                go.AddComponent<EnemyAI>();
                
            // --- SCALING DIFFICULTY ---
            int currentLevel = LevelManager.CurrentLevel;
            float timeFactor = LevelManager.DifficultyFactor;
            
            EnemyAI ai = go.GetComponent<EnemyAI>();
            if (ai != null)
            {
                // Base damage is 25. Increase by 25% per level, then multiply by time factor.
                float baseDamage = 25f * (1f + (currentLevel - 1) * 0.25f);
                ai.attackDamage = Mathf.RoundToInt(baseDamage * timeFactor); 
                
                // Base speed is handled by NavMeshAgent. Increase by 15% per level, then scale with the square root of time factor.
                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.speed *= (1f + (currentLevel - 1) * 0.15f) * Mathf.Sqrt(timeFactor); 
            }
            
            Health health = go.GetComponent<Health>();
            if (health == null)
                health = go.AddComponent<Health>();
            
            health.hitTextPrefabs = hitTextPrefabs;
            
            // Base health is 200. Increase by 50% per level, then multiply by time factor.
            float baseHealth = 200f * (1f + (currentLevel - 1) * 0.5f);
            int scaledHealth = Mathf.RoundToInt(baseHealth * timeFactor);
            health.SetMaxHealth(scaledHealth); 

            if (consumablePrefabs != null && consumablePrefabs.Length > 0)
            {
                health.lootPrefabs = consumablePrefabs;
                health.dropChance = 1.0f; 
            }

            if (go.GetComponent<EnemyHealthBar>() == null)
                go.AddComponent<EnemyHealthBar>();
        }
        else if (tag == "Consumable")
        {
            if (sourcePrefab != null)
                go.transform.localScale = sourcePrefab.transform.localScale;
            else
                go.transform.localScale = Vector3.one * 2.0f; 
            
            if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
            {
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.0f; 
            }
            else
            {
                var existingCols = go.GetComponentsInChildren<Collider>();
                foreach(var c in existingCols) c.isTrigger = true;
            }
            
            PickupItem spawnedPickup = go.GetComponent<PickupItem>();
            if (spawnedPickup == null)
            {
                spawnedPickup = go.AddComponent<PickupItem>();
            }

            if (sourcePrefab != null)
            {
                PickupItem originalPickup = sourcePrefab.GetComponent<PickupItem>();
                if (originalPickup != null && originalPickup.itemData != null)
                {
                    spawnedPickup.itemData = originalPickup.itemData;
                }
            }
        }
        else if (tag == "Obstacle")
        {
            if (go.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.carving = true;
            }
        }
    }

    void GeneratePrimitives(PrimitiveType type, int targetCount, string tag, Color color, float verticalOffset)
    {
        int objectsSpawned = 0;
        int attempts = 0;

        while (objectsSpawned < targetCount && attempts < 1000)
        {
            attempts++;
            Vector3 surfacePoint = GetRandomSurfacePoint();
            Vector3 spawnPos = surfacePoint + new Vector3(0, verticalOffset, 0);

            if (IsPositionClear(spawnPos))
            {
                GameObject go = GameObject.CreatePrimitive(type);
                go.transform.position = spawnPos;
                go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                go.transform.SetParent(this.transform);
                go.tag = tag;
                
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = color;

                MaterialFixer.Fix(go);

                InitializeComponents(go, tag);
                objectsSpawned++;
            }
        }
    }

    Vector3 GetRandomSurfacePoint()
    {
        float x = Random.Range(-mapSize.x / 2f, mapSize.x / 2f);
        float z = Random.Range(-mapSize.y / 2f, mapSize.y / 2f);
        
        Vector3 rayStart = new Vector3(x, 150f, z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f))
        {
            return hit.point;
        }

        return new Vector3(x, 0, z);
    }
}
