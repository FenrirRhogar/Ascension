using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public ConsumableSO itemData;

    private void Start()
    {
        // Ensure it has a trigger collider
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        gameObject.tag = "Consumable";
    }
}
