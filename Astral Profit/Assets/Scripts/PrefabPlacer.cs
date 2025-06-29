using UnityEngine;

public class PrefabPlacer : MonoBehaviour
{
    public GameObject refineryPrefab;   // Prefab for the refinery
    public GameObject extractorPrefab;  // Prefab for the extractor
    private GameObject currentInstance; // Current instance being placed
    private bool isPlacingRefinery = false;  // Tracks if refinery placement mode is active
    private bool isPlacingExtractor = false; // Tracks if extractor placement mode is active
    public LayerMask groundLayer;       // Layer mask for ground
    public LayerMask resourceLayer;     // Layer mask for resources

    void Update()
    {
        // Toggle refinery placement mode with "B"
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!isPlacingRefinery) // Activating refinery mode
            {
                EnablePlacementMode(ref isPlacingRefinery, refineryPrefab);
            }
            else // Deactivating refinery mode
            {
                DisablePlacementMode();
            }
        }

        // Toggle extractor placement mode with "V"
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (!isPlacingExtractor) // Activating extractor mode
            {
                EnablePlacementMode(ref isPlacingExtractor, extractorPrefab);
            }
            else // Deactivating extractor mode
            {
                DisablePlacementMode();
            }
        }

        // Update placement for refinery or extractor
        if (isPlacingRefinery && currentInstance != null)
        {
            UpdatePrefabPositionOnGround();
            if (Input.GetMouseButtonDown(0) && IsOverExactGround())
            {
                PlacePrefab(isRefinery: true);
            }
        }
        else if (isPlacingExtractor && currentInstance != null)
        {
            UpdatePrefabPositionOnResource();
            if (Input.GetMouseButtonDown(0) && IsOverValidResource())
            {
                PlacePrefab(isRefinery: false);
            }
        }
    }

    void EnablePlacementMode(ref bool isSpecificPlacing, GameObject prefab)
    {
        // Disable any current placement mode
        DisablePlacementMode();

        // Enable the specific mode and create the prefab instance
        isSpecificPlacing = true;
        currentInstance = Instantiate(prefab);
    }

    void DisablePlacementMode()
    {
        // Reset both modes and destroy the current instance
        isPlacingRefinery = false;
        isPlacingExtractor = false;
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
    }

    void UpdatePrefabPositionOnGround()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Only update position if the hit point's y-coordinate is exactly 0.25
            if (Mathf.Approximately(hit.point.y, 0.25f))
            {
                currentInstance.transform.position = hit.point;
            }
        }
    }

    void UpdatePrefabPositionOnResource()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, resourceLayer))
        {
            // Update position if hit object is tagged "Resource" and has a sphere collider trigger
            if (hit.collider.CompareTag("Resource") && hit.collider is SphereCollider && hit.collider.isTrigger)
            {
                currentInstance.transform.position = new Vector3(hit.point.x, 0.25f, hit.point.z);
            }
            else
            {
                // Hide the extractor if it's not over a valid resource
                currentInstance.transform.position = new Vector3(0, -1000, 0); // Position far below ground to hide it
            }
        }
    }

    bool IsOverExactGround()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Check if the hit position's y-coordinate is exactly 0.25
            return Mathf.Approximately(hit.point.y, 0.25f);
        }
        return false;
    }

    bool IsOverValidResource()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, resourceLayer))
        {
            // Check if hit object is tagged "Resource" and has a sphere collider trigger
            return hit.collider.CompareTag("Resource") && hit.collider is SphereCollider && hit.collider.isTrigger;
        }
        return false;
    }

    void PlacePrefab(bool isRefinery)
    {
        // Place the current instance and reset placement mode
        Instantiate(currentInstance, currentInstance.transform.position, Quaternion.identity);

        if (isRefinery)
        {
            currentInstance.GetComponent<UpdateGrid>().enabled = true;
        }
        else
        {
            currentInstance.GetComponent<UpdateGrid>().enabled = true;
            currentInstance.GetComponent<ResourceExtractor>().enabled = true;
        }

        DisablePlacementMode();
    }
}
