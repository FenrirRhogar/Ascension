using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextFileToUI : MonoBehaviour
{
    [Tooltip("Drag the .txt file (like CREDITS.txt or CONTROLS.txt) here.")]
    public TextAsset textAsset;

    private void Start()
    {
        LoadText();
    }

    // Call this if you ever change the text file during runtime
    public void LoadText()
    {
        if (textAsset != null)
        {
            var textUI = GetComponent<TextMeshProUGUI>();
            if (textUI != null)
            {
                textUI.text = textAsset.text;
            }
        }
        else
        {
            Debug.LogWarning($"[TextFileToUI] No TextAsset assigned to {gameObject.name}");
        }
    }
}
