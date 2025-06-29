using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public float movementSpeed = 10f;
    public float movementTime = 10f;
    public float rotationAmount = 1f;
    public float zoomSpeed = 1f;
    public float minZoomSize = 5f;
    public float maxZoomSize = 20f;
    public int maxSidewaysLimit = 50;
    public int minSidewaysLimit = -50;

    private Vector3 newPosition;
    private float newZoom;
    private Vector3 dragStartPosition;

    void Start()
    {
        newPosition = transform.position;
        newZoom = cameraTransform.GetComponent<Camera>().orthographicSize;
    }

    void Update()
    {
        if (!Input.GetKey(KeyCode.LeftShift)) // Toggle movement restriction with Shift key
        {
            HandleMovementInput();
            HandleMouseInput();
        }
    }

    void HandleMouseInput()
    {
        // Zooming with mouse scroll
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom -= Input.mouseScrollDelta.y * zoomSpeed;
            newZoom = Mathf.Clamp(newZoom, minZoomSize, maxZoomSize);
            cameraTransform.GetComponent<Camera>().orthographicSize = newZoom;
        }

        // Dragging
        if (Input.GetMouseButtonDown(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }

        if (Input.GetMouseButton(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                Vector3 dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + (dragStartPosition - dragCurrentPosition);
                newPosition.x = Mathf.Clamp(newPosition.x, minSidewaysLimit, maxSidewaysLimit);
                newPosition.z = Mathf.Clamp(newPosition.z, minSidewaysLimit, maxSidewaysLimit);
            }
        }
    }

    void HandleMovementInput()
    {
        // Movement
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            newPosition += transform.forward * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            newPosition -= transform.forward * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            newPosition += transform.right * movementSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            newPosition -= transform.right * movementSpeed * Time.deltaTime;

        // Rotation
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(Vector3.up, rotationAmount * Time.deltaTime);
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(Vector3.up, -rotationAmount * Time.deltaTime);

        // Update transform position and rotation
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
    }
}
