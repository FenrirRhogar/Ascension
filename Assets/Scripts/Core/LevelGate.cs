using UnityEngine;

public class LevelGate : MonoBehaviour
{
    private bool isActivated = false;

    void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;

        // Check if a player touched the gate
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isActivated = true;
            Debug.Log("[LevelGate] Player reached the gate! Proceeding to next level...");
            
            // Tell the LevelManager to load the next stage
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CompleteLevel();
            }
        }
    }
}
