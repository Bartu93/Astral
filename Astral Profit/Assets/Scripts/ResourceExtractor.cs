using UnityEngine;
using System.Collections;

public class ResourceExtractor : MonoBehaviour
{
    [SerializeField] private float extractionTime = 5f;  // Time required to extract the resource
    [SerializeField] private float loadingTime = 2f;     // Time required to load the resource onto a truck
    private bool isResourceReady = false;               // Tracks if a resource is ready for loading
    private TruckController currentTruck = null;        // Reference to the truck being loaded

    private void Start()
    {
        StartCoroutine(ExtractionCycle());
    }

    /// <summary>
    /// Handles the extraction cycle to make resources available periodically.
    /// </summary>
    private IEnumerator ExtractionCycle()
    {
        while (true)
        {
            Debug.Log("Extraction started...");
            yield return new WaitForSeconds(extractionTime);

            isResourceReady = true;
            Debug.Log("Resource ready. Notifying Truck Manager...");
            //NotifyTruckManager();

            // Wait until the resource is loaded before extracting the next resource
            yield return new WaitUntil(() => !isResourceReady);
        }
    }

    private IEnumerator LoadResource()
    {
        isResourceReady = false; // Resource is being loaded
        yield return new WaitForSeconds(loadingTime);

        if (currentTruck != null)
        {
            Debug.Log("Resource loaded onto truck.");
            currentTruck.OnResourceLoaded(5); // Example: Load 5 tonnes of resources
        }

        currentTruck = null; // Reset for the next truck
    }
}
