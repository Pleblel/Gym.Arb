using UnityEngine;

public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Distance / Zoom")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 15f;
    public float zoomSpeed = 5f;

    [Header("Rotation")]
    public float rotationSpeed = 120f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    private float currentX = 0f;
    private float currentY = 20f;

    void Start()
    {
        if (target == null) return;

        Vector3 dir = transform.position - target.position;
        distance = dir.magnitude;

        Quaternion rot = Quaternion.LookRotation(dir);
        currentY = rot.eulerAngles.x;
        currentX = rot.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        if (Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        }

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
