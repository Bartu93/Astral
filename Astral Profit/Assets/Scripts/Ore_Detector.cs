using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

public class Ore_Detector : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public string animationTrigger = "StartDetection";

    [Header("Detection Settings")]
    public float delayBeforeAnimation = 1f;

    [Header("VFX Settings")]
    public GameObject vfxPrefab;
    public Transform vfxSpawnPoint; // Optional: specific spawn point for VFX
    public float vfxGrowthInterval = 30f; // Frames (30 frames = 0.5 seconds at 60fps)
    public Vector3 vfxStartScale = Vector3.one * 0.1f;
    public Vector3 vfxMaxScale = Vector3.one * 2f;
    public float vfxGrowthAmount = 0.2f;
    public float vfxTweenDuration = 0.3f;
    public AnimationCurve vfxScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Ore Spawning Settings")]
    public GameObject commonOrePrefab;
    public GameObject strategicOrePrefab;
    public float oreSpawnChancePerInterval = 40f; // Chance that ANY ore spawns this interval
    public float commonOreSpawnChance = 70f; // Percentage chance for common ore (when ore spawns)
    public float strategicOreSpawnChance = 30f; // Percentage chance for strategic ore (when ore spawns)
    public int maxOresPerInterval = 3;
    public float oreSpawnRadius = 0.8f; // Multiplier of VFX scale for spawn area
    public LayerMask oreLayer = 1; // Layer for spawned ores
    public bool preventOreOverlap = true;
    public float oreOverlapCheckRadius = 0.5f;

    [Header("Ore Scale Settings")]
    public float commonOreMinScale = 0.5f;
    public float commonOreMaxScale = 1.5f;
    public float strategicOreMinScale = 0.8f;
    public float strategicOreMaxScale = 2.0f;

    private bool animationStarted = false;
    private GameObject currentVFX;
    private Coroutine vfxGrowthCoroutine;
    private SphereCollider vfxSphereCollider;
    private List<GameObject> spawnedOres = new List<GameObject>();

    void Start()
    {
        // Get the Animator component if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Start animation after delay (this only runs when building is placed)
        if (delayBeforeAnimation > 0)
        {
            Invoke(nameof(StartAnimation), delayBeforeAnimation);
        }
        else
        {
            StartAnimation();
        }
    }

    void StartAnimation()
    {
        if (animationStarted || animator == null) return;

        animator.SetTrigger(animationTrigger);
        animationStarted = true;

        // Start VFX system
        StartVFXSystem();

        Debug.Log($"Ore Detector animation started for {gameObject.name}");
    }

    void StartVFXSystem()
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning($"VFX Prefab not assigned for {gameObject.name}");
            return;
        }

        vfxPrefab.SetActive(true);
        currentVFX = vfxPrefab;

        // Set initial scale
        currentVFX.transform.localScale = vfxStartScale;

        // Setup or get sphere collider for VFX
        SetupVFXCollider();

        // Start growth coroutine
        vfxGrowthCoroutine = StartCoroutine(VFXGrowthLoop());
    }

    void SetupVFXCollider()
    {
        // Get or add sphere collider to VFX
        vfxSphereCollider = currentVFX.GetComponent<SphereCollider>();
        if (vfxSphereCollider == null)
        {
            vfxSphereCollider = currentVFX.AddComponent<SphereCollider>();
        }

        // Configure collider
        vfxSphereCollider.isTrigger = true;
        vfxSphereCollider.radius = 0.5f; // Base radius, will scale with VFX

        Debug.Log($"VFX Sphere Collider setup for {gameObject.name}");
    }

    IEnumerator VFXGrowthLoop()
    {
        Vector3 currentScale = vfxStartScale;

        while (currentVFX != null && currentScale.x < vfxMaxScale.x)
        {
            // Wait for the specified number of frames
            for (int i = 0; i < vfxGrowthInterval; i++)
            {
                yield return null; // Wait one frame
            }

            // Calculate new scale
            Vector3 newScale = currentScale + Vector3.one * vfxGrowthAmount;
            newScale = Vector3.Min(newScale, vfxMaxScale); // Clamp to max scale

            // Animate scale change with tween
            yield return StartCoroutine(TweenScale(currentScale, newScale));

            currentScale = newScale;

            // Spawn ores after each growth interval
            SpawnOres();
        }

        Debug.Log($"VFX growth completed for {gameObject.name}");

        // Disable VFX when max scale is reached
        if (currentVFX != null)
        {
            currentVFX.SetActive(false);
            Debug.Log($"VFX disabled for {gameObject.name} - max scale reached");
        }
    }

    IEnumerator TweenScale(Vector3 fromScale, Vector3 toScale)
    {
        if (currentVFX == null) yield break;

        float elapsed = 0f;

        while (elapsed < vfxTweenDuration)
        {
            if (currentVFX == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / vfxTweenDuration;

            // Apply animation curve for smooth easing
            float curveValue = vfxScaleCurve.Evaluate(t);
            Vector3 currentScale = Vector3.Lerp(fromScale, toScale, curveValue);

            currentVFX.transform.localScale = currentScale;

            yield return null;
        }

        // Ensure final scale is set
        if (currentVFX != null)
        {
            currentVFX.transform.localScale = toScale;
        }
    }

    void SpawnOres()
    {
        if (currentVFX == null || (commonOrePrefab == null && strategicOrePrefab == null))
        {
            Debug.LogWarning($"Cannot spawn ores - missing prefabs or VFX for {gameObject.name}");
            return;
        }

        // Check if any ore should spawn this interval
        float spawnRoll = Random.Range(0f, 100f);
        if (spawnRoll > oreSpawnChancePerInterval)
        {
            Debug.Log($"No ore spawned this interval for {gameObject.name} (rolled {spawnRoll:F1}%, needed ≤{oreSpawnChancePerInterval}%)");
            return;
        }

        Debug.Log($"Ore spawn triggered for {gameObject.name} (rolled {spawnRoll:F1}%)");

        // Calculate spawn area based on current VFX scale
        float spawnRadius = (currentVFX.transform.localScale.x * oreSpawnRadius) * 0.5f;
        Vector3 spawnCenter = currentVFX.transform.position;

        // Determine how many ores to spawn this interval
        int oresToSpawn = Random.Range(1, maxOresPerInterval + 1);

        for (int i = 0; i < oresToSpawn; i++)
        {
            // Determine ore type based on percentage chances
            GameObject oreToSpawn = DetermineOreType();
            if (oreToSpawn == null) continue;

            // Find valid spawn position
            Vector3 spawnPosition = FindValidSpawnPosition(spawnCenter, spawnRadius);
            if (spawnPosition == Vector3.zero) continue; // No valid position found

            // Instantiate ore at ground level
            GameObject newOre = Instantiate(oreToSpawn, spawnPosition, Quaternion.identity);

            // Set random uniform scale based on ore type
            Vector3 randomScale = GetRandomOreScale(oreToSpawn);
            newOre.transform.localScale = randomScale;

            // Set ore layer
            newOre.layer = Mathf.RoundToInt(Mathf.Log(oreLayer.value, 2));

            // Add to spawned ores list
            spawnedOres.Add(newOre);

            Debug.Log($"Spawned {oreToSpawn.name} at {spawnPosition} with uniform scale {randomScale.x} for {gameObject.name}");
        }
    }

    GameObject DetermineOreType()
    {
        float totalChance = commonOreSpawnChance + strategicOreSpawnChance;
        if (totalChance <= 0) return null;

        float randomValue = Random.Range(0f, totalChance);

        if (randomValue <= commonOreSpawnChance && commonOrePrefab != null)
        {
            return commonOrePrefab;
        }
        else if (strategicOrePrefab != null)
        {
            return strategicOrePrefab;
        }

        return null;
    }

    Vector3 GetRandomOreScale(GameObject orePrefab)
    {
        float minScale, maxScale;

        // Determine scale range based on ore type
        if (orePrefab == commonOrePrefab)
        {
            minScale = commonOreMinScale;
            maxScale = commonOreMaxScale;
        }
        else if (orePrefab == strategicOrePrefab)
        {
            minScale = strategicOreMinScale;
            maxScale = strategicOreMaxScale;
        }
        else
        {
            // Fallback to common ore scales if ore type is unknown
            minScale = commonOreMinScale;
            maxScale = commonOreMaxScale;
        }

        // Generate uniform random scale (same for all axes to maintain circular shape)
        float uniformScale = Random.Range(minScale, maxScale);
        return Vector3.one * uniformScale;
    }

    Vector3 FindValidSpawnPosition(Vector3 center, float radius)
    {
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random position within circle (ground-based)
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidatePosition = new Vector3(
                center.x + randomCircle.x,
                center.y, // Keep at detector height or use ground level
                center.z + randomCircle.y
            );

            // Optionally raycast down to find ground level
            RaycastHit hit;
            if (Physics.Raycast(candidatePosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                candidatePosition.y = hit.point.y + 0.1f; // Slightly above ground
            }

            // Check for overlap if prevention is enabled
            if (preventOreOverlap)
            {
                bool hasOverlap = false;
                Collider[] overlapping = Physics.OverlapSphere(candidatePosition, oreOverlapCheckRadius, oreLayer);

                foreach (Collider col in overlapping)
                {
                    if (spawnedOres.Contains(col.gameObject))
                    {
                        hasOverlap = true;
                        break;
                    }
                }

                if (!hasOverlap)
                {
                    return candidatePosition;
                }
            }
            else
            {
                return candidatePosition;
            }
        }

        Debug.LogWarning($"Could not find valid spawn position for ore in {gameObject.name}");
        return Vector3.zero; // Invalid position
    }

    void OnDestroy()
    {
        // Clean up coroutine when detector is destroyed
        if (vfxGrowthCoroutine != null)
        {
            StopCoroutine(vfxGrowthCoroutine);
        }

        // Clean up spawned ores
        CleanupSpawnedOres();
    }

    void CleanupSpawnedOres()
    {
        foreach (GameObject ore in spawnedOres)
        {
            if (ore != null)
            {
                DestroyImmediate(ore);
            }
        }
        spawnedOres.Clear();
    }

    // Public method to manually stop VFX
    public void StopVFX()
    {
        if (vfxGrowthCoroutine != null)
        {
            StopCoroutine(vfxGrowthCoroutine);
            vfxGrowthCoroutine = null;
        }

        if (vfxPrefab != null)
        {
            vfxPrefab.SetActive(false);
        }
    }

    // Public method to restart VFX
    public void RestartVFX()
    {
        StopVFX();
        CleanupSpawnedOres();
        StartVFXSystem();
    }

    // Public method to get all spawned ores
    public List<GameObject> GetSpawnedOres()
    {
        // Remove null references
        spawnedOres.RemoveAll(ore => ore == null);
        return new List<GameObject>(spawnedOres);
    }

    // Public method to clear all spawned ores
    public void ClearSpawnedOres()
    {
        CleanupSpawnedOres();
    }

    // Public method to set ore spawn chances
    public void SetOreSpawnChances(float intervalSpawnChance, float commonChance, float strategicChance)
    {
        oreSpawnChancePerInterval = Mathf.Clamp(intervalSpawnChance, 0f, 100f);
        commonOreSpawnChance = Mathf.Clamp(commonChance, 0f, 100f);
        strategicOreSpawnChance = Mathf.Clamp(strategicChance, 0f, 100f);
    }

    // Public method to set ore scale ranges
    public void SetOreScaleRanges(float commonMin, float commonMax, float strategicMin, float strategicMax)
    {
        commonOreMinScale = commonMin;
        commonOreMaxScale = commonMax;
        strategicOreMinScale = strategicMin;
        strategicOreMaxScale = strategicMax;
    }

    // Gizmo drawing for visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (currentVFX != null)
        {
            Gizmos.color = Color.yellow;
            float spawnRadius = (currentVFX.transform.localScale.x * oreSpawnRadius) * 0.5f;
            Gizmos.DrawWireSphere(currentVFX.transform.position, spawnRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentVFX.transform.position, spawnRadius * 0.9f);
        }
    }
}