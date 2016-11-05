using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Used for testing to see which platforms the player can jump to from the current platform.
/// The jump checker script should be attached to an object that has a (or multiple!) collider component(s) that jumping should be tested from.
/// The script will then highlight the colliders that can be reached from the attached collider(s).
/// </summary>
public class JumpChecker : MonoBehaviour
{
    /// <summary>
    /// Reference to the walk checker that has generated the walkable faces for this jump tester to operate on.
    /// </summary>
    WalkChecker walkChecker = null;

    /// <summary>
    /// Whether this jump checker has been initialised or not- 
    /// used for preventing first-time use code from running after every usage of the jump checker,
    /// and for preventing the jump checker from trying to render anything before it is properly set up.
    /// </summary>
    bool initialised = false;

    /// <summary>
    /// Attached faces are the collider faces that are attached to this gameobject- these are the faces that we are jumping from.
    /// </summary>
    List<WalkChecker.ColliderFace> attachedFaces;
    /// <summary>
    /// Reachable faces are the main output of this class- 
    /// they are the collider faces that can be reached from a jump from the attached faces- the platforms that we can jump to. 
    /// </summary>
    List<WalkChecker.ColliderFace> reachableFaces;

    /// <summary>
    /// Initialises the class, if it isn't already, updates the walk checker, then calls GetReachableFace() to update the list of reachable faces.
    /// Called from a UI button set up in the JumpCheckerEditor.
    /// </summary>
    public void TestJumps()
    {
        if (!initialised)
        {
            walkChecker = FindObjectOfType<WalkChecker>();
            if (!walkChecker)
            {
                Debug.LogError("Error: there must be a walk checker in the scene for the jump checker to work!");
                return;
            }

            attachedFaces = new List<WalkChecker.ColliderFace>();
            reachableFaces = new List<WalkChecker.ColliderFace>();

            initialised = true;
        }

        //Update the walkable surfaces before we check where we can jump to/what we are attached to.
        walkChecker.DisplayDebugSurfaces();

        //The player may add/remove colliders between the first and later uses of the jump checker-
        //as such, this has to be done every time the jump checker is used, rather than at initialisation.
        Collider[] attachedColliders = GetComponentsInChildren<Collider>();
        if (attachedColliders.Length == 0)
        {
            Debug.LogError("Error: the jump checker must be added to a gameobject that has a collider component!");
            return;
        }

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

        reachableFaces = GetReachableFaces();
    }

    /// <summary>
    /// Determines which walkable faces within the level can be reached from the attached faces.
    /// </summary>
    /// <returns>A list containing each walkable face that can be reached from the attached faces.</returns>
    List<WalkChecker.ColliderFace> GetReachableFaces()
    {
        //Thoughts about the logic to use here when determining which faces are reachable-
        //Any colliders higher than the height of the highest point on this platform plus the player's jump height can be immediately excluded from the search.
        //Directly above the attached collider, the area the player will be able to reach will be defined by a linear boundry at the same angle as the attached collider.
        //^ the above is not entirely correct- in situations in which the collider is sloped, the player can start at the highest point, then jump horizontally to get above the other points. The area defined by that linear area presumes that the player is jumping straight up.
        //To the sides of the attached collider, the area the player will be able to reach will curving away into parabolas.
        //Under each blocking object, the area that the player can reach will be a parabola curving inwards from the blocking edge.
        //All of the logic used so far presumes that x-movement is linear in speed, and y-movement is quadratic (due to gravity).
        //The problem with just using the parabola with its turning point above the leftmost/rightmost point when determining if a platform is reachable is that in some situations (those in which the platform is rotated such that you can get further from jumping off a higher point rather than the closest one), the player can reach further by jumping off a point other than the furthest point.

        List<WalkChecker.ColliderFace> returnFaces = new List<WalkChecker.ColliderFace>();

        //Iterate through each attached face, find all of their reachable faces, and then add those reachable faces to the overall list for this game object.
        foreach (WalkChecker.ColliderFace attachedFace in attachedFaces)
        {
            float highestJumpPoint = attachedFace.HighestPoint().y + walkChecker.playerJumpHeight;

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

                //The problem with just using the parabola with its turning point above the leftmost/rightmost point
                //when determining if a platform is reachable is that in some situations
                //(those in which the platform is rotated such that you can get further from jumping off a 
                //higher point rather than the closest one), the player can reach further by jumping off
                //a point other than the furthest point.
                

                //Parabola equation: a(x - b)^2 + c
                //Turning point is jumpheight above the leftmost/rightmost point. (b = rightmostx/leftmostx, c = pos.y + jumpheight)
                //a is proportional to the strength of gravity and the player's move speed.
                //Want to find the y-value at x = potentialFace.leftmostpoint/potentialFace.rightmostpoint (should be the opposite of whether the face is on the left or right)


                //TODO: more logic for excluding invalid faces.

            }

            //We've excluded all unreachable faces now, add the remaining reachable faces to the overall list of reachable faces.
            returnFaces.AddRange(reachableFaces);
        }

        return returnFaces;
    }

    /// <summary>
    /// Draws the reachable collider face (these are displayed slightly above the surface drawn for walkable collider faces).
    /// </summary>
    public void OnDrawGizmos()
    {
        if (initialised)
        {
            Gizmos.color = Color.yellow;

            for (int i = 0; i < reachableFaces.Count; ++i)
            {
                Gizmos.matrix = Matrix4x4.TRS(reachableFaces[i].position, Quaternion.Euler(new Vector3(0, 0, reachableFaces[i].rotation)), new Vector3(reachableFaces[i].length, 0.1f, walkChecker.playerDiameter));
                //This cube is drawn 1 unit higher than the position of the face to prevent z-fighting 
                //and similar problems from the gizmos used to display walkable faces.
                Gizmos.DrawCube(new Vector3(0, 1, 0), new Vector3(1, 1, 1));
            }
        }
    }

    /// <summary>
    /// Resets the jump checker to the stat it was in before being initialised.
    /// </summary>
    public void Reset()
    {
        initialised = false;
        attachedFaces = null;
        reachableFaces = null;
    }
}
