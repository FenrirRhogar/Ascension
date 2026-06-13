using UnityEngine;
using System.Collections.Generic;

public class ProceduralGenerator : MonoBehaviour
{
    [Header("Assets")]
    public GameObject[] obstaclePrefabs;
    public GameObject[] npcPrefabs;
    public GameObject[] consumablePrefabs;
    public GameObject[] hitTextPrefabs;
    public GameObject levelGatePrefab;
    public GameObject campfirePrefab;

    [Header("Settings")]
    public Vector2 mapSize = new Vector2(100f, 100f);
    public int obstacleDensity = 50;
    public int npcDensity = 10;
    public int campfireDensity = 5;
    public LayerMask collisionMask;
    public GameObject groundObject;

    public void RunGeneration()
    {
        bool isBossLevel = LevelManager.CurrentLevel == 5;

        // 0. Spawning Campfires (especially for Night levels)
        if (campfirePrefab != null)
        {
            int count = (AtmosphereManager.Instance != null && AtmosphereManager.Instance.isNight) ? campfireDensity * 2 : campfireDensity;
            SpawnGroup(new GameObject[] { campfirePrefab }, count, "LightSource");
        }

        // 1. Spawning Obstacles
        if (obstaclePrefabs != null) SpawnGroup(obstaclePrefabs, isBossLevel ? obstacleDensity / 4 : obstacleDensity, "Obstacle");

        // 2. Spawning Enemies
        if (npcPrefabs != null) 
        {
            if (isBossLevel)
            {
                // Spawn a few minions and ONE massive Boss
                SpawnGroup(npcPrefabs, 5, "Enemy");
                SpawnBoss();
            }
            else
            {
                SpawnGroup(npcPrefabs, npcDensity + LevelManager.CurrentLevel * 2, "Enemy");
            }
        }

        // 3. Spawning Gate
        if (levelGatePrefab != null) SpawnLevelGate();
    }

    private void SpawnBoss()
    {
        Vector3 pos = GetRandomGroundPos();
        if (pos.y < -90f) pos = new Vector3(0, 0, 0); // Fallback

        GameObject selected = npcPrefabs[0]; // Just use the first enemy prefab as a base
        GameObject boss = Instantiate(selected, pos, Quaternion.identity, this.transform);
        boss.tag = "Enemy";
        boss.name = "MOUNTAIN_TITAN_BOSS";
        
        // Massive Scale
        boss.transform.localScale = Vector3.one * 3.0f;
        boss.layer = 3; 
        foreach (Transform child in boss.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 3;

        MaterialFixer.Fix(boss);
        InitializeComponents(boss, "Boss");
    }

    private void SpawnGroup(GameObject[] pool, int count, string tag)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomGroundPos();
            if (pos.y < -90f) continue;

            GameObject selected = pool[Random.Range(0, pool.Length)];
            GameObject go = Instantiate(selected, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), this.transform);
            go.tag = tag;
            
            if (tag == "Enemy")
            {
                go.transform.localScale = Vector3.one * 0.5f;
                go.layer = 3; // Assign to an empty layer (Layer 3 is empty in TagManager)
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = 3;
                }
            }

            if (tag == "Obstacle")
            {
                var tint = go.AddComponent<BiomeTint>();
                tint.ApplyTint(LevelManager.GetCurrentBiome());
            }

            MaterialFixer.Fix(go);
            InitializeComponents(go, tag);
        }
    }

    private void SpawnLevelGate()
    {
        for (int i = 0; i < 20; i++)
        {
            // Spawn far away
            float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
            float dist = mapSize.x * 0.4f;
            Vector3 searchPos = new Vector3(Mathf.Cos(angle) * dist, 500f, Mathf.Sin(angle) * dist);

            if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, 1000f, collisionMask))
            {
                GameObject gate = Instantiate(levelGatePrefab, hit.point, Quaternion.identity, this.transform);
                gate.name = "LevelGate_EXIT";
                var col = gate.GetComponent<Collider>();
                if (col == null) col = gate.AddComponent<BoxCollider>();
                col.isTrigger = true;
                return;
            }
        }
    }

    private Vector3 GetRandomGroundPos()
    {
        float rx = Random.Range(-mapSize.x * 0.45f, mapSize.x * 0.45f);
        float rz = Random.Range(-mapSize.y * 0.45f, mapSize.y * 0.45f);
        
        Ray ray = new Ray(new Vector3(rx, 500f, rz), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, collisionMask))
        {
            return hit.point;
        }
        return new Vector3(0, -100f, 0);
    }

    private void InitializeComponents(GameObject go, string tag)
    {
        if (tag == "Enemy" || tag == "Boss")
        {
            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            if (go.GetComponent<EnemyAI>() == null && go.GetComponent<RangedEnemyAI>() == null) go.AddComponent<EnemyAI>();
            
            Health h = go.GetComponent<Health>();
            if (h == null) h = go.AddComponent<Health>();
            h.hitTextPrefabs = hitTextPrefabs;

            // --- TIME-BASED SCALING ---
            // Base health + Level scaling + Time scaling (10 HP per second elapsed in the run)
            int timeBonus = Mathf.FloorToInt(LevelManager.runTime * 10f);
            
            if (tag == "Boss")
            {
                // Boss is massively stronger and scales harder with time
                h.SetMaxHealth(5000 + (timeBonus * 3));
            }
            else
            {
                h.SetMaxHealth(200 + (LevelManager.CurrentLevel * 50) + timeBonus);
            }

            if (consumablePrefabs != null && consumablePrefabs.Length > 0)
            {
                h.lootPrefabs = consumablePrefabs;
                h.dropChance = tag == "Boss" ? 1.0f : 0.3f; // Boss always drops loot
            }

            if (go.GetComponent<EnemyHealthBar>() == null) go.AddComponent<EnemyHealthBar>();

            // FORCE a CapsuleCollider if missing or on children only
            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            
            // Adjust collider based on scale
            if (tag == "Boss")
            {
                cap.center = new Vector3(0, 1f, 0); 
                cap.height = 2f;
                cap.radius = 0.5f;
            }
            else
            {
                // Configure for 0.5 scale
                cap.center = new Vector3(0, 1f, 0); 
                cap.height = 2f;
                cap.radius = 0.5f;
            }
            cap.isTrigger = false;

            // Ensure layer is Default (0) for standard physics interaction
            go.layer = 0;
            foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = 0;
        }
        else if (tag == "Obstacle")
        {
            if (go.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obs = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.carving = true;
            }
        }
    }
}
