using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

public abstract class ConsumableSO : ScriptableObject
{
    [Header("Item Metadata")]
    public string itemName;
    public Sprite inventoryIcon;
    
    public bool activateOnPickup; 
    public float buffDuration = 10f; // 0 means instant effect

    [Header("Rarity Settings")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("SFX")]
    public AudioClip pickupSFX;
    public AudioClip useSFX;

    // Passes the PlayerController to mutate the specific player's state
    public abstract void UseItem(PlayerController player);
}
