using UnityEngine;

public class MenuCameraOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Vector3 centerPoint = Vector3.zero;
    public float distance = 10f;
    public float height = 3f;
    public float rotationSpeed = 5f;
    
    [Header("Look Settings")]
    public float lookDamping = 2f;
    public Vector3 lookOffset = new Vector3(0, 1.5f, 0);

    private float currentAngle;

    void Start()
    {
        // Set initial position based on current angle
        UpdatePosition();
    }

    void LateUpdate()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        UpdatePosition();
        
        // Smoothly look at the center
        Quaternion targetRotation = Quaternion.LookRotation((centerPoint + lookOffset) - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookDamping * Time.deltaTime);
    }

    private void UpdatePosition()
    {
        float x = centerPoint.x + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * distance;
        float z = centerPoint.z + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * distance;
        transform.position = new Vector3(x, centerPoint.y + height, z);
    }
}
