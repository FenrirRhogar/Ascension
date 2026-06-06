using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private Coroutine currentShake;

    public void Shake(float duration, float magnitude)
    {
        if (currentShake != null) StopCoroutine(currentShake);
        currentShake = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime; // Use unscaled so it works during hit-stop
            yield return null;
        }

        transform.localPosition = originalPos;
        currentShake = null;
    }
}
