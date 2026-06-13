using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    private PlayerController playerController;
    private ResourceSystem resourceSystem;
    
    // Fixed array of 3 slots
    public ConsumableSO[] slots = new ConsumableSO[3] { null, null, null };
    
    public PickupItem targetedItem { get; private set; }
    public float pickupRange = 5.0f;

    public float PotionCooldownRemaining => 0f;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        resourceSystem = GetComponent<ResourceSystem>();
    }

    public void OnPickUp(InputValue value)
    {
        if (value.isPressed && targetedItem != null)
        {
            // Verify distance
            if (Vector3.Distance(transform.position, targetedItem.transform.position) > pickupRange)
            {
                Debug.Log("[Inventory] Item is too far away to pick up.");
                return;
            }

            if (targetedItem.itemData == null)
            {
                Debug.LogError($"[Inventory] Cannot pick up {targetedItem.gameObject.name} because its 'Item Data' field is missing a ConsumableSO!");
                return;
            }

            // 1. If it is an artifact (or marked as activateOnPickup), use and register it immediately
            if (targetedItem.itemData is ArtifactSO || targetedItem.itemData.activateOnPickup)
            {
                ArtifactManager am = GetComponent<ArtifactManager>();
                if (am == null) am = gameObject.AddComponent<ArtifactManager>();

                if (SoundManager.Instance != null && targetedItem.itemData.pickupSFX != null)
                    SoundManager.Instance.PlaySound(targetedItem.itemData.pickupSFX);

                targetedItem.itemData.UseItem(playerController);

                Destroy(targetedItem.gameObject);
                targetedItem = null;
                return;
            }

            // 2. Otherwise, treat it as a temporary consumable potion and put it in the inventory slots
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = targetedItem.itemData;
                    string itemName = slots[i].itemName; 

                    if (SoundManager.Instance != null && targetedItem.itemData.pickupSFX != null)
                        SoundManager.Instance.PlaySound(targetedItem.itemData.pickupSFX);

                    Destroy(targetedItem.gameObject);
                    targetedItem = null;
                    Debug.Log($"[Inventory] Picked up {itemName} into Slot {i+1}");
                    return;
                }
            }
            Debug.Log("[Inventory] Inventory Full!");
        }
    }

    public void SetTargetedItem(PickupItem item)
    {
        targetedItem = item;
    }

    // --- Usage Logic ---
    public void OnSlot1(InputValue value) { if (value.isPressed) UseItemInSlot(0); }
    public void OnSlot2(InputValue value) { if (value.isPressed) UseItemInSlot(1); }
    public void OnSlot3(InputValue value) { if (value.isPressed) UseItemInSlot(2); }

    private void UseItemInSlot(int index)
    {
        if (index < slots.Length && slots[index] != null)
        {
            Debug.Log($"[Inventory] Using {slots[index].itemName} from Slot {index+1}");
            
            if (SoundManager.Instance != null && slots[index].useSFX != null)
                SoundManager.Instance.PlaySound(slots[index].useSFX);

            slots[index].UseItem(playerController);
            slots[index] = null; // Clear the slot
        }
    }
}
