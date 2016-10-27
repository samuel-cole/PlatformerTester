using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(JumpChecker))]
public class JumpCheckerEditor : Editor {

    public JumpCheckerEditor()
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

        JumpChecker script = (JumpChecker)target;

        EditorGUI.BeginDisabledGroup(disableTimer > 0);
        if (GUILayout.Button("Check Jumps"))
        {
            disableTimer = disableTime;
            script.TestJumps();
            Repaint();
        }
    }
}
