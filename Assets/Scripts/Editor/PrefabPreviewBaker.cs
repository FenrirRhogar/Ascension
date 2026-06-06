using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabPreviewBaker : EditorWindow
{
    private struct BakeJob
    {
        public GameObject prefab;
        public ConsumableSO itemData;
        public string assetPath;
    }

    private List<BakeJob> jobs = new List<BakeJob>();
    private bool isBaking = false;
    private int currentJobIndex = 0;
    private int waitFrames = 0;

    // Adjustable settings to fix darkness
    private float brightnessMultiplier = 1.3f;
    private bool forceLinearToGamma = true;

    [MenuItem("Tools/Ascension/Bake Prefab Icons as Sprites")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPreviewBaker>("Prefab Preview Baker");
    }

    private void OnEnable()
    {
        // Automatically default to true if project is configured for Linear Color Space
        forceLinearToGamma = (PlayerSettings.colorSpace == ColorSpace.Linear);
    }

    private void OnGUI()
    {
        GUILayout.Label("Bake Prefab Previews to Transparent Sprites", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This tool scans all project prefabs with a PickupItem component, " +
                        "renders a high-quality editor preview, saves it as a UI sprite, " +
                        "and links it to the item's ScriptableObject.", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(15);

        // Render Settings Fields
        GUILayout.Label("Bake Color Settings", EditorStyles.boldLabel);
        brightnessMultiplier = EditorGUILayout.Slider("Brightness Boost", brightnessMultiplier, 0.5f, 3.0f);
        forceLinearToGamma = EditorGUILayout.Toggle("Linear To Gamma Correction", forceLinearToGamma);
        
        GUILayout.Space(15);

        if (GUILayout.Button("Find and Bake All Item Icons", GUILayout.Height(35)))
        {
            StartBaking();
        }

        if (isBaking && jobs.Count > 0)
        {
            float progress = (float)currentJobIndex / jobs.Count;
            Rect r = EditorGUILayout.GetControlRect(true, 20);
            EditorGUI.ProgressBar(r, progress, $"Baking {currentJobIndex + 1}/{jobs.Count}: {jobs[currentJobIndex].prefab.name}");
        }
    }

    private void StartBaking()
    {
        jobs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            PickupItem pickup = prefab.GetComponent<PickupItem>();
            if (pickup != null && pickup.itemData != null)
            {
                jobs.Add(new BakeJob { prefab = prefab, itemData = pickup.itemData, assetPath = path });
            }
        }

        if (jobs.Count > 0)
        {
            isBaking = true;
            currentJobIndex = 0;
            waitFrames = 0;
            // Warm up previews
            foreach (var job in jobs)
            {
                AssetPreview.GetAssetPreview(job.prefab);
            }
            Debug.Log($"[Baker] Started baking session for {jobs.Count} item prefabs.");
        }
        else
        {
            EditorUtility.DisplayDialog("Baker", "No prefabs found with a PickupItem component referencing a ScriptableObject!", "OK");
        }
    }

    private void Update()
    {
        if (!isBaking) return;

        if (currentJobIndex >= jobs.Count)
        {
            isBaking = false;
            AssetDatabase.Refresh();
            // Wait one tick post-refresh to assign the newly generated assets
            EditorApplication.delayCall += AssignSprites;
            return;
        }

        var job = jobs[currentJobIndex];
        Texture2D preview = AssetPreview.GetAssetPreview(job.prefab);

        if (preview == null)
        {
            // Preview is still loading asynchronously, request again and wait
            AssetPreview.GetAssetPreview(job.prefab);
            waitFrames++;
            if (waitFrames > 300) // Skip if taking too long to load
            {
                Debug.LogWarning($"[Baker] Skipping preview bake for {job.prefab.name} (Preview render timed out)");
                currentJobIndex++;
                waitFrames = 0;
            }
            Repaint();
            return;
        }

        // Save preview to disk
        SavePreviewAsPNG(job.prefab, job.itemData, preview);
        currentJobIndex++;
        waitFrames = 0;
        Repaint();
    }

    private void SavePreviewAsPNG(GameObject prefab, ConsumableSO itemData, Texture2D preview)
    {
        string dir = "Assets/DungeonCrawler/Sprites";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string destPath = $"{dir}/{prefab.name}_Icon.png";
        
        // Render target setup to copy pixels from non-readable thumbnail texture
        Texture2D readableTexture = new Texture2D(preview.width, preview.height, TextureFormat.RGBA32, false);
        RenderTexture tmp = RenderTexture.GetTemporary(
            preview.width,
            preview.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
            
        Graphics.Blit(preview, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;
        
        readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);

        // Correct colors
        Color[] pixels = readableTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            
            // 1. Correct linear space darkness to gamma space if enabled
            if (forceLinearToGamma)
            {
                c.r = Mathf.LinearToGammaSpace(c.r);
                c.g = Mathf.LinearToGammaSpace(c.g);
                c.b = Mathf.LinearToGammaSpace(c.b);
            }

            // 2. Apply customized brightness boost
            c.r = Mathf.Clamp01(c.r * brightnessMultiplier);
            c.g = Mathf.Clamp01(c.g * brightnessMultiplier);
            c.b = Mathf.Clamp01(c.b * brightnessMultiplier);

            pixels[i] = c;
        }
        readableTexture.SetPixels(pixels);
        readableTexture.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        byte[] bytes = readableTexture.EncodeToPNG();
        File.WriteAllBytes(destPath, bytes);
        DestroyImmediate(readableTexture);

        AssetDatabase.ImportAsset(destPath);
        
        // Configure import settings as Sprite
        TextureImporter importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        
        Debug.Log($"[Baker] Saved icon: {destPath}");
    }

    private void AssignSprites()
    {
        int assignedCount = 0;
        foreach (var job in jobs)
        {
            string spritePath = $"Assets/DungeonCrawler/Sprites/{job.prefab.name}_Icon.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                job.itemData.inventoryIcon = sprite;
                EditorUtility.SetDirty(job.itemData);
                assignedCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Baker] Finished. Assigned {assignedCount} sprites to ScriptableObjects.");
        EditorUtility.DisplayDialog("Baker Complete", $"Successfully baked and assigned previews for {assignedCount} items!\n\nSprites saved at Assets/DungeonCrawler/Sprites/", "OK");
    }
}
