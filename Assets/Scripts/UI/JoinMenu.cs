using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class JoinMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel; 
    [SerializeField] private GameObject classSelectionPanel;

    [Header("UI Elements")]
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private Text statusText; 

    private bool keyboardP1Used = false;
    private bool keyboardP2Used = false;

    private PlayerInput currentPlayerSelecting;
    private List<PlayerInput> finalizedPlayers = new List<PlayerInput>();

    private void Awake()
    {
        // Ensure only one JoinMenu exists
        if (FindObjectsByType<JoinMenu>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        UpdateLobbyUI();
    }

    public void JoinKeyboardWASD()
    {
        if (keyboardP1Used) return;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && mouse != null)
        {
            // Note: In local co-op, multiple players can share the same Keyboard device 
            // as long as the Control Scheme names match the ones in your Input Action Asset.
            var player = PlayerInputManager.instance.JoinPlayer(
                playerIndex: finalizedPlayers.Count, 
                controlScheme: "KeyboardMouse", 
                pairWithDevices: new InputDevice[] { keyboard, mouse }
            );

            if (player != null) 
            { 
                keyboardP1Used = true; 
                PrepareClassSelection(player); 
            }
        }
    }

    public void JoinKeyboardIJKL()
    {
        if (keyboardP2Used) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            var player = PlayerInputManager.instance.JoinPlayer(
                playerIndex: finalizedPlayers.Count, 
                controlScheme: "KeyboardIJKL", 
                pairWithDevices: new InputDevice[] { keyboard }
            );

            if (player != null) 
            { 
                keyboardP2Used = true; 
                PrepareClassSelection(player); 
            }
        }
    }

    public void JoinGamepad()
    {
        var usedDevices = new HashSet<InputDevice>();
        foreach (var player in PlayerInput.all) { foreach (var device in player.devices) usedDevices.Add(device); }
        
        var availableGamepad = Gamepad.all.FirstOrDefault(g => !usedDevices.Contains(g));
        if (availableGamepad != null)
        {
            var player = PlayerInputManager.instance.JoinPlayer(
                playerIndex: finalizedPlayers.Count, 
                controlScheme: "Gamepad", 
                pairWithDevices: new InputDevice[] { availableGamepad }
            );
            if (player != null) PrepareClassSelection(player);
        }
        else
        {
            Debug.LogWarning("No unused gamepads found!");
        }
    }

    private void PrepareClassSelection(PlayerInput player)
    {
        currentPlayerSelecting = player;
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(true);
        
        player.DeactivateInput();
        DontDestroyOnLoad(player.gameObject);
    }

    public void SelectClass(CharacterClassSO selectedClass)
    {
        if (currentPlayerSelecting != null)
        {
            var controller = currentPlayerSelecting.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.InitializeClass(selectedClass);
            }
            finalizedPlayers.Add(currentPlayerSelecting);
            currentPlayerSelecting = null;
        }
        
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        UpdateLobbyUI();
    }

    public void StartGame()
    {
        if (finalizedPlayers.Count > 0)
        {
            foreach (var player in finalizedPlayers)
            {
                player.ActivateInput();
                // Crucial: Set the control scheme again to ensure it sticks
                // player.SwitchControlScheme(player.currentControlScheme, player.devices.ToArray());
            }

            if (PlayerInputManager.instance != null) {
                PlayerInputManager.instance.DisableJoining();
                DontDestroyOnLoad(PlayerInputManager.instance.gameObject);
            }
            SceneManager.LoadScene("Level1");
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }

    private void UpdateLobbyUI()
    {
        if (startGameButton != null) {
            var btn = startGameButton.GetComponent<Button>();
            if (btn != null) btn.interactable = finalizedPlayers.Count > 0;
        }
        
        if (statusText != null) {
            string playersStr = "Players Joined:\n";
            foreach (var p in finalizedPlayers) {
                var controller = p.GetComponent<PlayerController>();
                string className = (controller != null && controller.currentClass != null) ? controller.currentClass.className : "No Class";
                playersStr += $"P{p.playerIndex + 1} ({p.currentControlScheme}): {className}\n";
            }
            statusText.text = playersStr;
        }
    }
}
