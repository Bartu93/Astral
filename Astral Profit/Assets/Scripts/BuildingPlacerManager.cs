using UnityEngine;
using UnityEngine.EventSystems;

// ATTACH THIS TO AN EMPTY GAMEOBJECT IN YOUR SCENE
public class BuildingPlacerManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public LayerMask groundLayer = 1;  // Layer mask for valid placement surfaces
    public LayerMask obstacleLayer = 0;  // Layer mask for obstacles (prevents placement)
    public float previewTransparency = 0.25f;  // Transparency for preview
    public Material previewMaterial;  // Optional custom material for preview
    public Material invalidPlacementMaterial;  // Red material for invalid placement

    [Header("Special Placement Rules")]
    public LayerMask oreLayer;                  // Assign Ore layer here (must match your detector-spawned ores)

    [Header("Input Settings")]
    public KeyCode cancelKey = KeyCode.Escape;  // Key to cancel placement
    public KeyCode confirmKey = KeyCode.Return;  // Alternative key to confirm placement

    // Private variables
    private GameObject currentPreview;  // Current preview instance
    private GameObject prefabToPlace;   // Prefab that will be placed
    private bool isInPlacementMode = false;
    private bool isValidPlacement = false;  // Tracks if current position is valid
    private Camera playerCamera;
    private float placementStartTime;   // Time when placement started
    private float clickDelay = 0.1f;    // Delay to prevent immediate placement

    // Building type detection
    private bool currentBuildingRequiresOre = false;  // Will be set based on building type

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

        Application.targetFrameRate = 120;
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

        // Determine if this building type requires ore
        DetermineBuildingRequirements(buildingPrefab);

        Debug.Log($"Placement mode enabled. isInPlacementMode: {isInPlacementMode}, requiresOre: {currentBuildingRequiresOre}");

        // Create preview instance
        CreatePreview();
    }

    void DetermineBuildingRequirements(GameObject buildingPrefab)
    {
        // Check if this is an extractor by looking for specific components or name patterns
        // Method 1: Check by name (adjust these names to match your prefabs)
        string prefabName = buildingPrefab.name.ToLower();
        if (prefabName.Contains("extractor") || prefabName.Contains("mine") || prefabName.Contains("drill"))
        {
            currentBuildingRequiresOre = true;
            Debug.Log("Building requires ore: " + buildingPrefab.name);
            return;
        }

        // Method 2: Check for specific components (if your extractors have unique scripts)
        // Example: if (buildingPrefab.GetComponent<ExtractorScript>() != null)
        // {
        //     currentBuildingRequiresOre = true;
        //     return;
        // }

        // Method 3: Check by tag (if you've tagged your extractors)
        // if (buildingPrefab.CompareTag("Extractor"))
        // {
        //     currentBuildingRequiresOre = true;
        //     return;
        // }

        // Default: building doesn't require ore (like ore detectors)
        currentBuildingRequiresOre = false;
        Debug.Log("Building does not require ore: " + buildingPrefab.name);
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

            // Make preview transparent (will be updated by UpdatePreviewMaterial)
            UpdatePreviewMaterial(true);

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

        // Disable ALL MonoBehaviour scripts on the preview (except this manager)
        MonoBehaviour[] scripts = currentPreview.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // Skip this manager script
            if (script is BuildingPlacerManager) continue;

            // Disable all other scripts
            script.enabled = false;
        }

        // Disable colliders to prevent interference
        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
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

        // Check both ground and obstacle layers for positioning
        LayerMask combinedLayers = groundLayer | obstacleLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, combinedLayers))
        {
            // Position the preview at the hit point
            currentPreview.transform.position = hit.point;

            // Check if we hit the ground layer specifically
            bool hitGround = ((1 << hit.collider.gameObject.layer) & groundLayer) != 0;

            if (hitGround)
            {
                // We hit valid ground, now check for obstacles at this position
                bool hasObstacle = CheckForObstacles(hit.point);

                if (!hasObstacle)
                {
                    // No obstacles, now check ore requirements
                    if (currentBuildingRequiresOre)
                    {
                        // This is an extractor - check if there's ore underneath
                        bool hasOre = CheckForOreUnderneath(hit.point);
                        isValidPlacement = hasOre;
                    }
                    else
                    {
                        // This is NOT an extractor (like ore detector) - placement is valid
                        isValidPlacement = true;
                    }
                }
                else
                {
                    // Has obstacles - invalid regardless of building type
                    isValidPlacement = false;
                }
            }
            else
            {
                // We hit an obstacle (like water) directly
                isValidPlacement = false;
            }

            // Update material based on validity
            UpdatePreviewMaterial(isValidPlacement);

            // Optionally align to surface normal
            // currentPreview.transform.up = hit.normal;
        }
        else if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
        {
            // Fallback: if we didn't hit the combined layers, try just ground
            currentPreview.transform.position = groundHit.point;

            // Check for obstacles at this position
            bool hasObstacle = CheckForObstacles(groundHit.point);

            if (!hasObstacle)
            {
                // No obstacles, now check ore requirements
                if (currentBuildingRequiresOre)
                {
                    // This is an extractor - check if there's ore underneath
                    bool hasOre = CheckForOreUnderneath(groundHit.point);
                    isValidPlacement = hasOre;
                }
                else
                {
                    // This is NOT an extractor (like ore detector) - placement is valid
                    isValidPlacement = true;
                }
            }
            else
            {
                // Has obstacles - invalid regardless of building type
                isValidPlacement = false;
            }

            UpdatePreviewMaterial(isValidPlacement);
        }
        else
        {
            // Hide preview if not over any valid surface
            currentPreview.transform.position = new Vector3(0, -1000, 0);
            isValidPlacement = false;
        }
    }

    bool CheckForObstacles(Vector3 position)
    {
        // Get the bounds of the preview object to check for overlaps
        Bounds previewBounds = GetPreviewBounds();

        // Check for obstacles using OverlapBox
        Collider[] obstacles = Physics.OverlapBox(
            position + previewBounds.center,
            previewBounds.extents,
            currentPreview.transform.rotation,
            obstacleLayer
        );

        return obstacles.Length > 0;
    }

    bool CheckForOreUnderneath(Vector3 position)
    {
        // Get the bounds of the preview object to check for overlaps
        Bounds previewBounds = GetPreviewBounds();

        // Check for ore using OverlapBox
        Collider[] ores = Physics.OverlapBox(
            position + previewBounds.center,
            previewBounds.extents * 0.5f,  // adjust multiplier if too strict/lenient
            currentPreview.transform.rotation,
            oreLayer
        );

        bool hasOre = ores.Length > 0;

        // Debug logging to help troubleshoot
        if (currentBuildingRequiresOre)
        {
            Debug.Log($"Checking for ore at {position}, found {ores.Length} ore colliders. HasOre: {hasOre}");
        }

        return hasOre;
    }

    Bounds GetPreviewBounds()
    {
        if (currentPreview == null) return new Bounds();

        // Get combined bounds of all renderers in the preview
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // Convert to local space relative to the preview object
        combinedBounds.center -= currentPreview.transform.position;

        return combinedBounds;
    }

    void UpdatePreviewMaterial(bool isValid)
    {
        if (currentPreview == null) return;

        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material newMat;

                if (!isValid && invalidPlacementMaterial != null)
                {
                    // Use red material for invalid placement
                    newMat = new Material(invalidPlacementMaterial);
                }
                else if (previewMaterial != null)
                {
                    // Use custom preview material
                    newMat = new Material(previewMaterial);
                }
                else
                {
                    // Use original material with transparency
                    newMat = new Material(materials[i]);

                    // Set rendering mode to transparent
                    newMat.SetFloat("_Mode", 3); // Transparent mode
                    newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMat.SetInt("_ZWrite", 0);
                    newMat.DisableKeyword("_ALPHATEST_ON");
                    newMat.EnableKeyword("_ALPHABLEND_ON");
                    newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMat.renderQueue = 3000;
                }

                // Set transparency and color
                Color color = newMat.color;
                if (!isValid)
                {
                    // Red color for invalid placement
                    color = Color.red;
                    color.a = previewTransparency;
                }
                else
                {
                    // Green color for valid placement
                    color = Color.green;
                    color.a = previewTransparency;
                }
                newMat.color = color;

                materials[i] = newMat;
            }

            renderer.materials = materials;
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

        // Base checks (not off-screen and valid placement flag is true)
        bool positionValid = currentPreview.transform.position.y > -100;
        return positionValid && isValidPlacement;
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
        // Enable ALL MonoBehaviour scripts on the placed building
        MonoBehaviour[] scripts = building.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
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
        isValidPlacement = false;  // Reset validity flag
        currentBuildingRequiresOre = false;  // Reset ore requirement

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