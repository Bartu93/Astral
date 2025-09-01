using UnityEngine.EventSystems;
using UnityEngine;

public class BuildingPlacerManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public LayerMask groundLayer = 1;  // Layer mask for valid placement surfaces
    public float previewTransparency = 0.25f;  // Transparency for preview
    public Material previewMaterial;  // Optional custom material for preview

    [Header("Input Settings")]
    public KeyCode cancelKey = KeyCode.Escape;  // Key to cancel placement
    public KeyCode confirmKey = KeyCode.Return;  // Alternative key to confirm placement

    private GameObject currentPreview;  // Current preview instance
    private GameObject prefabToPlace;   // Prefab that will be placed
    private bool isInPlacementMode = false;
    private Camera playerCamera;
    private float placementStartTime;   // Time when placement started
    private float clickDelay = 0.1f;    // Delay to prevent immediate placement

    // Events for other systems to hook into
    public System.Action<GameObject> OnBuildingPlaced;
    public System.Action OnPlacementCancelled;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }

    void Update()
    {
        if (isInPlacementMode)
        {
            // Debug.Log("In placement mode - updating preview"); // Uncomment for detailed logging
            UpdatePreviewPosition();
            HandleInput();
        }
    }

    public void StartBuildingPlacement(GameObject buildingPrefab)
    {
        Debug.Log($"StartBuildingPlacement called with: {(buildingPrefab != null ? buildingPrefab.name : "null")}");

        if (buildingPrefab == null)
        {
            Debug.LogError("Building prefab is null in StartBuildingPlacement!");
            return;
        }

        // Cancel any existing placement
        CancelPlacement();

        // Set up new placement
        prefabToPlace = buildingPrefab;
        isInPlacementMode = true;
        placementStartTime = Time.time;  // Record when placement started

        Debug.Log($"Placement mode enabled. isInPlacementMode: {isInPlacementMode}");

        // Create preview instance
        CreatePreview();
    }

    void CreatePreview()
    {
        Debug.Log($"CreatePreview called. prefabToPlace: {(prefabToPlace != null ? prefabToPlace.name : "null")}");

        if (prefabToPlace != null)
        {
            currentPreview = Instantiate(prefabToPlace);
            Debug.Log($"Preview created: {currentPreview.name}");

            // Disable any scripts that shouldn't run on preview
            DisablePreviewScripts();

            // Make preview transparent
            MakePreviewTransparent();

            // Position preview off-screen initially
            currentPreview.transform.position = new Vector3(0, -1000, 0);
            Debug.Log("Preview positioned off-screen initially");
        }
        else
        {
            Debug.LogError("Cannot create preview - prefabToPlace is null!");
        }
    }

    void DisablePreviewScripts()
    {
        if (currentPreview == null) return;

        // Disable common scripts that shouldn't run on preview
        MonoBehaviour[] scripts = currentPreview.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // Skip this manager script
            if (script is BuildingPlacerManager) continue;

            // Disable scripts like UpdateGrid, ResourceExtractor, etc.
            if (script.GetType().Name == "UpdateGrid" ||
                script.GetType().Name == "ResourceExtractor" ||
                script.GetType().Name == "BuildingLogic")
            {
                script.enabled = false;
            }
        }

        // Disable colliders to prevent interference
        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    void MakePreviewTransparent()
    {
        if (currentPreview == null) return;

        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                // Create a new material instance to avoid affecting the original
                Material newMat = new Material(materials[i]);

                // Set rendering mode to transparent
                if (previewMaterial != null)
                {
                    newMat = new Material(previewMaterial);
                }
                else
                {
                    // Standard transparency setup
                    newMat.SetFloat("_Mode", 3); // Transparent mode
                    newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMat.SetInt("_ZWrite", 0);
                    newMat.DisableKeyword("_ALPHATEST_ON");
                    newMat.EnableKeyword("_ALPHABLEND_ON");
                    newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMat.renderQueue = 3000;
                }

                // Set transparency
                Color color = newMat.color;
                color.a = previewTransparency;
                newMat.color = color;

                materials[i] = newMat;
            }

            renderer.materials = materials;
        }
    }

    void UpdatePreviewPosition()
    {
        if (currentPreview == null || playerCamera == null)
        {
            if (currentPreview == null) Debug.Log("UpdatePreviewPosition: currentPreview is null");
            if (playerCamera == null) Debug.Log("UpdatePreviewPosition: playerCamera is null");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Position the preview at the hit point
            currentPreview.transform.position = hit.point;
            // Debug.Log($"Preview positioned at: {hit.point}"); // Uncomment for detailed position logging

            // Optionally align to surface normal
            // currentPreview.transform.up = hit.normal;
        }
        else
        {
            // Hide preview if not over valid ground
            currentPreview.transform.position = new Vector3(0, -1000, 0);
            // Debug.Log("No valid ground hit - hiding preview"); // Uncomment for detailed logging
        }
    }

    void HandleInput()
    {
        // Cancel placement
        if (Input.GetKeyDown(cancelKey))
        {
            CancelPlacement();
            return;
        }

        // Check if mouse is over UI before processing clicks
        bool isOverUI = EventSystem.current.IsPointerOverGameObject();

        // Check if enough time has passed since placement started (prevents immediate placement)
        bool canPlace = Time.time - placementStartTime > clickDelay;

        // Confirm placement with left click or confirm key (but not if over UI or too soon)
        if ((Input.GetMouseButtonDown(0) && !isOverUI && canPlace) || Input.GetKeyDown(confirmKey))
        {
            if (IsValidPlacementPosition())
            {
                PlaceBuilding();
            }
        }

        // Optional: Right click to cancel (also check UI)
        if (Input.GetMouseButtonDown(1) && !isOverUI)
        {
            CancelPlacement();
        }
    }

    bool IsValidPlacementPosition()
    {
        if (currentPreview == null) return false;

        // Check if preview is at a valid position (not hidden)
        return currentPreview.transform.position.y > -100;
    }

    void PlaceBuilding()
    {
        if (currentPreview == null || prefabToPlace == null) return;

        Vector3 placementPosition = currentPreview.transform.position;
        Quaternion placementRotation = currentPreview.transform.rotation;

        // Instantiate the actual building
        GameObject newBuilding = Instantiate(prefabToPlace, placementPosition, placementRotation);

        // Enable scripts that should run on placed building
        EnableBuildingScripts(newBuilding);

        // Trigger event
        OnBuildingPlaced?.Invoke(newBuilding);

        // Clean up
        CancelPlacement();

        Debug.Log($"Building placed at {placementPosition}");
    }

    void EnableBuildingScripts(GameObject building)
    {
        // Enable scripts that were disabled in preview
        MonoBehaviour[] scripts = building.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script.GetType().Name == "UpdateGrid" ||
                script.GetType().Name == "ResourceExtractor" ||
                script.GetType().Name == "BuildingLogic")
            {
                script.enabled = true;
            }
        }

        // Enable colliders
        Collider[] colliders = building.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    public void CancelPlacement()
    {
        isInPlacementMode = false;

        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        prefabToPlace = null;

        // Trigger event
        OnPlacementCancelled?.Invoke();

        Debug.Log("Building placement cancelled");
    }

    // Public getters
    public bool IsInPlacementMode()
    {
        return isInPlacementMode;
    }

    public GameObject GetCurrentPreview()
    {
        return currentPreview;
    }

    public GameObject GetPrefabToPlace()
    {
        return prefabToPlace;
    }
}