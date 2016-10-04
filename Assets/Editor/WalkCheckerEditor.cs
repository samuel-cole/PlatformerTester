using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(WalkChecker))]
public class WalkCheckerEditor : Editor
{
    public WalkCheckerEditor()
    {
        EditorApplication.update += EditorUpdate;
    }

    float disableTime = 2.0f;
    float disableTimer = -1.0f;

    float timeLastFrame = 0;

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

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginDisabledGroup(disableTimer > 0);
        WalkChecker script = (WalkChecker)target;
        if (GUILayout.Button("Check Walkable Surfaces"))
        {
            disableTimer = disableTime;
            script.DisplayDebugSurfaces();
            Repaint();
        }
        if (GUILayout.Button("Clear Walkable Surfaces"))
        {
            script.RemoveDebugSurfaces();
        }
    }
}
