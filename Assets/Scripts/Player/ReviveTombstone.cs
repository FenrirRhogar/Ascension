using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ReviveTombstone : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 30f;
    public float reviveTimeRequired = 7f;
    public float checkRadius = 4f;

    [Header("Visuals")]
    public MeshRenderer progressCircle;
    public Material progressMaterial;
    public GameObject reviveVFXPrefab;

    [Header("UI Bars")]
    public Image lifetimeFill;
    public Image reviveFill;
    public GameObject uiCanvas;

    private PlayerController deadPlayer;
    private float currentReviveProgress = 0f;
    private float currentLifetime;
    private bool isRevived = false;
    private GameObject activeVFX;

    public void Setup(PlayerController player)
    {
        deadPlayer = player;
        currentLifetime = lifetime;
        
        if (reviveVFXPrefab != null)
        {
            activeVFX = Instantiate(reviveVFXPrefab, transform.position, Quaternion.identity, transform);
            activeVFX.transform.localScale = Vector3.one * 0.5f;
        }

        StartCoroutine(TombstoneLifetimeRoutine());
    }

    private void Update()
    {
        if (isRevived) return;

        // Auto-cleanup if the player has been respawned by other systems (falling, next level, etc.)
        if (deadPlayer != null)
        {
            var health = deadPlayer.GetComponent<Health>();
            if (health != null && health.currentHealth > 0)
            {
                Destroy(gameObject);
                return;
            }
        }

        // --- UI Billboarding ---
        if (uiCanvas != null && Camera.main != null)
        {
            uiCanvas.transform.LookAt(uiCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Check for nearby living players
        bool someoneReviving = false;
        Collider[] nearby = Physics.OverlapSphere(transform.position, checkRadius);
        
        foreach (var col in nearby)
        {
            var pc = col.GetComponent<PlayerController>();
            if (pc != null && pc != deadPlayer)
            {
                // Check if they have health (are alive)
                var health = pc.GetComponent<Health>();
                if (health != null && health.currentHealth > 0)
                {
                    someoneReviving = true;
                    break;
                }
            }
        }

        if (someoneReviving)
        {
            currentReviveProgress += Time.deltaTime;
            if (currentReviveProgress >= reviveTimeRequired)
            {
                PerformRevive();
            }
        }
        else
        {
            // Slowly decay progress if no one is standing there
            currentReviveProgress = Mathf.Max(0, currentReviveProgress - Time.deltaTime * 0.5f);
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float revivePercent = currentReviveProgress / reviveTimeRequired;
        float lifetimePercent = currentLifetime / lifetime;

        // UI Bars
        if (lifetimeFill != null) lifetimeFill.fillAmount = lifetimePercent;
        if (reviveFill != null) reviveFill.fillAmount = revivePercent;

        // Scale logic: Start at 0.75m radius and grow to 2.5m radius
        float targetScale = Mathf.Lerp(1.5f, 5.0f, revivePercent); 

        if (progressCircle != null)
        {
            // Use localScale for the progress indicator
            progressCircle.transform.localScale = new Vector3(targetScale, 0.1f, targetScale);
            
            Color c = Color.Lerp(Color.red, Color.green, revivePercent);
            if (progressMaterial != null) progressCircle.material.color = c;
            else progressCircle.GetComponent<Renderer>().material.color = c;
        }

        if (activeVFX != null)
        {
            // Keep VFX scale synced but slightly smaller than the circle
            float vfxScale = targetScale * 0.8f;
            activeVFX.transform.localScale = new Vector3(vfxScale, vfxScale, vfxScale);
        }
    }

    private void PerformRevive()
    {
        isRevived = true;
        if (LevelManager.Instance != null && deadPlayer != null)
        {
            LevelManager.Instance.RevivePlayer(deadPlayer.gameObject, transform.position);
        }
        Destroy(gameObject);
    }

    private IEnumerator TombstoneLifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        if (!isRevived)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
