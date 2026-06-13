using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("1. Main Prefabs")]
    public GameObject[] enemyPrefabs;
    public GameObject[] hitTextPrefabs;
    public GameObject levelGatePrefab;
    public GameObject reviveTombstonePrefab;

    [Header("2. Biome Assets")]
    public GameObject[] forestProps;
    public GameObject[] volcanoProps;
    public GameObject[] frozenProps;
    public GameObject[] desertProps;
    public GameObject[] autumnProps;

    [Header("3. Loot Pools")]
    public GameObject[] potionPrefabs;
    public GameObject[] artifactPrefabs;
    [Range(0f, 1f)] public float globalPotionDropRatio = 1.0f;
    [Range(0f, 1f)] public float globalArtifactDropRatio = 0.5f;

    [Header("4. Generation Settings")]
    public Vector2 mapSize = new Vector2(120f, 120f);
    public int obstacleDensity = 100;
    public int npcDensity = 15;
    public LayerMask collisionMask = 1;

    [Header("5. Spawn Settings")]
    public Transform[] spawnPoints;
    public float fallThreshold = -100f;
    public AudioClip victorySFX;
    public AudioClip levelMusic;

    // Public Properties
    public GameObject[] PotionPrefabs => potionPrefabs;
    public GameObject[] ArtifactPrefabs => artifactPrefabs;
    public float ArtifactDropRatio => globalArtifactDropRatio;

    // Run Stats
    public static int CurrentLevel { get; private set; } = 1;
    public static float runTime { get; private set; } = 0f;

    private List<GameObject> activePlayers = new List<GameObject>();
    private bool isComplete = false;
    private bool playersPositioned = false;

    void Awake()
    {
        Instance = this;
        if (CurrentLevel == 1) runTime = 0f;
        if (GetComponent<JuiceManager>() == null) gameObject.AddComponent<JuiceManager>();
    }

    void Start()
    {
        Debug.Log($"[LevelManager] Start. PlayerInput.all.Count = {PlayerInput.all.Count}");

        // 0. FIRST: Disable any non-player cameras in the scene (the Level1 "Main Camera")
        // This MUST happen before enabling player cameras to avoid conflicts
        DisableSceneCameras();

        // 1. Force-enable all player cameras using PlayerInput.all
        SplitScreenCamera.ForceEnableAllPlayerCameras();
        SplitScreenCamera.RefreshAllCameras();

        // Atmosphere Setup
        if (GetComponent<AtmosphereManager>() == null) gameObject.AddComponent<AtmosphereManager>();
        AtmosphereManager.Instance.ApplyAtmosphere(GetCurrentBiome());

        // 2. Show Banner
        StartCoroutine(ShowLevelBannerRoutine());

        // 3. Setup Terrain
        SetupTerrain();

        // 4. Position Players
        Invoke(nameof(PositionPlayers), 0.2f);
        
        // 5. Multi-stage camera refresh to guarantee cameras survive any lifecycle issues
        StartCoroutine(MultiStageCameraRefresh());
        
        // Track initial players
        activePlayers.Clear();
        foreach (var p in PlayerInput.all) activePlayers.Add(p.gameObject);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (SoundManager.Instance != null && levelMusic != null)
        {
            SoundManager.Instance.PlayMusic(levelMusic);
        }
    }

    /// <summary>
    /// Disables all cameras in the scene that are NOT part of a player.
    /// Also disables their AudioListeners to prevent audio conflicts.
    /// </summary>
    private void DisableSceneCameras()
    {
        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (var c in allCams)
        {
            // Check if this camera belongs to any player
            if (c.GetComponentInParent<PlayerInput>() != null) continue;

            Debug.Log($"[LevelManager] Deactivating Scene Camera: {c.gameObject.name}");
            
            // Disable AudioListener to prevent "more than 1 AudioListener" warnings
            var listener = c.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;

            c.enabled = false;
            c.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Runs multiple camera refresh passes over several frames.
    /// This ensures cameras are properly enabled even if Unity's lifecycle
    /// delays some operations (e.g., DontDestroyOnLoad migration, scene loading).
    /// </summary>
    private IEnumerator MultiStageCameraRefresh()
    {
        // Stage 1: End of current frame
        yield return new WaitForEndOfFrame();
        SplitScreenCamera.ForceEnableAllPlayerCameras();
        DisableSceneCameras();
        SplitScreenCamera.RefreshAllCameras();

        // Stage 2: Next frame
        yield return null;
        SplitScreenCamera.ForceEnableAllPlayerCameras();
        DisableSceneCameras();
        SplitScreenCamera.RefreshAllCameras();

        // Stage 3: Two more frames later (final safety net)
        yield return null;
        yield return null;
        SplitScreenCamera.ForceEnableAllPlayerCameras();
        DisableSceneCameras();
        SplitScreenCamera.RefreshAllCameras();

        Debug.Log($"[LevelManager] MultiStageCameraRefresh complete. Active players: {PlayerInput.all.Count}");
    }



    private void SetupTerrain()
    {
        BiomeType currentBiome = GetCurrentBiome();
        
        // Create the ground object
        GameObject ground = new GameObject("ProceduralGround");
        ground.transform.position = Vector3.zero;

        // Configure the Terrain generator
        var terrain = ground.AddComponent<ProceduralTerrain>();
        terrain.currentBiome = currentBiome;
        terrain.xSize = 100;
        terrain.zSize = 100;
        terrain.scale = mapSize.x / 100f;
        terrain.heightMultiplier = 20f;
        terrain.noiseScale = 0.02f;
        terrain.Generate();

        // Material setup
        var renderer = ground.GetComponent<MeshRenderer>();
        Shader terrainShader = Shader.Find("Custom/URPTerrainSplat");
        if (terrainShader == null) terrainShader = Shader.Find("Universal Render Pipeline/Lit");
        
        Material terrainMat = new Material(terrainShader);
        renderer.material = terrainMat;

        // Load and assign textures based on biome
        Texture2D groundTex = null;
        Texture2D rockTex = Resources.Load<Texture2D>("TerrainTextures/Rock");
        Texture2D peakTex = Resources.Load<Texture2D>("TerrainTextures/Snow");

        switch (currentBiome)
        {
            case BiomeType.Volcanic:
                groundTex = Resources.Load<Texture2D>("TerrainTextures/BlackSand");
                peakTex = Resources.Load<Texture2D>("TerrainTextures/Muddy");
                break;
            case BiomeType.Frozen:
                groundTex = Resources.Load<Texture2D>("TerrainTextures/Snow");
                break;
            case BiomeType.Desert:
                groundTex = Resources.Load<Texture2D>("TerrainTextures/Sand");
                peakTex = Resources.Load<Texture2D>("TerrainTextures/Sand");
                break;
            case BiomeType.Autumn:
                groundTex = Resources.Load<Texture2D>("TerrainTextures/Muddy");
                break;
            default: // Forest
                groundTex = Resources.Load<Texture2D>("TerrainTextures/Grass");
                break;
        }

        if (groundTex != null) terrainMat.SetTexture("_Layer1", groundTex);
        if (rockTex != null) terrainMat.SetTexture("_Layer2", rockTex);
        if (peakTex != null) terrainMat.SetTexture("_Layer3", peakTex);
        
        terrainMat.SetFloat("_Tiling", 12.0f);

        // Build NavMesh for AI
        var surface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        surface.BuildNavMesh();

        Physics.SyncTransforms();

        // 3. Run Generator
        ProceduralGenerator gen = GetComponent<ProceduralGenerator>();
        if (gen == null) gen = gameObject.AddComponent<ProceduralGenerator>();

        // Select assets based on biome
        GameObject[] props = currentBiome switch
        {
            BiomeType.Volcanic => volcanoProps,
            BiomeType.Frozen => frozenProps,
            BiomeType.Desert => desertProps,
            BiomeType.Autumn => autumnProps,
            _ => forestProps
        };

        gen.obstaclePrefabs = props;
        gen.npcPrefabs = enemyPrefabs;
        gen.hitTextPrefabs = hitTextPrefabs;
        gen.levelGatePrefab = levelGatePrefab;
        gen.collisionMask = collisionMask;
        gen.groundObject = ground;
        
        // Loot
        List<GameObject> loot = new List<GameObject>();
        if (potionPrefabs != null) loot.AddRange(potionPrefabs);
        if (artifactPrefabs != null) loot.AddRange(artifactPrefabs);
        gen.consumablePrefabs = loot.ToArray();

        // Standard Spawning
        gen.RunGeneration();
    }

    public static BiomeType GetCurrentBiome()
    {
        int biomeCount = System.Enum.GetValues(typeof(BiomeType)).Length;
        return (BiomeType)((CurrentLevel - 1) % biomeCount);
    }

    public static string GetThreatName() { return runTime < 240f ? "NORMAL" : "HARD"; }
    public static Color GetThreatColor() { return runTime < 240f ? Color.green : Color.red; }

    void Update()
    {
        runTime += Time.deltaTime;

        if (!playersPositioned) return;

        foreach (var p in PlayerInput.all)
        {
            if (p == null) continue;

            // If player falls below threshold, they DIE instead of respawning
            var health = p.GetComponent<Health>();
            if (health != null && health.currentHealth > 0)
            {
                if (p.transform.position.y < fallThreshold) 
                {
                    health.TakeDamage(health.MaxHealth + 100); 
                }
            }
        }
    }

    public void OnPlayerDeath(GameObject playerGO)
    {
        // 0. Disable camera ONLY if others are still alive (prevents "No Camera" warning)
        bool anyoneElseAlive = false;
        foreach (var p in PlayerInput.all)
        {
            if (p.gameObject == playerGO) continue;
            var h = p.GetComponent<Health>();
            if (h != null && h.currentHealth > 0) { anyoneElseAlive = true; break; }
        }

        if (anyoneElseAlive)
        {
            var cam = playerGO.GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;
            SplitScreenCamera.RefreshAllCameras();
        }

        // 1. Spawn Tombstone
        if (reviveTombstonePrefab != null)
        {
            GameObject tomb = Instantiate(reviveTombstonePrefab, playerGO.transform.position, Quaternion.identity);
            var script = tomb.GetComponent<ReviveTombstone>();
            if (script == null) script = tomb.AddComponent<ReviveTombstone>();
            script.Setup(playerGO.GetComponent<PlayerController>());
        }

        // Check for Game Over
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        bool anyoneAlive = false;
        foreach (var p in PlayerInput.all)
        {
            var h = p.GetComponent<Health>();
            if (h != null && h.currentHealth > 0)
            {
                anyoneAlive = true;
                break;
            }
        }

        if (!anyoneAlive)
        {
            Debug.Log("GAME OVER: All players died.");
            StartCoroutine(GameOverRoutine());
        }
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("GameOverCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 1. Black Background
        GameObject bgGO = new GameObject("BlackBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0); // Start transparent
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        
        // 2. Game Over Text
        GameObject textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(canvasGO.transform, false);

        // Stretch text to fill center of screen
        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.4f);
        rt.anchorMax = new Vector2(0.9f, 0.6f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var uiText = textGO.AddComponent<TextMeshProUGUI>();
        uiText.text = "GAME OVER\n<size=50%>YOUR JOURNEY ENDS HERE</size>";
        uiText.fontSize = 100;
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.color = new Color(1, 0, 0, 0); // Start transparent red
        uiText.fontStyle = FontStyles.Bold;

        // 3. Fade In Effect
        float elapsed = 0;
        float fadeTime = 2.5f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / fadeTime;
            bgImage.color = new Color(0, 0, 0, alpha);
            uiText.color = new Color(1, 0, 0, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(4f);

        // Reset Run and Return to the starting scene
        CurrentLevel = 1;
        runTime = 0f;
        SceneManager.LoadScene("SampleScene"); 
    }

    public void RevivePlayer(GameObject playerGO, Vector3 position)
    {
        var controller = playerGO.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        // Re-enable camera FIRST so RefreshAllCameras sees it
        var cam = playerGO.GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;
        SplitScreenCamera.RefreshAllCameras();

        playerGO.transform.position = position + Vector3.up * 1f;
        
        Health h = playerGO.GetComponent<Health>();
        if (h != null) h.ResetHealth();

        ResourceSystem rs = playerGO.GetComponent<ResourceSystem>();
        if (rs != null) rs.ResetResources();
        
        if (controller != null) controller.enabled = true;
        Debug.Log($"[Revive] {playerGO.name} is back in the fight!");
    }

    public void CompleteLevel()
    {
        if (CurrentLevel >= 5)
        {
            // VICTORY STATE
            StartCoroutine(ShowVictoryBannerRoutine());
            return;
        }

        // Before loading next level, ensure everyone is revived, cameras are ON, and stats are FULL
        foreach (var p in PlayerInput.all)
        {
            var h = p.GetComponent<Health>();
            if (h != null) h.ResetHealth();

            var rs = p.GetComponent<ResourceSystem>();
            if (rs != null) rs.ResetResources();

            var cam = p.GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = true;
        }

        CurrentLevel++;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private System.Collections.IEnumerator ShowVictoryBannerRoutine()
    {
        // Hide crosshair and lock movement (optional, but good for end state)
        foreach (var p in PlayerInput.all)
        {
            var controller = p.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
        }

        GameObject canvasGO = new GameObject("VictoryBannerCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject textGO = new GameObject("VictoryText");
        textGO.transform.SetParent(canvasGO.transform, false);

        // Stretch text to fill most of the screen to prevent word cutting
        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI uiText = textGO.AddComponent<TextMeshProUGUI>();

        if (SoundManager.Instance != null && victorySFX != null)
            SoundManager.Instance.PlaySound(victorySFX);

        uiText.text = $"VICTORY\n<size=50%>THE ASCENSION IS COMPLETE\nTime: {Mathf.FloorToInt(runTime / 60)}m {Mathf.FloorToInt(runTime % 60)}s</size>";
        uiText.fontSize = 120; 
        uiText.enableWordWrapping = true;
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.color = new Color(0.4f, 1f, 0.4f); // Victory Green
        uiText.fontStyle = FontStyles.Bold;
        
        yield return new WaitForSeconds(6f);
        
        // Reset Run
        CurrentLevel = 1;
        runTime = 0f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void RespawnPlayer(GameObject playerGO)
    {
        var input = playerGO.GetComponent<PlayerInput>();
        if (input == null) return;

        // Restore Camera in case it was disabled
        var cam = playerGO.GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = true;
        SplitScreenCamera.RefreshAllCameras();

        // Restore Stats
        Health h = playerGO.GetComponent<Health>();
        if (h != null) h.ResetHealth();
        
        ResourceSystem rs = playerGO.GetComponent<ResourceSystem>();
        if (rs != null) rs.ResetResources();

        int effectiveIndex = -1;
        var allPlayers = PlayerInput.all;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i] == input)
            {
                effectiveIndex = i;
                break;
            }
        }
        
        if (effectiveIndex == -1) effectiveIndex = input.playerIndex;
        int idx = Mathf.Clamp(effectiveIndex, 0, spawnPoints.Length - 1);
        
        Vector3 safePos = GetSafeSpawnPosition(idx);
        TeleportToSpawn(input, safePos, spawnPoints[idx].rotation);
    }

    private void PositionPlayers()
    {
        var players = PlayerInput.all;
        for (int i = 0; i < players.Count; i++)
        {
            RespawnPlayer(players[i].gameObject);
        }
        playersPositioned = true;
    }

    private Vector3 GetSafeSpawnPosition(int index)
    {
        if (index < 0 || index >= spawnPoints.Length) return Vector3.zero;

        Vector3 pos = spawnPoints[index].position;
        // Project onto ground
        Ray ray = new Ray(new Vector3(pos.x, 500f, pos.z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 800f, collisionMask))
        {
            return hit.point;
        }
        return pos; // Fallback to raw transform if no ground found
    }

    private void TeleportToSpawn(PlayerInput player, Vector3 pos, Quaternion rot)
    {
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = pos + Vector3.up * 1.5f;
        player.transform.rotation = rot;
        if (controller != null) controller.enabled = true;
    }

    private string GetCoolLevelName()
    {
        string[] names = {
            "The Whispering Slopes",
            "Echoes of the Peak",
            "Frostbite Pass",
            "Obsidian Valley",
            "The Gilded Ascent",
            "Shadow of the Summit",
            "Ancient Skyway",
            "The Eternal Climb",
            "Ruinous Heights",
            "Celestial Plateau"
        };
        
        int nameIndex = (CurrentLevel - 1) % names.Length;
        return names[nameIndex];
    }

    private System.Collections.IEnumerator ShowLevelBannerRoutine()
    {
        GameObject canvasGO = new GameObject("LevelBannerCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject textGO = new GameObject("LevelText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI uiText = textGO.AddComponent<TextMeshProUGUI>();
        
        uiText.text = $"LEVEL {CurrentLevel}\n<size=65%>{GetCoolLevelName().ToUpper()}</size>";
        uiText.fontSize = 40; // Smaller font size
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.color = new Color(1f, 0.85f, 0.4f); // Softer gold color
        uiText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        
        yield return new WaitForSeconds(3.5f);
        if (canvasGO != null) Destroy(canvasGO);
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
