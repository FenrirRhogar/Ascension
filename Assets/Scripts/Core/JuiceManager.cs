using UnityEngine;
using System.Collections;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void HitStop(float duration)
    {
        StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    public void ShakeCamera(Camera cam, float duration, float magnitude)
    {
        if (cam == null) return;
        var shaker = cam.GetComponent<CameraShake>();
        if (shaker == null) shaker = cam.gameObject.AddComponent<CameraShake>();
        shaker.Shake(duration, magnitude);
    }
}
