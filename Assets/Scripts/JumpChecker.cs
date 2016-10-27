using UnityEngine;
using System.Collections.Generic;

//Used for testing to see which platforms the player can jump to from the current platform.
//The jump checker script should be attached to an object that has a collider component that jumping should be tested from.
//The script will then highlight the colliders that can be reached from this collider.
public class JumpChecker : MonoBehaviour
{
    WalkChecker walkChecker = null;
    Collider attachedCollider = null;
    bool initialised = false;

    List<WalkChecker.ColliderFace> reachableFaces;

    public void TestJumps()
    {
        if (!initialised)
        {
            attachedCollider = GetComponent<Collider>();
            if (!attachedCollider)
            {
                Debug.LogError("Error: the jump checker must be added to a gameobject that has a collider component!");
                return;
            }

            walkChecker = FindObjectOfType<WalkChecker>();
            if (!walkChecker)
            {
                Debug.LogError("Error: there must be a walk checker in the scene for the jump checker to work!");
                return;
            }

            reachableFaces = new List<WalkChecker.ColliderFace>();

            initialised = true;
        }

        //Update the walkable surfaces before we check where we can jump to.
        walkChecker.DisplayDebugSurfaces();

        reachableFaces = GetReachableFaces();
    }

    List<WalkChecker.ColliderFace> GetReachableFaces()
    {
        //TODO: logic for which faces can be reached.
        return walkChecker.walkableFaces;
    }

    public void OnDrawGizmos()
    {
        if (initialised)
        {
            Gizmos.color = Color.yellow;

            for (int i = 0; i < reachableFaces.Count; ++i)
            {
                Gizmos.matrix = Matrix4x4.TRS(reachableFaces[i].position, Quaternion.Euler(new Vector3(0, 0, reachableFaces[i].rotation)), new Vector3(reachableFaces[i].length, 0.1f, walkChecker.playerDiameter));
                Gizmos.DrawCube(new Vector3(0, 1, 0), new Vector3(1, 1, 1));
            }
        }
    }

}
