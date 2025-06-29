using UnityEngine;
using Pathfinding;

public class TruckController : MonoBehaviour
{
    private AIDestinationSetter destinationSetter;

    [SerializeField] private int maxCapacity = 5; // Maximum capacity in tonnes
    private int currentLoad = 0;                 // Current load in tonnes
    private bool isLoaded = false;              // Whether the truck is loaded or not

    private void Awake()
    {
        destinationSetter = GetComponent<AIDestinationSetter>();
    }

    /// <summary>
    /// Sets the destination for the truck.
    /// </summary>
    /// <param name="target">The target transform to move to.</param>
    public void SetDestination(Transform target)
    {
        if (destinationSetter != null)
        {
            destinationSetter.target = target;
            Debug.Log($"Truck heading towards {target.name}");
        }
        else
        {
            Debug.LogWarning("AIDestinationSetter component is missing!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle arrival at extractors for loading
        if (other.CompareTag("Extractor") && !isLoaded)
        {
            var extractor = other.GetComponent<ResourceExtractor>();
            if (extractor != null)
            {
                Debug.Log("Truck arrived at extractor. Waiting for loading...");
                //extractor.LoadTruck(this); // Extractor handles the loading process
            }
        }

        // Handle arrival at refineries for unloading
        if (other.CompareTag("Refinery") && isLoaded)
        {
            Debug.Log("Truck arrived at refinery. Unloading...");
            UnloadAtRefinery();
        }
    }

    /// <summary>
    /// Called by the extractor to load resources onto the truck.
    /// </summary>
    /// <param name="loadAmount">Amount of resource to load.</param>
    public void OnResourceLoaded(int loadAmount)
    {
        currentLoad = Mathf.Min(loadAmount, maxCapacity); // Load up to the truck's capacity
        isLoaded = true;
        Debug.Log($"Truck loaded with {currentLoad} tonnes.");

        // After loading, find the nearest refinery
        GameObject nearestRefinery = FindClosestRefinery();
        if (nearestRefinery != null)
        {
            SetDestination(nearestRefinery.transform);
        }
        else
        {
            Debug.LogWarning("No refineries found for unloading!");
        }
    }

    /// <summary>
    /// Unloads resources at the refinery.
    /// </summary>
    private void UnloadAtRefinery()
    {
        Debug.Log($"Unloaded {currentLoad} tonnes at refinery.");
        currentLoad = 0;
        isLoaded = false;

        // After unloading, notify the Truck Manager
        //TruckManager.Instance.AddTruck(this);
    }

    /// <summary>
    /// Finds the closest refinery to the truck.
    /// </summary>
    /// <returns>The nearest refinery GameObject.</returns>
    private GameObject FindClosestRefinery()
    {
        GameObject[] refineries = GameObject.FindGameObjectsWithTag("Refinery");
        GameObject closest = null;
        float minDistance = Mathf.Infinity;

        foreach (var refinery in refineries)
        {
            float distance = Vector3.Distance(transform.position, refinery.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = refinery;
            }
        }

        return closest;
    }
}
