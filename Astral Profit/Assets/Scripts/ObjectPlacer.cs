using UnityEngine;

public class RaycastTest : MonoBehaviour
{
    public LayerMask groundLayer;   // Layer for ground detection
    public GameObject redSpherePrefab;  // Prefab for the red sphere to spawn

    void Update()
    {
        // When left mouse button is pressed, shoot the ray
        if (Input.GetMouseButtonDown(0))
        {
            ShootRay();
        }
    }

    void ShootRay()
    {
        // Get the mouse position in screen space
        Vector3 mousePos = Input.mousePosition;
        Debug.Log(mousePos);

        // Convert mouse position to a ray from the camera
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Raycast to check if it hits the ground layer
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, groundLayer))
        {
            // Debug the hit position
            Debug.Log("Ray hit position: " + hitInfo.point);

            // Instantiate a small red sphere at the hit point
            Instantiate(redSpherePrefab, hitInfo.point, Quaternion.identity);
        }
        else
        {
            Debug.Log("No ground hit.");
        }
    }
}
