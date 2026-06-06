using UnityEngine;
using UnityEditor;
using System.IO;

public class ArtifactOrganizer
{
    [MenuItem("Tools/Ascension/Organize and Link Artifacts")]
    public static void OrganizeArtifacts()
    {
        string artifactsBaseDir = "Assets/DungeonCrawler/Artifacts";
        string gemsPrefabsDir = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs";

        // Map artifacts to their source gem prefabs
        var mappings = new (string assetName, string folderName, string prefabName)[]
        {
            ("AmuletofLife", "Amulet of Life", "HeartGem"),
            ("RunicCatalyst", "Runic Catalyst", "SphereGem"),
            ("WingedBoots", "Winged Boots", "SpeedChevYellow"),
            ("EndlessHeart", "Endless Heart", "Heart"),
            ("SwordofMight", "Sword of Might", "Ruby"),
            ("ReachEmblem", "Reach Emblem", "Star")
        };

        foreach (var mapping in mappings)
        {
            string targetFolder = $"{artifactsBaseDir}/{mapping.folderName}";
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            // Move the SO asset
            string sourceAssetPath = $"{artifactsBaseDir}/{mapping.assetName}.asset";
            string destAssetPath = $"{targetFolder}/{mapping.assetName}.asset";

            if (File.Exists(sourceAssetPath) && !File.Exists(destAssetPath))
            {
                string error = AssetDatabase.MoveAsset(sourceAssetPath, destAssetPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[Organizer] Failed to move asset: {sourceAssetPath} to {destAssetPath}. Error: {error}");
                }
            }

            // Copy the prefab
            string sourcePrefabPath = $"{gemsPrefabsDir}/{mapping.prefabName}.prefab";
            string destPrefabPath = $"{targetFolder}/{mapping.prefabName}.prefab";

            if (File.Exists(sourcePrefabPath))
            {
                if (File.Exists(destPrefabPath))
                {
                    AssetDatabase.DeleteAsset(destPrefabPath);
                }
                AssetDatabase.CopyAsset(sourcePrefabPath, destPrefabPath);
            }
            else
            {
                Debug.LogError($"[Organizer] Source gem prefab not found: {sourcePrefabPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Now link them
        foreach (var mapping in mappings)
        {
            string targetFolder = $"{artifactsBaseDir}/{mapping.folderName}";
            string destAssetPath = $"{targetFolder}/{mapping.assetName}.asset";
            string destPrefabPath = $"{targetFolder}/{mapping.prefabName}.prefab";

            ArtifactSO artifactSO = AssetDatabase.LoadAssetAtPath<ArtifactSO>(destAssetPath);
            GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(destPrefabPath);

            if (artifactSO != null && prefabGO != null)
            {
                // Load prefab contents, edit, and save
                string localPath = AssetDatabase.GetAssetPath(prefabGO);
                GameObject root = PrefabUtility.LoadPrefabContents(localPath);

                // Set a reasonable default scale (e.g. 0.4f) for these gem models
                root.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                PickupItem pickup = root.GetComponent<PickupItem>();
                if (pickup == null)
                {
                    pickup = root.AddComponent<PickupItem>();
                }
                pickup.itemData = artifactSO;

                PrefabUtility.SaveAsPrefabAsset(root, localPath);
                PrefabUtility.UnloadPrefabContents(root);

                Debug.Log($"[Organizer] Linked prefab {mapping.prefabName} with SO {mapping.assetName}");
            }
            else
            {
                Debug.LogWarning($"[Organizer] Could not load SO or Prefab for {mapping.folderName}: SO={artifactSO != null}, Prefab={prefabGO != null}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Artifact Organizer", "Successfully organized and linked all artifacts!\n\nCheck Assets/DungeonCrawler/Artifacts/ for details.", "OK");
    }
}
