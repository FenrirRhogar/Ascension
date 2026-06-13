using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class SplitScreenCamera : MonoBehaviour
{
    private Camera cam;
    public int playerIndex { get; private set; }
    private bool subscribedToSceneLoaded = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    void OnEnable()
    {
        UpdatePlayerIndex();
        SubscribeToSceneLoaded();
        RefreshAllCameras();
    }

    void OnDisable()
    {
        // IMPORTANT: Do NOT unsubscribe from sceneLoaded here.
        // If the camera is briefly disabled during a scene transition
        // (e.g. by Unity's internal lifecycle), we lose the callback
        // and can never re-enable ourselves. Instead, we guard the
        // callback with a null check and clean up only in OnDestroy.
        RefreshAllCameras();
    }

    void OnDestroy()
    {
        // Clean up the subscription only when truly destroyed
        if (subscribedToSceneLoaded)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            subscribedToSceneLoaded = false;
        }
    }

    private void SubscribeToSceneLoaded()
    {
        if (!subscribedToSceneLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribedToSceneLoaded = true;
        }
    }

    public void UpdatePlayerIndex()
    {
        var playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            playerIndex = playerInput.playerIndex;
            if (cam != null) cam.depth = playerIndex;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Guard: If this object was destroyed, bail out
        if (this == null) return;

        Debug.Log($"[SplitScreenCamera] OnSceneLoaded: {gameObject.name} for scene '{scene.name}'");

        // Force-enable our own camera immediately
        if (cam != null)
        {
            cam.enabled = true;
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        UpdatePlayerIndex();

        // Delay the full refresh to let all cameras process
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; // Wait one frame
        ForceEnableAllPlayerCameras();
        RefreshAllCameras();
    }

    /// <summary>
    /// Force-enables all player cameras by iterating PlayerInput.all.
    /// This is the MOST RELIABLE way to find DontDestroyOnLoad player cameras
    /// since PlayerInput.all is always kept in sync by Unity's Input System.
    /// </summary>
    public static void ForceEnableAllPlayerCameras()
    {
        Debug.Log($"[SplitScreenCamera] ForceEnableAllPlayerCameras: PlayerInput.all.Count = {PlayerInput.all.Count}");

        foreach (var playerInput in PlayerInput.all)
        {
            if (playerInput == null) continue;

            // Find the camera in children (including inactive)
            Camera playerCam = playerInput.GetComponentInChildren<Camera>(true);
            if (playerCam == null) continue;

            // Only re-enable cameras that belong to a living player
            var health = playerInput.GetComponent<Health>();
            bool isAlive = health == null || health.currentHealth > 0;

            if (isAlive)
            {
                // Ensure the camera GameObject is active
                if (!playerCam.gameObject.activeSelf)
                {
                    playerCam.gameObject.SetActive(true);
                    Debug.Log($"[SplitScreenCamera] Reactivated camera GO: {playerCam.gameObject.name} (Player {playerInput.playerIndex})");
                }

                // Ensure the Camera component is enabled
                if (!playerCam.enabled)
                {
                    playerCam.enabled = true;
                    Debug.Log($"[SplitScreenCamera] Re-enabled camera: {playerCam.gameObject.name} (Player {playerInput.playerIndex})");
                }

                // Ensure the SplitScreenCamera component exists and is configured
                var split = playerCam.GetComponent<SplitScreenCamera>();
                if (split != null)
                {
                    split.cam = playerCam;
                    split.UpdatePlayerIndex();
                    split.SubscribeToSceneLoaded();
                }
            }
        }
    }

    public static void RefreshAllCameras()
    {
        // Build camera list from PlayerInput.all for reliability
        var cameras = new List<SplitScreenCamera>();

        foreach (var playerInput in PlayerInput.all)
        {
            if (playerInput == null) continue;
            var split = playerInput.GetComponentInChildren<SplitScreenCamera>(true);
            if (split != null && split.cam != null && split.cam.enabled && split.gameObject.activeInHierarchy)
            {
                cameras.Add(split);
            }
        }

        cameras = cameras.OrderBy(c => c.playerIndex).ToList();

        Debug.Log($"[SplitScreenCamera] RefreshAllCameras: {cameras.Count} active cameras");

        for (int i = 0; i < cameras.Count; i++)
        {
            cameras[i].UpdateViewport(i, cameras.Count);
        }
    }

    private void UpdateViewport(int effectiveIndex, int totalPlayers)
    {
        if (cam == null) return;

        // --- Audio Listener Management ---
        // The first VISIBLE camera gets the listener
        var listener = GetComponent<AudioListener>();
        if (effectiveIndex == 0 && cam.enabled)
        {
            if (listener == null) listener = gameObject.AddComponent<AudioListener>();
            listener.enabled = true;
        }
        else
        {
            if (listener != null) listener.enabled = false;
        }

        if (!cam.enabled) return;

        if (totalPlayers <= 1)
        {
            cam.rect = new Rect(0, 0, 1, 1);
        }
        else if (totalPlayers == 2)
        {
            cam.rect = effectiveIndex == 0 ? new Rect(0, 0, 0.5f, 1) : new Rect(0.5f, 0, 0.5f, 1);
        }
        else if (totalPlayers == 3)
        {
            if (effectiveIndex == 0) cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
            else if (effectiveIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else cam.rect = new Rect(0, 0, 1, 0.5f);
        }
        else
        {
            if (effectiveIndex == 0) cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
            else if (effectiveIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else if (effectiveIndex == 2) cam.rect = new Rect(0, 0, 0.5f, 0.5f);
            else cam.rect = new Rect(0.5f, 0, 0.5f, 0.5f);
        }
    }
}

