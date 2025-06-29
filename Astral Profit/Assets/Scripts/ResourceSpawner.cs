using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceSpawner : MonoBehaviour
{
    [SerializeField] private GameObject resourcePrefab;
    [SerializeField] private int resourcesToPlace = 10;
    [SerializeField] private float spawnRadius = 5f; // Distance to check for nearby resources
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

   

    public void PlaceResources()
    {
        int resourcesPlaced = 0;

        while (resourcesPlaced < resourcesToPlace)
        {
            Vector3 randomPosition = GetRandomPosition();
            if (CanPlaceResource(randomPosition))
            {
                GameObject resource = Instantiate(resourcePrefab, randomPosition, Quaternion.identity);
                resource.transform.parent = this.transform; // Set the resource as a child of ResourceSpawner
                resourcesPlaced++;
            }
        }
    }

    Vector3 GetRandomPosition()
    {
        float x = Random.Range(0, 100); // Adjust this range to fit your map bounds
        float z = Random.Range(0, 100); // Adjust this range to fit your map bounds
        return new Vector3(x, 0.25f, z);
    }

    bool CanPlaceResource(Vector3 position)
    {
        // Perform an OverlapSphere to check for nearby resources
        Collider[] hitColliders = Physics.OverlapSphere(position, spawnRadius);
        foreach (Collider hit in hitColliders)
        {
            // If a resource prefab is within range, we don't place here
            if (hit.gameObject.CompareTag("Resource")) return false;
        }

        // Cast a ray downward to check if it's a valid ground spot
        Ray ray = new Ray(position + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 20f))
        {
            if (((1 << hitInfo.collider.gameObject.layer) & groundLayer) != 0)
            {
                // Additional check: make sure no obstacles in the way of resource
                return CheckForObstacles(position);
            }
        }
        Debug.Log("Not a valid placement if not on ground layer");
        return false; // Not a valid placement if not on ground layer
    }

    bool CheckForObstacles(Vector3 position)
    {
        // Cast a ray upwards to check for obstacles
        Ray ray = new Ray(position, Vector3.up);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, spawnRadius))
        {
            // Return false if the hit object is on the obstacle layer
            if (((1 << hitInfo.collider.gameObject.layer) & obstacleLayer) != 0) return false;
        }

        return true;
    }
}
