using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class SplitScreenCamera : MonoBehaviour
{
    private static List<SplitScreenCamera> allCameras = new List<SplitScreenCamera>();
    
    private Camera cam;
    private int playerIndex;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    void OnEnable()
    {
        allCameras.Add(this);
        
        var playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            playerIndex = playerInput.playerIndex;
            cam.depth = playerIndex;
        }

        RefreshAllCameras();
    }

    void OnDisable()
    {
        allCameras.Remove(this);
        RefreshAllCameras();
    }

    public static void RefreshAllCameras()
    {
        foreach (var camScript in allCameras)
        {
            camScript.UpdateViewport();
        }
    }

    private void UpdateViewport()
    {
        if (cam == null) return;

        int totalPlayers = allCameras.Count;

        if (totalPlayers <= 1)
        {
            cam.rect = new Rect(0, 0, 1, 1);
        }
        else if (totalPlayers == 2)
        {
            cam.rect = playerIndex == 0 ? new Rect(0, 0, 0.5f, 1) : new Rect(0.5f, 0, 0.5f, 1);
        }
        else if (totalPlayers == 3)
        {
            if (playerIndex == 0) cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
            else if (playerIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else cam.rect = new Rect(0, 0, 1, 0.5f);
        }
        else
        {
            if (playerIndex == 0) cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
            else if (playerIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else if (playerIndex == 2) cam.rect = new Rect(0, 0, 0.5f, 0.5f);
            else cam.rect = new Rect(0.5f, 0, 0.5f, 0.5f);
        }
        
        Debug.Log($"P{playerIndex} Camera: Rect updated to {cam.rect} (Total Players: {totalPlayers})");
    }
}
