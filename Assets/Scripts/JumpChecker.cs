using UnityEngine;
using System.Collections.Generic;

//Used for testing to see which platforms the player can jump to from the current platform.
//The jump checker script should be attached to an object that has a collider component that jumping should be tested from.
//The script will then highlight the colliders that can be reached from this collider.
public class JumpChecker : MonoBehaviour
{
    WalkChecker walkChecker = null;
    bool initialised = false;

    //Attached faces are the collider faces that are attached to this gameobject- these are the faces that we are jumping from.
    List<WalkChecker.ColliderFace> attachedFaces;
    //Reachable faces are the collider faces that can be reached from a jump from the attached faces- the platforms that we can jump to.
    List<WalkChecker.ColliderFace> reachableFaces;

    public void TestJumps()
    {
        if (!initialised)
        {
            Collider[] attachedColliders = GetComponentsInChildren<Collider>();
            if (attachedColliders.Length == 0)
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

            attachedFaces = new List<WalkChecker.ColliderFace>();

            foreach (WalkChecker.ColliderFace walkableFace in walkChecker.walkableFaces)
            {
                foreach (Collider attachedCollider in attachedColliders)
                {
                    if (walkableFace.collider == attachedCollider)
                    {
                        attachedFaces.Add(walkableFace);
                        break;
                    }
                }
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
        //Thoughts about the logic to use here when determining which faces are reachable-
        //Any colliders higher than the height of the highest point on this platform plus the player's jump height can be immediately excluded from the search.
        //The top of the reachable area by the player will be flat, with the sides of this then curving away into parabolas.

        List<WalkChecker.ColliderFace> returnFaces = new List<WalkChecker.ColliderFace>();

        //Iterate through each attached face, find all of their reachable faces, and then add those reachable faces to the overall list for this game object.
        foreach (WalkChecker.ColliderFace attachedFace in attachedFaces)
        {
            float highestJumpPoint = attachedFace.position.y + (attachedFace.length/2.0f) * Mathf.Sin(Mathf.Abs(attachedFace.rotation)) + walkChecker.playerJumpHeight;

            //Start with all faces, then exclude the invalid ones.
            List<WalkChecker.ColliderFace> reachableFaces = walkChecker.walkableFaces;
            foreach (WalkChecker.ColliderFace potentialFace in reachableFaces)
            {
                //Any colliders higher than the height of the highest point on this platform plus the player's jump height can be immediately excluded from the search.
                if (potentialFace.position.y > attachedFace.position.y + walkChecker.playerJumpHeight)
                {
                    reachableFaces.Remove(potentialFace);
                    continue;
                }
            }


            //We've excluded all unreachable faces now, add the remaining reachable faces to the overall list of reachable faces.
            returnFaces.AddRange(reachableFaces);
        }


        //TODO: logic for which faces can be reached.
        return returnFaces;
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

    public void Reset()
    {
        initialised = false;
        attachedFaces = null;
        reachableFaces = null;
    }
}
