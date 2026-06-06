using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Generation Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] hitTextPrefabs;
    [SerializeField] private GameObject levelGatePrefab;

    [Header("Item Drop Pools")]
    [SerializeField] private GameObject[] potionPrefabs;
    [SerializeField] private GameObject[] artifactPrefabs;
    [SerializeField] [Range(0f, 1f)] private float artifactDropRatio = 0.3f;

    public GameObject[] PotionPrefabs => potionPrefabs;
    public GameObject[] ArtifactPrefabs => artifactPrefabs;
    public float ArtifactDropRatio => artifactDropRatio;

    [Header("Generation Settings")]
    [SerializeField] private Vector2 mapSize = new Vector2(50f, 50f);
    [SerializeField] private int numberOfObstacles = 30;
    [SerializeField] private int numberOfNPCs = 8;
    [SerializeField] private float enemyScale = 0.5f;
    [SerializeField] private LayerMask collisionMask = 1; // Default layer

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float fallThreshold = -20f;

    // Run Progression
    public static int CurrentLevel { get; private set; } = 1;
    public static float runTime { get; private set; } = 0f;
    public static float DifficultyFactor => 1f + (runTime / 120f); // Doubles difficulty every 2 minutes

    public static string GetThreatName()
    {
        if (runTime < 120f) return "EASY";
        if (runTime < 240f) return "NORMAL";
        if (runTime < 360f) return "HARD";
        if (runTime < 480f) return "VERY HARD";
        if (runTime < 600f) return "INSANE";
        return "IMPOSSIBLE";
    }

    public static Color GetThreatColor()
    {
        if (runTime < 120f) return Color.green;
        if (runTime < 240f) return Color.yellow;
        if (runTime < 360f) return new Color(1f, 0.5f, 0f); // Orange
        if (runTime < 480f) return Color.red;
        if (runTime < 600f) return new Color(0.6f, 0f, 0f); // Dark Red
        return new Color(0.5f, 0f, 1f); // Purple
    }

    void Awake()
    {
        Instance = this;
        if (CurrentLevel == 1)
        {
            runTime = 0f;
        }

        if (gameObject.GetComponent<JuiceManager>() == null)
            gameObject.AddComponent<JuiceManager>();
            
        Debug.Log($"[LevelManager] ASCENSION RUN - Level {CurrentLevel} (Threat: {GetThreatName()})");
    }

    public void CompleteLevel()
    {
        CurrentLevel++;
        Debug.Log($"[LevelManager] Level Complete! Ascending to Level {CurrentLevel}...");
        
        // In a full game, you would persist player stats/items here using DontDestroyOnLoad
        // For now, we simply reload the current scene to generate a new, harder mountain.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void RespawnPlayer(GameObject playerGO)
    {
        var playerInput = playerGO.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            int playerIndex = playerInput.playerIndex;
            int spawnIndex = (playerIndex >= 0 && playerIndex < spawnPoints.Length) ? playerIndex : 0;
            
            Vector3 targetPos = spawnPoints[spawnIndex].position;
            Ray ray = new Ray(targetPos + Vector3.up * 40f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 60f))
            {
                targetPos = hit.point;
            }

            TeleportToSpawn(playerInput, targetPos, spawnPoints[spawnIndex].rotation);
            
            var health = playerGO.GetComponent<Health>();
            if (health != null) health.ResetHealth();
        }
    }

    void Start()
    {
        // Show Level Banner
        StartCoroutine(ShowLevelBannerRoutine());

        // 1. Load Custom Mountain Assets if available
        List<GameObject> customObstacles = new List<GameObject>();
        var loadedAssets = Resources.LoadAll<GameObject>("MountainAssets");
        if (loadedAssets != null && loadedAssets.Length > 0)
        {
            foreach (var asset in loadedAssets)
            {
                if (asset.name.Contains("rock") || asset.name.Contains("tree"))
                    customObstacles.Add(asset);
            }
            Debug.Log($"[LevelManager] Auto-loaded {customObstacles.Count} mountain assets from Resources.");
        }
// 2. Generate Procedural Terrain
GameObject ground = new GameObject("ProceduralGround");
ground.transform.position = Vector3.zero;

var terrain = ground.AddComponent<ProceduralTerrain>();
// High resolution for smooth curves
terrain.xSize = 120;
terrain.zSize = 120;
terrain.scale = 1.5f; // Good world size
terrain.heightMultiplier = 20f; // Smoother hills
terrain.noiseScale = 0.015f; // Larger, more sweeping features
terrain.Generate();

// Apply mountain material from Resources
var renderer = ground.GetComponent<MeshRenderer>();
var mountainMat = Resources.Load<Material>("MountainMaterials/mountain_terrain_01");
if (mountainMat != null)
{
    MaterialFixer.FixMaterial(mountainMat); // Fix for URP
    renderer.material = mountainMat;
}
else
{
    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
}

var surface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.BuildNavMesh();

// IMPORTANT: Force Unity to update physics so the new mesh is detectable by raycasts
Physics.SyncTransforms();

var generator = gameObject.AddComponent<ProceduralGenerator>();
// Match the generator size to the actual terrain size (xSize * scale)
float actualWorldSize = 120 * 1.5f; 
generator.mapSize = new Vector2(actualWorldSize * 0.9f, actualWorldSize * 0.9f); // 90% coverage to stay safe

generator.numberOfObstacles = numberOfObstacles * 4; // Fill the larger area
generator.numberOfNPCs = numberOfNPCs * 2;
        generator.obstaclePrefabs = customObstacles.Count > 0 ? customObstacles.ToArray() : obstaclePrefabs;
        generator.npcPrefabs = enemyPrefabs;

        List<GameObject> mapItems = new List<GameObject>();
        if (potionPrefabs != null) mapItems.AddRange(potionPrefabs);
        if (artifactPrefabs != null) mapItems.AddRange(artifactPrefabs);
        generator.consumablePrefabs = mapItems.ToArray();

        generator.hitTextPrefabs = hitTextPrefabs; // Pass hit text prefabs
        generator.levelGatePrefab = levelGatePrefab; // PASS THE GATE PREFAB
        generator.groundObject = ground; // Link the ground object
        generator.enemyScale = enemyScale;
        generator.collisionMask = collisionMask;
        
        generator.RunGeneration();

        // 2. Setup Players
        Invoke(nameof(PositionPlayers), 0.1f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PositionPlayers()
    {
        var players = PlayerInput.all;
        Debug.Log($"[LevelManager] PositionPlayers found {players.Count} players.");

        for (int i = 0; i < players.Count; i++)
        {
            if (i < spawnPoints.Length)
            {
                Vector3 targetPos = spawnPoints[i].position;
                // Raycast to floor to ensure they don't float
                Ray ray = new Ray(targetPos + Vector3.up * 40f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 60f))
                {
                    targetPos = hit.point;
                }

                TeleportToSpawn(players[i], targetPos, spawnPoints[i].rotation);
                Debug.Log($"[LevelManager] Teleported P{i+1} to {targetPos}");
            }
        }
    }

    void Update()
    {
        runTime += Time.deltaTime;

        var players = PlayerInput.all;
        foreach (var p in players)
        {
            if (p.transform.position.y < fallThreshold)
            {
                RespawnPlayer(p.gameObject);
            }
        }
    }

    private void TeleportToSpawn(PlayerInput player, Vector3 floorPosition, Quaternion rotation)
    {
        var charController = player.GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;
        
        // CharacterController pivot is at its center. 
        // A height of 2 means we need to move the pivot 1 unit UP from the floor.
        player.transform.position = floorPosition + new Vector3(0, 1.05f, 0); 
        player.transform.rotation = rotation;
        
        if (charController != null) charController.enabled = true;
    }

    private System.Collections.IEnumerator ShowLevelBannerRoutine()
    {
        // 1. Create a Screen Space Canvas
        GameObject canvasGO = new GameObject("LevelBannerCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Force to absolute top
        
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>(); // Needed for canvas to render properly sometimes

        // 2. Create the Text Object
        GameObject textGO = new GameObject("LevelText");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        TextMeshProUGUI uiText = textGO.AddComponent<TextMeshProUGUI>();
        uiText.text = $"LEVEL {CurrentLevel}";
        
        uiText.fontSize = 120; // Bigger
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.color = new Color(1f, 0.8f, 0.1f, 1f); // Golden yellow
        
        // Prevent clipping if the text is too big for the rect
        uiText.overflowMode = TextOverflowModes.Overflow;
        uiText.textWrappingMode = TextWrappingModes.NoWrap;
        
        // Add a clean outline via TMPro properties
        uiText.outlineWidth = 0.2f;
        uiText.outlineColor = new Color(0f, 0f, 0f, 1f); // Solid black

        // Center perfectly in the upper third of the screen
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800, 200);
        rect.anchoredPosition = new Vector2(0, 300); // 300 pixels above exact center

        // 3. Fade Out Logic
        float displayTime = 3.0f; // Show solid for 3 seconds
        float fadeTime = 2.0f; // Fade out over 2 seconds

        yield return new WaitForSeconds(displayTime);

        float elapsed = 0f;
        Color startColor = uiText.color;
        Color startOutline = uiText.outlineColor;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeTime);
            
            uiText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            uiText.outlineColor = new Color(startOutline.r, startOutline.g, startOutline.b, startOutline.a * alpha);
            
            yield return null;
        }

        // 4. Cleanup
        Destroy(canvasGO);
    }

    public static GameObject GetWeightedRandomItem(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        if (prefabs.Length == 1) return prefabs[0];

        float totalWeight = 0f;
        float[] weights = new float[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            float weight = GetRarityWeight(prefabs[i]);
            weights[i] = weight;
            totalWeight += weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < prefabs.Length; i++)
        {
            randomValue -= weights[i];
            if (randomValue <= 0f)
            {
                return prefabs[i];
            }
        }

        return prefabs[prefabs.Length - 1];
    }

    private static float GetRarityWeight(GameObject prefab)
    {
        if (prefab == null) return 0f;
        PickupItem pickup = prefab.GetComponent<PickupItem>();
        if (pickup == null || pickup.itemData == null) return 100f; // Default weight

        switch (pickup.itemData.rarity)
        {
            case ItemRarity.Common:
                return 100f;
            case ItemRarity.Uncommon:
                return 50f;
            case ItemRarity.Rare:
                return 15f;
            case ItemRarity.Legendary:
                return 3f;
            default:
                return 100f;
        }
    }
}
