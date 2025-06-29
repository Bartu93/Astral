using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Add some space
        EditorGUILayout.Space();
        
        MapGenerator mapGenerator = (MapGenerator)target;
        
        // Add the Generate Map button
        if (GUILayout.Button("Generate New Map", GUILayout.Height(30)))
        {
            GenerateMapInEditor(mapGenerator);
        }
        
        // Add a Clear Map button as well
        if (GUILayout.Button("Clear Map", GUILayout.Height(25)))
        {
            ClearMapInEditor(mapGenerator);
        }
    }
    
    private void GenerateMapInEditor(MapGenerator mapGenerator)
    {
        // Clear existing map first
        ClearMapInEditor(mapGenerator);
        
        // Generate new map
        mapGenerator.GenerateMapInEditor();
        
        // Mark the scene as dirty so Unity knows to save changes
        EditorUtility.SetDirty(mapGenerator);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mapGenerator.gameObject.scene);
    }
    
    private void ClearMapInEditor(MapGenerator mapGenerator)
    {
        // Find all child objects of the MapGenerator and destroy them
        for (int i = mapGenerator.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(mapGenerator.transform.GetChild(i).gameObject);
        }
    }
}