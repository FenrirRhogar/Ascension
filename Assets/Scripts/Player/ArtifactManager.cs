using UnityEngine;
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    private PlayerController player;
    private Health health;

    public List<ArtifactSO> collectedArtifacts { get; private set; } = new List<ArtifactSO>();

    void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<Health>();
    }

    public void AddArtifact(ArtifactSO artifact)
    {
        if (artifact == null) return;

        collectedArtifacts.Add(artifact);
        Debug.Log($"[ArtifactManager] {player.name} collected artifact: {artifact.itemName}");

        // Apply permanent buffs to player stats
        if (player.currentClass != null)
        {
            if (artifact.speedBonus > 0f)
            {
                player.currentClass.baseMovementSpeed += artifact.speedBonus;
            }
            if (artifact.manaRegenBonus > 0f)
            {
                player.currentClass.manaRegenRate += artifact.manaRegenBonus;
            }
            if (artifact.staminaRegenBonus > 0f)
            {
                player.currentClass.staminaRegenRate += artifact.staminaRegenBonus;
            }
        }

        if (artifact.healthBonus > 0 && health != null)
        {
            health.IncreaseMaxHealth(artifact.healthBonus);
        }

        if (artifact.damageMultiplierBonus > 0f)
        {
            player.damageMultiplier += artifact.damageMultiplierBonus;
        }

        if (artifact.meleeRangeBonus > 0f)
        {
            player.meleeRangeBonus += artifact.meleeRangeBonus;
        }

        // Trigger UI update or notify PlayerHUD
        var hud = player.GetComponent<PlayerHUD>();
        if (hud != null)
        {
            hud.RefreshArtifactDisplay();
        }
    }

    public void ConvertAndAddPotionAsArtifact(ConsumableSO potion)
    {
        if (potion == null) return;

        // Create a runtime ArtifactSO in memory
        ArtifactSO artifact = ScriptableObject.CreateInstance<ArtifactSO>();
        artifact.inventoryIcon = potion.inventoryIcon;
        artifact.activateOnPickup = true;

        if (potion is PotionSO genericPotion)
        {
            // Map standard generic potions to interesting artifact designs and stats
            if (genericPotion.healthRestore > 0)
            {
                artifact.itemName = "Amulet of Life";
                artifact.healthBonus = 25; // +25 Max Health permanently
                Debug.Log($"[Artifact] Converted Health Potion into Amulet of Life artifact for {player.name}.");
            }
            else if (genericPotion.manaRestore > 0)
            {
                artifact.itemName = "Runic Catalyst";
                artifact.manaRegenBonus = 2f; // +2.0 Mana/sec permanently
                Debug.Log($"[Artifact] Converted Mana Potion into Runic Catalyst artifact for {player.name}.");
            }
            else if (genericPotion.speedMultiplier > 1f)
            {
                artifact.itemName = "Winged Boots";
                artifact.speedBonus = 0.8f; // +0.8 Move Speed permanently
                Debug.Log($"[Artifact] Converted Speed Potion into Winged Boots artifact for {player.name}.");
            }
            else if (genericPotion.grantInfiniteStamina)
            {
                artifact.itemName = "Endless Heart";
                artifact.staminaRegenBonus = 4f; // +4.0 Stamina/sec permanently
                Debug.Log($"[Artifact] Converted Stamina Potion into Endless Heart artifact for {player.name}.");
            }
            else if (genericPotion.ultimateCharge > 0)
            {
                artifact.itemName = "Sword of Might";
                artifact.damageMultiplierBonus = 0.15f; // +15% Damage permanently
                Debug.Log($"[Artifact] Converted Ultimate Potion into Sword of Might artifact for {player.name}.");
            }
            else
            {
                artifact.itemName = potion.itemName + " Artifact";
                artifact.damageMultiplierBonus = 0.05f; // +5% Damage
            }
        }
        else
        {
            // Fallback for legacy subclasses based on asset naming patterns
            string pName = potion.name.ToLower();
            if (pName.Contains("health"))
            {
                artifact.itemName = "Amulet of Life";
                artifact.healthBonus = 25;
            }
            else if (pName.Contains("mana"))
            {
                artifact.itemName = "Runic Catalyst";
                artifact.manaRegenBonus = 2f;
            }
            else if (pName.Contains("speed"))
            {
                artifact.itemName = "Winged Boots";
                artifact.speedBonus = 0.8f;
            }
            else if (pName.Contains("stamina") || pName.Contains("vigor"))
            {
                artifact.itemName = "Endless Heart";
                artifact.staminaRegenBonus = 4f;
            }
            else
            {
                artifact.itemName = potion.itemName + " Artifact";
                artifact.damageMultiplierBonus = 0.05f;
            }
        }

        AddArtifact(artifact);
    }

    public static ArtifactSO CreateRandomArtifact(Sprite baseIcon)
    {
        ArtifactSO artifact = ScriptableObject.CreateInstance<ArtifactSO>();
        artifact.inventoryIcon = baseIcon;
        artifact.activateOnPickup = true;

        int rand = Random.Range(0, 6);
        switch (rand)
        {
            case 0:
                artifact.itemName = "Amulet of Life";
                artifact.healthBonus = 25;
                break;
            case 1:
                artifact.itemName = "Runic Catalyst";
                artifact.manaRegenBonus = 2.0f;
                break;
            case 2:
                artifact.itemName = "Winged Boots";
                artifact.speedBonus = 0.8f;
                break;
            case 3:
                artifact.itemName = "Endless Heart";
                artifact.staminaRegenBonus = 4.0f;
                break;
            case 4:
                artifact.itemName = "Sword of Might";
                artifact.damageMultiplierBonus = 0.15f;
                break;
            case 5:
            default:
                artifact.itemName = "Reach Emblem";
                artifact.meleeRangeBonus = 1.2f;
                break;
        }

        return artifact;
    }
}
