using UnityEngine;
using UnityEngine.UI;

// Script for individual building buttons - ATTACH THIS TO YOUR UI BUTTONS
public class BuildingButton : MonoBehaviour
{
    [Header("Building Settings")]
    public GameObject buildingPrefab;  // The prefab to spawn

    [Header("Optional Settings")]
    public string buildingName = "";  // Optional name for display/debugging

    private Button button;
    private BuildingPlacerManager placerManager;

    void Start()
    {
        button = GetComponent<Button>();
        placerManager = FindObjectOfType<BuildingPlacerManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        if (placerManager == null)
        {
            Debug.LogError("BuildingPlacerManager not found in scene!");
        }

        // Set building name from prefab if not manually set
        if (string.IsNullOrEmpty(buildingName) && buildingPrefab != null)
        {
            buildingName = buildingPrefab.name;
        }
    }

    void OnButtonClick()
    {
        Debug.Log($"Button clicked! Building: {buildingName}");

        if (placerManager == null)
        {
            Debug.LogError("BuildingPlacerManager is null!");
            return;
        }

        if (buildingPrefab == null)
        {
            Debug.LogError("Building prefab is null!");
            return;
        }

        Debug.Log($"Starting placement for: {buildingPrefab.name}");

        // Simply start the placement - the manager will automatically detect requirements
        placerManager.StartBuildingPlacement(buildingPrefab);
    }

    void OnDestroy()
    {
        // Clean up the button listener when the object is destroyed
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}