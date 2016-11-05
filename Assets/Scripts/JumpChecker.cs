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

        //Concavity of the parobola defined by this player's jump arc.
        //Inequality to find space above a parabola: y > a(x - b)^2 + c
        //To get a: x = ut + 0.5 * gt^2, therefore after 1 second distance = 0.5g
        //a = (y - c)/(x - b)^2, x != b. Sub in (x:initial + playerspeed, y:initial - 0.5g), to get a = -0.5g / playerspeed^2
        float parabolaConcavity = -0.5f * walkChecker.playerHorizontalSpeed / (walkChecker.playerHorizontalSpeed * walkChecker.playerHorizontalSpeed);
        //Derived from equations of motion.
        float playerJumpSpeed = Mathf.Sqrt(2 * walkChecker.gravityStrength * walkChecker.playerJumpHeight);
        float xDistToJumpMaxima = (playerJumpSpeed / walkChecker.gravityStrength) * walkChecker.playerHorizontalSpeed;

        //Iterate through each attached face, find all of their reachable faces, and then add those reachable faces to the overall list for this game object.
        foreach (WalkChecker.ColliderFace attachedFace in attachedFaces)
        {
            float highestJumpPoint = attachedFace.HighestPoint().y + walkChecker.playerJumpHeight;
            Vector2 attachedFaceLeftPoint = attachedFace.LeftMostPoint();
            Vector2 attachedFaceRightPoint = attachedFace.RightMostPoint();

            //Start with all faces, then exclude the invalid ones.
            List<WalkChecker.ColliderFace> reachableFaces = new List<WalkChecker.ColliderFace>(walkChecker.walkableFaces);
            foreach (WalkChecker.ColliderFace potentialFace in reachableFaces.ToArray())
            {
                //Any colliders higher than the height of the highest point on this platform plus the player's jump height can be immediately excluded from the search.
                if (potentialFace.position.y > highestJumpPoint)
                {
                    reachableFaces.Remove(potentialFace);
                    continue;
                }

                Vector2 potentialFaceLeftPoint = potentialFace.LeftMostPoint();
                Vector2 potentialFaceRightPoint = potentialFace.RightMostPoint();

                //The below calculations for excluding points above the parabola defined by the player's jump arc are super-naive.
                //They presume that the best point to jump from is always the edge of the attached platform closest to the potential platform.
                //They also presume that all platforms within the horizontal range defined by the maxima of the player's jumps from the edges of the attached platform can be reached 
                //as long as they are lower than the highest point the player can reach, which isn't true in the case of an angled attached platform.
                //They don't account for the player having a radius- they only account for if the player's center will reach the platform.
                //TODO: these calculations put the jump turning point above the edge of the attached platform, when they should check how far out the player can get
                //before they reach the turning point.
                if (potentialFaceLeftPoint.x > attachedFaceRightPoint.x + xDistToJumpMaxima)
                {
                    //Inequality to find space above a parabola: y > a(x - b)^2 + c
                    //Turning point is jumpheight above the rightmost attached point. (b = rightmostx, c = pos.y + jumpheight)
                    //Want to find the y-value at x = potentialFace.leftmostpoint
                    float c = attachedFaceRightPoint.y + walkChecker.playerJumpHeight;
                    if (potentialFaceLeftPoint.y > parabolaConcavity * Mathf.Pow((potentialFaceLeftPoint.x - (attachedFaceRightPoint.x + xDistToJumpMaxima)), 2) + c)
                    {
                        reachableFaces.Remove(potentialFace);
                        continue;
                    }
                }
                else if (potentialFaceRightPoint.x < attachedFaceLeftPoint.x - xDistToJumpMaxima)
                {
                    //Inequality to find space above a parabola: y > a(x - b)^2 + c
                    //Turning point is jumpheight above the leftmost attached point. (b = leftmostx, c = pos.y + jumpheight)
                    //Want to find the y-value at x = potentialFace.rightmostpoint
                    float c = attachedFaceLeftPoint.y + walkChecker.playerJumpHeight;
                    if (potentialFaceRightPoint.y > parabolaConcavity * Mathf.Pow((potentialFaceRightPoint.x - (attachedFaceLeftPoint.x - xDistToJumpMaxima)), 2) + c)
                    {
                        reachableFaces.Remove(potentialFace);
                        continue;
                    }
                }


                //TODO: more logic for excluding invalid faces.
                //This should include logic to account for objects blocking the area in which the player can jump.

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

            if (reachableFaces == null)
            {
                initialised = false;
                return;
            }

            for (int i = 0; i < reachableFaces.Count; ++i)
            {
                Gizmos.matrix = Matrix4x4.TRS(reachableFaces[i].position, Quaternion.Euler(new Vector3(0, 0, reachableFaces[i].rotation)), new Vector3(reachableFaces[i].length, 0.1f, walkChecker.playerDiameter));
                Gizmos.DrawCube(new Vector3(0, 2.5f, 0), new Vector3(1, 5, 1));
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
