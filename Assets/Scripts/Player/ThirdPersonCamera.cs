using UnityEngine;

public class ThirdPersonCameraOrbit : MonoBehaviour
{
    [Header("Target")]
    public Transform target;      // Player

    [Header("Camera child")]
    public Transform cam;         // Main Camera (hija)

    [Header("Framing")]
    public float distance = 3f;
    public float height = 0.5f;
    public float shoulderOffset = 0f;

    [Header("Look")]
    public float sensX = 120f;
    public float sensY = 80f;
    public float minPitch = -20f;
    public float maxPitch = 70f;

    float yaw, pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.transform;
        }

        var ang = transform.eulerAngles;
        yaw = ang.y;
        pitch = ang.x;
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        float dx = Input.GetAxis("Mouse X");
        float dy = Input.GetAxis("Mouse Y");

        yaw += dx * sensX * Time.deltaTime;
        pitch -= dy * sensY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.position = target.position;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        cam.localPosition = new Vector3(shoulderOffset, height, -distance);
        cam.localRotation = Quaternion.identity;
    }
}