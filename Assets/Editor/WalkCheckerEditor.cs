using UnityEngine;
using UnityEditor;

/// <summary>
/// Walk checker editor is used for providing the user control over the walkchecker- 
/// it adds buttons to display and clear the walkable surface displays.
/// </summary>
[CustomEditor(typeof(WalkChecker))]
public class WalkCheckerEditor : Editor
{
    /// <summary>
    /// Used for telling Unity to call this classes update function for every editor tick.
    /// </summary>
    public WalkCheckerEditor()
    {
        EditorApplication.update += EditorUpdate;
    }

    /// <summary>
    /// The duration that the editor buttons should be disabled for after being pressed, 
    /// to show the user that their input worked, and to prevent multiple rapid walk checks.
    /// </summary>
    const float DISABLE_TIME = 2.0f;
    /// <summary>
    /// The current amount of time until the editor buttons are re-enabled. 
    /// Should be negative if editor buttons are already enabled.
    /// </summary>
    float disableTimer = -1.0f;

    /// <summary>
    /// The real time since startup of the last frame- used for generating an editor delta time.
    /// </summary>
    float timeLastFrame = 0;

    /// <summary>
    /// Called every frame that the editor is running. Handles re-enabling disabled editor buttons after a set duration.
    /// </summary>
    void EditorUpdate()
    {
        if (disableTimer > 0.0f)
        {
            disableTimer -= (Time.realtimeSinceStartup - timeLastFrame);
            if (disableTimer < 0.0f)
                Repaint();
        }
        timeLastFrame = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Used for drawing editor buttons for user control, 
    /// and for calling the appropriate functions within the walk checker if those buttons are pushed.
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WalkChecker script = (WalkChecker)target;

        //Display a mask field to allow players to choose which collision layers the walk checker should use.
        string[] maskOptions = new string[32];
        for (int i = 0; i < 32; ++i)
        {
            maskOptions[i] = i.ToString();
        }
        script.collisionLayerMask = EditorGUI.MaskField(EditorGUILayout.GetControlRect(), "Blocking Layers: ", script.collisionLayerMask, maskOptions);

        EditorGUI.BeginDisabledGroup(disableTimer > 0);

        //Display a button to allow the player to use the walk checker.
        if (GUILayout.Button("Check Walkable Surfaces"))
        {
            disableTimer = DISABLE_TIME;
            script.DisplayDebugSurfaces();
            Repaint();
        }
        //Display a button to allow the user to reset the walk checker.
        if (GUILayout.Button("Clear Walkable Surfaces"))
        {
            script.RemoveDebugSurfaces();
            Repaint();
        }
    }
}
