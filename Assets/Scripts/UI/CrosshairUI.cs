using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private Texture2D crosshairTexture;
    [SerializeField] private float size = 20f;
    [SerializeField] private Color color = Color.white;

    private void OnGUI()
    {
        // For split-screen, each camera has its own viewport.
        // OnGUI renders per camera if we use the right rect.
        // However, standard OnGUI is screen-space.
        // A better way for split-screen is a Canvas, but let's use a simple approach.
        
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null || !cam.enabled) return;

        // Calculate center based on camera pixel rect (handles split-screen)
        Rect pixelRect = cam.pixelRect;
        float centerX = pixelRect.x + pixelRect.width / 2;
        float centerY = (Screen.height - pixelRect.y) - pixelRect.height / 2;

        Rect drawRect = new Rect(centerX - size / 2, centerY - size / 2, size, size);

        if (crosshairTexture != null)
        {
            GUI.color = color;
            GUI.DrawTexture(drawRect, crosshairTexture);
        }
        else
        {
            // Draw a simple plus sign if no texture
            DrawLine(new Rect(centerX - size / 2, centerY, size, 2), color);
            DrawLine(new Rect(centerX, centerY - size / 2, 2, size), color);
        }
    }

    private void DrawLine(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
}
