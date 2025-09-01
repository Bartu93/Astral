using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Script for individual building buttons
public class BuildingButton : MonoBehaviour
{
    [Header("Building Settings")]
    public GameObject buildingPrefab;  // The prefab to spawn
    public string buildingName = "Building";  // Optional name for the building

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
        placerManager.StartBuildingPlacement(buildingPrefab);
    }
}