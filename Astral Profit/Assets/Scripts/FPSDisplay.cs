using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private float deltaTime = 0.0f;
    public Color textColor = Color.white; // You can change this to customize the text color
    public int fontSize = 24; // Customize the font size
    public Vector2 position = new Vector2(10, 10); // Position on the screen

    void Update()
    {
        // Update the deltaTime with the time between frames, smoothed over multiple frames
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        // Calculate the FPS
        float fps = 1.0f / deltaTime;

        // Set the style for the text
        GUIStyle style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = textColor;

        // Create a string for the FPS display
        string text = string.Format("{0:0.} FPS", fps);

        // Draw the text in the upper left corner of the screen
        GUI.Label(new Rect(position.x, position.y, 200, 50), text, style);
    }
}
