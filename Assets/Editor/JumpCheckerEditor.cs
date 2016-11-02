using UnityEngine;
using UnityEditor;


/// <summary>
/// Jump checker editor is used for providing the user control over the walkchecker- 
/// it adds buttons to display and clear the reachable surface displays.
/// </summary>
[CustomEditor(typeof(JumpChecker))]
public class JumpCheckerEditor : Editor
{
    /// <summary>
    /// Used for telling Unity to call this classes update function for every editor tick.
    /// </summary>
    public JumpCheckerEditor()
    {
        EditorApplication.update += EditorUpdate;
    }

    /// <summary>
    /// The duration that the editor buttons should be disabled for after being pressed, 
    /// to show the user that their input worked, and to prevent multiple rapid jump checks.
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
    /// and for calling the appropriate functions within the jump checker if those buttons are pushed.
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        JumpChecker script = (JumpChecker)target;

        EditorGUI.BeginDisabledGroup(disableTimer > 0);

        //Display a button to allow the player to use the jump checker.
        if (GUILayout.Button("Check Jumps"))
        {
            disableTimer = DISABLE_TIME;
            script.TestJumps();
            Repaint();
        }

        //Display a button to allow the user to reset the jump checker.
        if (GUILayout.Button("Clear Jump Checks"))
        {
            disableTimer = DISABLE_TIME;
            script.Reset();
            Repaint();
        }
        //TODO: perhaps add some form of option here to allow users to select which walkable faces from those attached to this object that jumping should be tested from.
    }
}
