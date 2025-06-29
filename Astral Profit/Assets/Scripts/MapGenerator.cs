using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] ResourceSpawner resourceSpawner;
    [SerializeField] EnvironmentInstantiator environmentInstantiator;

    [SerializeField] int _mapWidth;
    [SerializeField] int _mapHeight;
    [SerializeField] int _tileSize;
    [SerializeField] int _combineThreshold = 10; // Number of objects to combine per layer
    [SerializeField] bool _skipGenerationIfMapExists = false; // Skip generation if map already exists

    public float xRotationOffset;
    public float zRotationOffset;

    // Noise settings
    [SerializeField] private float _baseNoiseFrequency = 100f;
    [SerializeField] private int _noiseSeed = 45687254;
    [SerializeField] private int _octaves = 3;
    [SerializeField] private float _persistence = 0.5f;
    [SerializeField] private float _lacunarity = 2f;

    // Mountain Range Settings
    [Header("Mountain Range Settings")]
    [SerializeField] private bool _generateMountainRanges = true;
    [SerializeField] private int _mountainRangeCount = 3; // Number of mountain ranges to generate
    [SerializeField] private Vector2Int _mountainRangeMinSize = new Vector2Int(3, 3); // Minimum size (width, height)
    [SerializeField] private Vector2Int _mountainRangeMaxSize = new Vector2Int(8, 8); // Maximum size (width, height)
    [SerializeField] private List<GameObject> _rockPrefabs = new List<GameObject>(); // Rock prefabs to place
    [SerializeField] private float _rockDensity = 0.7f; // Probability of placing a rock on each mountain tile (0-1)
    [SerializeField] private Vector2Int _rocksPerTileRange = new Vector2Int(1, 3); // Min and max rocks per tile when placing
    [SerializeField] private Vector2 _rockScaleRange = new Vector2(0.6f, 1.4f); // Min and max scale for rocks
    [SerializeField] private float _rockHeightOffset = 0.5f; // Height offset for rock placement
    [SerializeField] private float _rockSpreadRadius = 1.5f; // How far rocks can spread from tile center
    [SerializeField] private int _minDistanceBetweenRanges = 5; // Minimum distance between mountain ranges

    [Header("General Map Population Settings")]
    [SerializeField] private bool _generateMapObjects = true;
    [SerializeField] private int _mapObjectCount = 100; // NEW: Target number of objects to place
    [SerializeField] private List<GameObject> _mapObjectPrefabs = new List<GameObject>();
    //[SerializeField] private float _mapObjectDensity = 0.1f; // This can now be used as backup or removed
    [SerializeField] private Vector2 _mapObjectScaleRange = new Vector2(0.8f, 1.2f);
    [SerializeField] private float _mapObjectHeightOffset = 0.0f;
    [SerializeField] private float _mapObjectSpreadRadius = 0.8f;
    [SerializeField] private bool _avoidMountainTiles = true;
    [SerializeField] private List<int> _excludedLayers = new List<int>();

    // Struct to define a layer (threshold and prefab)
    [System.Serializable]
    public struct Layer
    {
        public float threshold;
        public GameObject prefab;
    }

    // Struct to define a mountain range
    [System.Serializable]
    public struct MountainRange
    {
        public Vector2Int position; // Bottom-left pivot position
        public Vector2Int size; // Width and height
        public List<Vector2Int> tiles; // Tiles that belong to this mountain range
    }

    public List<Layer> layers = new List<Layer>();
    private List<MountainRange> mountainRanges = new List<MountainRange>();

    private Dictionary<int, List<CombineInstance>> combineInstancesByLayer = new Dictionary<int, List<CombineInstance>>(); // Store combine instances by layer
    private Dictionary<int, int> currentBatchCountByLayer = new Dictionary<int, int>(); // Track batch count per layer
    private Dictionary<Vector2, GameObject> tileMap = new Dictionary<Vector2, GameObject>(); // Store tile data
    private HashSet<Vector2Int> mountainTiles = new HashSet<Vector2Int>(); // Track which tiles are mountains

    void Start()
    {
        // Check if we should skip generation if map already exists
        if (_skipGenerationIfMapExists && HasExistingMap())
        {
            Debug.Log("Map generation skipped - existing map detected and skip option is enabled.");
            return;
        }

        // Start the entire sequence in one coroutine
        StartCoroutine(GenerateMapThenScanThenSpawnResources());
    }

    private IEnumerator GenerateMapThenScanThenSpawnResources()
    {
        // Step 1: Generate Mountain Ranges first
        if (_generateMountainRanges)
        {
            yield return StartCoroutine(GenerateMountainRanges());
        }

        // Step 2: Generate Map
        yield return StartCoroutine(GenerateMap());

        // Step 3: Place Rocks on Mountain Ranges
        if (_generateMountainRanges && _rockPrefabs.Count > 0)
        {
            yield return StartCoroutine(PlaceRocksOnMountains());
        }

        // Step 4: Place General Map Objects (REMOVED DUPLICATE GenerateMap() CALL)
        if (_generateMapObjects && _mapObjectPrefabs.Count > 0)
        {
            yield return StartCoroutine(PlaceGeneralMapObjects());
            Debug.Log("Map objects placement completed");
        }

        // Step 5: Scan after Map Generation
        yield return StartCoroutine(PerformScan());

        // Step 6: Spawn Resources after Scanning Completes
        yield return StartCoroutine(SpawnResources());
    }

    private IEnumerator GenerateMountainRanges()
    {
        mountainRanges.Clear();
        mountainTiles.Clear();

        for (int i = 0; i < _mountainRangeCount; i++)
        {
            MountainRange range = CreateMountainRange();
            if (range.tiles.Count > 0) // Only add if valid range was created
            {
                mountainRanges.Add(range);
                foreach (var tile in range.tiles)
                {
                    mountainTiles.Add(tile);
                }
            }
            yield return null; // Spread work across frames
        }

        Debug.Log($"Generated {mountainRanges.Count} mountain ranges with a total of {mountainTiles.Count} mountain tiles.");
    }

    private MountainRange CreateMountainRange()
    {
        MountainRange range = new MountainRange();
        int attempts = 50; // Maximum attempts to find a valid position

        while (attempts > 0)
        {
            // Random size within bounds
            int width = UnityEngine.Random.Range(_mountainRangeMinSize.x, _mountainRangeMaxSize.x + 1);
            int height = UnityEngine.Random.Range(_mountainRangeMinSize.y, _mountainRangeMaxSize.y + 1);

            // Random position (ensuring the range fits within map bounds)
            int x = UnityEngine.Random.Range(0, _mapWidth - width);
            int z = UnityEngine.Random.Range(0, _mapHeight - height);

            Vector2Int position = new Vector2Int(x, z);
            Vector2Int size = new Vector2Int(width, height);

            // Check if this position conflicts with existing mountain ranges
            if (IsValidMountainRangePosition(position, size))
            {
                range.position = position;
                range.size = size;
                range.tiles = GenerateMountainRangeTiles(position, size);
                break;
            }

            attempts--;
        }

        return range;
    }

    private bool IsValidMountainRangePosition(Vector2Int position, Vector2Int size)
    {
        // Check distance from existing mountain ranges
        foreach (var existingRange in mountainRanges)
        {
            // Calculate distance between range centers
            Vector2 thisCenter = new Vector2(position.x + size.x / 2f, position.y + size.y / 2f);
            Vector2 existingCenter = new Vector2(
                existingRange.position.x + existingRange.size.x / 2f,
                existingRange.position.y + existingRange.size.y / 2f
            );

            float distance = Vector2.Distance(thisCenter, existingCenter);
            if (distance < _minDistanceBetweenRanges)
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector2Int> GenerateMountainRangeTiles(Vector2Int position, Vector2Int size)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // Create an organic mountain shape instead of a perfect rectangle
        Vector2 center = new Vector2(position.x + size.x / 2f, position.y + size.y / 2f);
        float maxDistance = Mathf.Min(size.x, size.y) / 2f;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int z = position.y; z < position.y + size.y; z++)
            {
                // Ensure we're within map bounds
                if (x >= 0 && x < _mapWidth && z >= 0 && z < _mapHeight)
                {
                    // Create organic shape using distance from center and some randomness
                    Vector2 tilePos = new Vector2(x, z);
                    float distanceFromCenter = Vector2.Distance(tilePos, center);
                    float normalizedDistance = distanceFromCenter / maxDistance;

                    // Use perlin noise for organic edges
                    float noiseValue = Mathf.PerlinNoise(x * 0.3f, z * 0.3f);
                    float threshold = 0.3f + normalizedDistance * 0.7f; // Fade out towards edges

                    if (noiseValue > threshold)
                    {
                        tiles.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        return tiles;
    }

    private IEnumerator PlaceRocksOnMountains()
    {
        if (_rockPrefabs.Count == 0) yield break;

        GameObject rockParent = new GameObject("Mountain_Rocks");
        rockParent.transform.parent = this.transform;

        int rocksPlaced = 0;

        foreach (var tile in mountainTiles)
        {
            if (UnityEngine.Random.value <= _rockDensity)
            {
                Vector2 hexCoords = GetHexCoords(tile.x, tile.y);
                Vector3 tileCenter = new Vector3(hexCoords.x, _rockHeightOffset, hexCoords.y);

                // Determine how many rocks to place on this tile
                int rocksToPlace = UnityEngine.Random.Range(_rocksPerTileRange.x, _rocksPerTileRange.y + 1);

                for (int i = 0; i < rocksToPlace; i++)
                {
                    // Create position with random spread around tile center
                    Vector3 position = tileCenter + new Vector3(
                        UnityEngine.Random.Range(-_rockSpreadRadius, _rockSpreadRadius),
                        UnityEngine.Random.Range(-0.2f, 0.3f), // Small height variation
                        UnityEngine.Random.Range(-_rockSpreadRadius, _rockSpreadRadius)
                    );

                    // Random rotation
                    Quaternion rotation = Quaternion.Euler(
                        UnityEngine.Random.Range(-15f, 15f),
                        UnityEngine.Random.Range(0f, 360f),
                        UnityEngine.Random.Range(-15f, 15f)
                    );

                    // Random scale variation using the configurable range
                    float scale = UnityEngine.Random.Range(_rockScaleRange.x, _rockScaleRange.y);

                    // Select random rock prefab
                    GameObject rockPrefab = _rockPrefabs[UnityEngine.Random.Range(0, _rockPrefabs.Count)];
                    GameObject rock = Instantiate(rockPrefab, position, rotation, rockParent.transform);
                    rock.transform.localScale = Vector3.one * scale;

                    // Optional: Add random naming for organization
                    rock.name = $"Rock_{tile.x}_{tile.y}_{i}";

                    rocksPlaced++;
                }

                // Spread work across frames
                if (rocksPlaced % 20 == 0)
                {
                    yield return null;
                }
            }
        }

        Debug.Log($"Placed {rocksPlaced} rocks on mountain ranges.");
    }

    private IEnumerator PlaceGeneralMapObjects()
    {
        Debug.Log($"Starting PlaceGeneralMapObjects - Prefab count: {_mapObjectPrefabs.Count}");

        if (_mapObjectPrefabs.Count == 0)
        {
            Debug.LogWarning("No map object prefabs assigned!");
            yield break;
        }

        GameObject objectParent = new GameObject("Map_Objects");
        objectParent.transform.parent = this.transform;

        int objectsPlaced = 0;
        int maxAttempts = _mapObjectCount * 3; // Prevent infinite loops

        // Create list of valid tiles for random selection
        List<Vector2Int> validTiles = new List<Vector2Int>();

        Debug.Log($"Checking tiles for validity - Map size: {_mapWidth}x{_mapHeight}");
        Debug.Log($"Mountain tiles count: {mountainTiles.Count}");
        Debug.Log($"Avoid mountain tiles: {_avoidMountainTiles}");

        for (int x = 0; x < _mapWidth; x++)
        {
            for (int z = 0; z < _mapHeight; z++)
            {
                Vector2Int tilePos = new Vector2Int(x, z);

                // Skip mountain tiles if enabled
                if (_avoidMountainTiles && mountainTiles.Contains(tilePos))
                    continue;

                // Check layer - only allow beach (0) or ground (1) layers
                Vector2 hexCoords = GetHexCoords(x, z);
                float noiseValue = GeneratePerlinNoiseWithOctaves(hexCoords);
                int tileLayerIndex = 0;

                foreach (var layer in layers)
                {
                    if (noiseValue >= layer.threshold)
                    {
                        tileLayerIndex = layers.IndexOf(layer);
                    }
                    else
                    {
                        break;
                    }
                }

                // Only allow beach (layer 0) or ground (layer 1) tiles
                if (tileLayerIndex != 0 && tileLayerIndex != 1)
                    continue;

                // Skip if this layer is in the excluded list
                if (_excludedLayers.Contains(tileLayerIndex))
                    continue;
                
                // Skip water areas (below beach threshold)
                if (noiseValue < layers[0].threshold)
                    continue;

                // Only allow beach (layer 0) or ground (layer 1) tiles
                if (tileLayerIndex != 0 && tileLayerIndex != 1)
                    continue;
                validTiles.Add(tilePos);
            }
        }

        Debug.Log($"Found {validTiles.Count} valid tiles for object placement (beach/ground only)");

        if (validTiles.Count == 0)
        {
            Debug.LogWarning("No valid tiles found for object placement!");
            yield break;
        }

        // Shuffle the valid tiles for random placement
        for (int i = 0; i < validTiles.Count; i++)
        {
            Vector2Int temp = validTiles[i];
            int randomIndex = UnityEngine.Random.Range(i, validTiles.Count);
            validTiles[i] = validTiles[randomIndex];
            validTiles[randomIndex] = temp;
        }

        // Place objects until we reach the target count or run out of valid tiles
        foreach (var tilePos in validTiles)
        {
            if (objectsPlaced >= _mapObjectCount)
                break;

            Vector2 hexCoords = GetHexCoords(tilePos.x, tilePos.y);

            // Create position with random spread around tile center
            Vector3 tileCenter = new Vector3(hexCoords.x, _mapObjectHeightOffset, hexCoords.y);
            Vector3 position = tileCenter + new Vector3(
                UnityEngine.Random.Range(-_mapObjectSpreadRadius, _mapObjectSpreadRadius),
                UnityEngine.Random.Range(-0.1f, 0.2f), // Small height variation
                UnityEngine.Random.Range(-_mapObjectSpreadRadius, _mapObjectSpreadRadius)
            );

            // Random rotation (full 360 on Y, slight tilt on X and Z)
            Quaternion rotation = Quaternion.Euler(
                UnityEngine.Random.Range(-5f, 5f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(-5f, 5f)
            );

            // Random scale variation using the configurable range
            float scale = UnityEngine.Random.Range(_mapObjectScaleRange.x, _mapObjectScaleRange.y);

            // Select random object prefab
            GameObject objectPrefab = _mapObjectPrefabs[UnityEngine.Random.Range(0, _mapObjectPrefabs.Count)];
            GameObject mapObject = Instantiate(objectPrefab, position, rotation, objectParent.transform);
            mapObject.transform.localScale = Vector3.one * scale;

            // Optional: Add naming for organization
            mapObject.name = $"MapObject_{tilePos.x}_{tilePos.y}";

            objectsPlaced++;

            // Spread work across frames
            if (objectsPlaced % 20 == 0)
            {
                yield return null;
            }
        }

        Debug.Log($"Placed {objectsPlaced} general map objects on beach/ground tiles (target was {_mapObjectCount}).");
    }

    private IEnumerator GenerateMap()
    {
        MakeMapGrid(); // Run your map generation logic
        yield return null; // Wait until map generation is fully complete
    }

    private IEnumerator PerformScan()
    {
        //yield return new WaitForSeconds(1f); // Optional delay for scanning
        AstarPath.active.Scan(); // Perform the scan
        yield return null; // Ensure the scan fully completes before moving to the next step
    }

    private IEnumerator SpawnResources()
    {
        //resourceSpawner.PlaceResources(); // Start resource spawning
        //environmentInstantiator.PlaceEnvironmentObjects();
        yield return null; // Ensure resources fully spawn before ending
    }

    // NEW METHOD: Generate map in editor mode (synchronous)
    public void GenerateMapInEditor()
    {
        // Reset dictionaries for a fresh generation
        combineInstancesByLayer.Clear();
        currentBatchCountByLayer.Clear();
        tileMap.Clear();
        mountainRanges.Clear();
        mountainTiles.Clear();

        // Generate mountain ranges first
        if (_generateMountainRanges)
        {
            for (int i = 0; i < _mountainRangeCount; i++)
            {
                MountainRange range = CreateMountainRange();
                if (range.tiles.Count > 0)
                {
                    mountainRanges.Add(range);
                    foreach (var tile in range.tiles)
                    {
                        mountainTiles.Add(tile);
                    }
                }
            }
        }

        // Generate the map synchronously (no coroutines in editor)
        MakeMapGrid();

        // Place rocks if enabled
        if (_generateMountainRanges && _rockPrefabs.Count > 0)
        {
            PlaceRocksOnMountainsEditor();
        }

        // Place general map objects if enabled
        if (_generateMapObjects && _mapObjectPrefabs.Count > 0)
        {
            PlaceGeneralMapObjectsEditor();
        }

        Debug.Log("Map with mountain ranges and general objects generated in editor!");
    }

    private void PlaceGeneralMapObjectsEditor()
{
    Debug.Log($"Starting PlaceGeneralMapObjectsEditor - Prefab count: {_mapObjectPrefabs.Count}");

    if (_mapObjectPrefabs.Count == 0)
    {
        Debug.LogWarning("No map object prefabs assigned!");
        return;
    }

    GameObject objectParent = new GameObject("Map_Objects");
    objectParent.transform.parent = this.transform;

    int objectsPlaced = 0;

    // Create list of valid tiles for random selection
    List<Vector2Int> validTiles = new List<Vector2Int>();

    for (int x = 0; x < _mapWidth; x++)
    {
        for (int z = 0; z < _mapHeight; z++)
        {
            Vector2Int tilePos = new Vector2Int(x, z);

            // Skip mountain tiles if enabled
            if (_avoidMountainTiles && mountainTiles.Contains(tilePos))
                continue;

            // Check layer - only allow beach (0) or ground (1) layers
            Vector2 hexCoords = GetHexCoords(x, z);
            float noiseValue = GeneratePerlinNoiseWithOctaves(hexCoords);
            int tileLayerIndex = 0;

            foreach (var layer in layers)
            {
                if (noiseValue >= layer.threshold)
                {
                    tileLayerIndex = layers.IndexOf(layer);
                }
                else
                {
                    break;
                }
            }

            // Only allow beach (layer 0) or ground (layer 1) tiles
            if (tileLayerIndex != 0 && tileLayerIndex != 1)
                continue;

            // Skip if this layer is in the excluded list
            if (_excludedLayers.Contains(tileLayerIndex))
                continue;

            validTiles.Add(tilePos);
        }
    }

    Debug.Log($"Found {validTiles.Count} valid tiles for object placement (beach/ground only)");

    if (validTiles.Count == 0)
    {
        Debug.LogWarning("No valid tiles found for object placement!");
        return;
    }

    // Shuffle the valid tiles for random placement
    for (int i = 0; i < validTiles.Count; i++)
    {
        Vector2Int temp = validTiles[i];
        int randomIndex = UnityEngine.Random.Range(i, validTiles.Count);
        validTiles[i] = validTiles[randomIndex];
        validTiles[randomIndex] = temp;
    }

    // Place objects until we reach the target count or run out of valid tiles
    foreach (var tilePos in validTiles)
    {
        if (objectsPlaced >= _mapObjectCount)
            break;

        Vector2 hexCoords = GetHexCoords(tilePos.x, tilePos.y);

        // Create position with random spread around tile center
        Vector3 tileCenter = new Vector3(hexCoords.x, _mapObjectHeightOffset, hexCoords.y);
        Vector3 position = tileCenter + new Vector3(
            UnityEngine.Random.Range(-_mapObjectSpreadRadius, _mapObjectSpreadRadius),
            UnityEngine.Random.Range(-0.1f, 0.2f), // Small height variation
            UnityEngine.Random.Range(-_mapObjectSpreadRadius, _mapObjectSpreadRadius)
        );

        // Random rotation (full 360 on Y, slight tilt on X and Z)
        Quaternion rotation = Quaternion.Euler(
            UnityEngine.Random.Range(-5f, 5f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(-5f, 5f)
        );

        // Random scale variation using the configurable range
        float scale = UnityEngine.Random.Range(_mapObjectScaleRange.x, _mapObjectScaleRange.y);

        // Select random object prefab
        GameObject objectPrefab = _mapObjectPrefabs[UnityEngine.Random.Range(0, _mapObjectPrefabs.Count)];
        GameObject mapObject = Instantiate(objectPrefab, position, rotation, objectParent.transform);
        mapObject.transform.localScale = Vector3.one * scale;

        // Optional: Add naming for organization
        mapObject.name = $"MapObject_{tilePos.x}_{tilePos.y}";

        objectsPlaced++;
    }

    Debug.Log($"Placed {objectsPlaced} general map objects on beach/ground tiles in editor (target was {_mapObjectCount}).");
}

    private void PlaceRocksOnMountainsEditor()
    {
        if (_rockPrefabs.Count == 0) return;

        GameObject rockParent = new GameObject("Mountain_Rocks");
        rockParent.transform.parent = this.transform;

        int rocksPlaced = 0;

        foreach (var tile in mountainTiles)
        {
            if (UnityEngine.Random.value <= _rockDensity)
            {
                Vector2 hexCoords = GetHexCoords(tile.x, tile.y);
                Vector3 tileCenter = new Vector3(hexCoords.x, _rockHeightOffset, hexCoords.y);

                // Determine how many rocks to place on this tile
                int rocksToPlace = UnityEngine.Random.Range(_rocksPerTileRange.x, _rocksPerTileRange.y + 1);

                for (int i = 0; i < rocksToPlace; i++)
                {
                    // Create position with random spread around tile center
                    Vector3 position = tileCenter + new Vector3(
                        UnityEngine.Random.Range(-_rockSpreadRadius, _rockSpreadRadius),
                        UnityEngine.Random.Range(-0.2f, 0.3f), // Small height variation
                        UnityEngine.Random.Range(-_rockSpreadRadius, _rockSpreadRadius)
                    );

                    // Random rotation
                    Quaternion rotation = Quaternion.Euler(
                        UnityEngine.Random.Range(-15f, 15f),
                        UnityEngine.Random.Range(0f, 360f),
                        UnityEngine.Random.Range(-15f, 15f)
                    );

                    // Random scale variation
                    // Random scale variation using the configurable range
                    float scale = UnityEngine.Random.Range(_rockScaleRange.x, _rockScaleRange.y);

                    // Select random rock prefab
                    GameObject rockPrefab = _rockPrefabs[UnityEngine.Random.Range(0, _rockPrefabs.Count)];
                    GameObject rock = Instantiate(rockPrefab, position, rotation, rockParent.transform);
                    rock.transform.localScale = Vector3.one * scale;

                    // Optional: Add random naming for organization
                    rock.name = $"Rock_{tile.x}_{tile.y}_{i}";

                    rocksPlaced++;
                }
            }
        }

        Debug.Log($"Placed {rocksPlaced} rocks on mountain ranges in editor.");
    }

    // NEW METHOD: Check if map already exists
    private bool HasExistingMap()
    {
        // Check if there are any child objects with "CombinedMesh_Layer_" in their name
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.Contains("CombinedMesh_Layer_"))
            {
                return true;
            }
        }
        return false;
    }

    // PUBLIC METHOD: Get the skip generation setting (for editor use)
    public bool GetSkipGenerationSetting()
    {
        return _skipGenerationIfMapExists;
    }

    private Vector2 GetHexCoords(int x, int z)
    {
        float hexWidth = _tileSize;
        float hexHeight = _tileSize * Mathf.Sqrt(3) / 2;

        float xPos = x * hexWidth * 0.75f;
        float zPos = z * hexHeight + (x % 2 == 1 ? hexHeight / 2 : 0);

        return new Vector2(xPos, zPos);
    }

    float GeneratePerlinNoiseWithOctaves(Vector2 coords)
    {
        float frequency = _baseNoiseFrequency;
        float amplitude = 1f;
        float maxAmplitude = 0f;
        float noiseValue = 0f;

        for (int i = 0; i < _octaves; i++)
        {
            noiseValue += Mathf.PerlinNoise((coords.x + _noiseSeed) / frequency, (coords.y + _noiseSeed) / frequency) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= _persistence;
            frequency *= _lacunarity;
        }

        return noiseValue / maxAmplitude;
    }

    void MakeMapGrid()
    {
        Vector3 eulerRotation = new Vector3(xRotationOffset, 0, zRotationOffset);
        Quaternion rotation = Quaternion.Euler(eulerRotation);

        for (int x = 0; x < _mapWidth; x++)
        {
            for (int z = 0; z < _mapHeight; z++)
            {
                Vector2 hexCoords = GetHexCoords(x, z);
                Vector3 position = new Vector3(hexCoords.x, 0, hexCoords.y);

                if (_noiseSeed == -1) _noiseSeed = UnityEngine.Random.Range(0, 10000);

                float noiseValue = GeneratePerlinNoiseWithOctaves(hexCoords);

                // Check if this tile is part of a mountain range
                bool isMountainTile = mountainTiles.Contains(new Vector2Int(x, z));

                GameObject prefab = layers[0].prefab;
                int layerIndex = 0;

                if (isMountainTile)
                {
                    // Force mountain tiles to use higher layer (assuming higher indices are mountains)
                    // You can adjust this logic based on your layer setup
                    if (layers.Count > 2) // Ensure we have mountain layers
                    {
                        layerIndex = layers.Count - 1; // Use the highest layer for mountains
                        prefab = layers[layerIndex].prefab;
                    }
                }
                else
                {
                    // Normal terrain generation logic
                    foreach (var layer in layers)
                    {
                        if (noiseValue >= layer.threshold)
                        {
                            prefab = layer.prefab;
                            layerIndex = layers.IndexOf(layer);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (noiseValue < layers[0].threshold && !isMountainTile) continue;

                GameObject tile = Instantiate(prefab, position, rotation);
                tileMap[hexCoords] = tile; // Store tile in the map for future reference

                MeshFilter meshFilter = tile.GetComponent<MeshFilter>();

                if (meshFilter)
                {
                    CombineInstance combine = new CombineInstance();
                    combine.mesh = meshFilter.sharedMesh;
                    combine.transform = tile.transform.localToWorldMatrix;

                    if (!combineInstancesByLayer.ContainsKey(layerIndex))
                    {
                        combineInstancesByLayer[layerIndex] = new List<CombineInstance>();
                        currentBatchCountByLayer[layerIndex] = 0;
                    }

                    combineInstancesByLayer[layerIndex].Add(combine);
                    currentBatchCountByLayer[layerIndex]++;

                    // Use DestroyImmediate in editor mode, Destroy in play mode
                    if (Application.isPlaying)
                        Destroy(tile);
                    else
                        DestroyImmediate(tile);

                    if (currentBatchCountByLayer[layerIndex] >= _combineThreshold)
                    {
                        CombineMeshBatch(layerIndex);
                        currentBatchCountByLayer[layerIndex] = 0;
                    }
                }
            }
        }

        // Combine remaining batches for each layer
        foreach (var layerIndex in currentBatchCountByLayer.Keys)
        {
            if (currentBatchCountByLayer[layerIndex] > 0)
            {
                CombineMeshBatch(layerIndex);
            }
        }
    }

    void CombineMeshBatch(int layerIndex)
    {
        GameObject combinedObject = new GameObject($"CombinedMesh_Layer_{layerIndex}");
        combinedObject.transform.parent = this.transform;

        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedMeshRenderer = combinedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstancesByLayer[layerIndex].ToArray(), true, true);

        combinedMeshFilter.mesh = combinedMesh;
        combinedMeshRenderer.material = layers[layerIndex].prefab.GetComponent<MeshRenderer>().sharedMaterial;

        MeshCollider combinedMeshCollider = combinedObject.AddComponent<MeshCollider>();
        combinedMeshCollider.sharedMesh = combinedMesh;

        if (layerIndex == 2 || layerIndex == 3)
        {
            combinedObject.layer = LayerMask.NameToLayer("Obstacle");
        }
        else
        {
            combinedObject.layer = LayerMask.NameToLayer("Ground");
        }

        combineInstancesByLayer[layerIndex].Clear();
    }

    // Public method to visualize mountain ranges in editor (for debugging)
    void OnDrawGizmosSelected()
    {
        if (mountainRanges == null) return;

        Gizmos.color = Color.red;
        foreach (var range in mountainRanges)
        {
            // Draw range bounds
            Vector2 pivotHex = GetHexCoords(range.position.x, range.position.y);
            Vector3 pivotWorld = new Vector3(pivotHex.x, 1f, pivotHex.y);

            Vector2 sizeHex = new Vector2(range.size.x * _tileSize * 0.75f, range.size.y * _tileSize * Mathf.Sqrt(3) / 2);
            Vector3 size3D = new Vector3(sizeHex.x, 0.1f, sizeHex.y);

            Gizmos.DrawWireCube(pivotWorld + size3D * 0.5f, size3D);

            // Draw individual mountain tiles
            Gizmos.color = Color.yellow;
            foreach (var tile in range.tiles)
            {
                Vector2 tileHex = GetHexCoords(tile.x, tile.y);
                Vector3 tileWorld = new Vector3(tileHex.x, 0.5f, tileHex.y);
                Gizmos.DrawCube(tileWorld, Vector3.one * _tileSize * 0.5f);
            }
            Gizmos.color = Color.red;
        }
    }
}