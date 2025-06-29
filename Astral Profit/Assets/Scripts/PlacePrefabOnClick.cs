using UnityEngine;

public class PlacePrefabOnClick : MonoBehaviour
{
    public GameObject prefabToPlace; // Assign the prefab in the inspector
    private Camera mainCamera;
    private GameObject selectedObject;
    private float clickTime;
    private bool isClicking;
    public Vector3 offset;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Start timing the click
            clickTime = 0;
            isClicking = true;
            selectedObject = GetSelectedObject();
        }

        if (Input.GetMouseButton(0))
        {
            if (isClicking)
            {
                clickTime += Time.deltaTime;
                if (clickTime >= 2f)
                {
                    PlacePrefab(selectedObject);
                    isClicking = false;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Reset the click timing
            isClicking = false;
            clickTime = 0;
            selectedObject = null;
        }
    }

    GameObject GetSelectedObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    void PlacePrefab(GameObject selectedObject)
    {
        if (selectedObject != null && prefabToPlace != null)
        {
            // Get the bounds of the selected object
            Bounds bounds = selectedObject.GetComponent<Renderer>().bounds;
            // Calculate the center position
            Vector3 centerPosition = bounds.center;

            // Instantiate the prefab at the center position
            Instantiate(prefabToPlace, centerPosition + offset, Quaternion.identity);
        }
    }
}
