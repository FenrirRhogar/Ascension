using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemGenerator
{
    [MenuItem("Tools/Ascension/Generate All Potions and Artifacts")]
    public static void GenerateAllItems()
    {
        string baseDir = "Assets/DungeonCrawler";
        string potionDir = baseDir + "/Potions";
        string artifactDir = baseDir + "/Artifacts";

        // Create directories if they don't exist
        if (!Directory.Exists(potionDir)) Directory.CreateDirectory(potionDir);
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        Debug.Log("[ItemGenerator] Generating all potion and artifact assets...");

        // ==================== POTIONS ====================
        CreatePotion(potionDir, "Health Potion", ItemRarity.Common, 0f, 50, 0f, 0f, 1f, false, 0f, 0f, 0f, false);
        CreatePotion(potionDir, "Speed Booster", ItemRarity.Uncommon, 10f, 0, 0f, 0f, 1.5f, false, 0f, 0f, 0f, false);
        CreatePotion(potionDir, "Vigor Potion", ItemRarity.Uncommon, 10f, 0, 0f, 0f, 1f, true, 0f, 0f, 0f, false);
        CreatePotion(potionDir, "Rejuvenation Potion", ItemRarity.Uncommon, 10f, 0, 0f, 0f, 1f, false, 8f, 0f, 0f, false);
        CreatePotion(potionDir, "Archmage's Brew", ItemRarity.Rare, 10f, 0, 0f, 0f, 1f, false, 0f, 0f, 0f, true);
        CreatePotion(potionDir, "Ultimate Potion", ItemRarity.Rare, 0f, 0, 0f, 100f, 1f, false, 0f, 0f, 0f, false);

        // ==================== ARTIFACTS ====================
        CreateArtifact(artifactDir, "Amulet of Life", ItemRarity.Common, 0f, 25, 0f, 0f, 0f, 0f);
        CreateArtifact(artifactDir, "Runic Catalyst", ItemRarity.Common, 0f, 0, 0f, 2.0f, 0f, 0f);
        CreateArtifact(artifactDir, "Winged Boots", ItemRarity.Uncommon, 0.8f, 0, 0f, 0f, 0f, 0f);
        CreateArtifact(artifactDir, "Endless Heart", ItemRarity.Uncommon, 0f, 0, 0f, 0f, 4.0f, 0f);
        CreateArtifact(artifactDir, "Sword of Might", ItemRarity.Rare, 0f, 0, 0.15f, 0f, 0f, 0f);
        CreateArtifact(artifactDir, "Reach Emblem", ItemRarity.Legendary, 0f, 0, 0f, 0f, 0f, 1.2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ItemGenerator] Successfully generated all assets! Please assign sprites in the inspector.");
        EditorUtility.DisplayDialog("Item Generator", "Successfully generated all Potions and Artifacts in Assets/DungeonCrawler!\n\nRemember to assign Inventory Icons and link them to your pickup prefabs/LevelManager.", "OK");
    }

    private static string FindAssetPath(string searchDir, string filename)
    {
        if (!Directory.Exists(searchDir)) return null;
        string[] files = Directory.GetFiles(searchDir, filename, SearchOption.AllDirectories);
        if (files.Length > 0)
        {
            return files[0].Replace("\\", "/");
        }
        return null;
    }

    private static void CreatePotion(string path, string name, ItemRarity rarity, float duration, int hp, float mana, float ult, float speed, bool infStam, float hpRegen, float manaRegen, float stamRegen, bool infMana)
    {
        string filename = $"{name.Replace(" ", "").Replace("'", "")}.asset";
        string assetPath = FindAssetPath(path, filename);
        bool isNew = false;

        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = $"{path}/{filename}";
            isNew = true;
        }
        
        PotionSO potion = AssetDatabase.LoadAssetAtPath<PotionSO>(assetPath);
        if (potion == null)
        {
            potion = ScriptableObject.CreateInstance<PotionSO>();
            isNew = true;
        }

        potion.itemName = name;
        potion.rarity = rarity;
        potion.activateOnPickup = false;
        potion.buffDuration = duration;
        potion.healthRestore = hp;
        potion.manaRestore = mana;
        potion.ultimateCharge = ult;
        potion.speedMultiplier = speed;
        potion.grantInfiniteStamina = infStam;
        potion.healthRegenBonus = hpRegen;
        potion.manaRegenBonus = manaRegen;
        potion.staminaRegenBonus = stamRegen;
        potion.grantInfiniteMana = infMana;

        if (isNew)
        {
            AssetDatabase.CreateAsset(potion, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(potion);
        }
    }

    private static void CreateArtifact(string path, string name, ItemRarity rarity, float speed, int hp, float damage, float manaRegen, float staminaRegen, float range)
    {
        string filename = $"{name.Replace(" ", "").Replace("'", "")}.asset";
        string assetPath = FindAssetPath(path, filename);
        bool isNew = false;

        if (string.IsNullOrEmpty(assetPath))
        {
            assetPath = $"{path}/{filename}";
            isNew = true;
        }
        
        ArtifactSO artifact = AssetDatabase.LoadAssetAtPath<ArtifactSO>(assetPath);
        if (artifact == null)
        {
            artifact = ScriptableObject.CreateInstance<ArtifactSO>();
            isNew = true;
        }

        artifact.itemName = name;
        artifact.rarity = rarity;
        artifact.activateOnPickup = true;
        artifact.buffDuration = 0f;
        artifact.speedBonus = speed;
        artifact.healthBonus = hp;
        artifact.damageMultiplierBonus = damage;
        artifact.manaRegenBonus = manaRegen;
        artifact.staminaRegenBonus = staminaRegen;
        artifact.meleeRangeBonus = range;

        if (isNew)
        {
            AssetDatabase.CreateAsset(artifact, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(artifact);
        }
    }
}
