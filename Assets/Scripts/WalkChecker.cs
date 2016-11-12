using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// The walk checker is used for finding which faces of which colliders within the scene can be walked on.
/// The two things that it checks to test if faces are walkable are whether the face is at an angle that the player can walk on,
/// and whether the face has any colliders above it that would prevent the player from fitting on it.
/// </summary>
public class WalkChecker : MonoBehaviour
{

    /// <summary>
    /// Collider face is a data-only class that is used for passing information 
    /// about each face to both the OnGUI render function and the jump checker.
    /// Each collider face represents a single face of a single collider within the game scene (eg: a box collider will have 6 of these).
    /// </summary>
    public class ColliderFace
    {
        /// <summary>
        /// The collider that this face belongs to.
        /// </summary>
        public Collider collider;
        /// <summary>
        /// The position of the face within the game scene (NOT the position of the collider).
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The rotation of the face, measured in degrees.
        /// As the walk checker is designed for 2.5d games, only a float is required for rotation (the z axis).
        /// </summary>
        public float rotation;
        /// <summary>
        /// The size of the collider face.
        /// As the walk checker is designed for 2.5d games, only a float is required for the size of the collider face (the x-axis length).
        /// </summary>
        public float length;

        /// <summary>
        /// Get the highest point on this collider face.
        /// </summary>
        /// <returns>Returns the highest point on this collider face. In situations in which the collider is flat (multiple highest points),
        /// the x-value used is the same as the position.</returns>
        public Vector2 HighestPoint()
        {
            float x;
            if (rotation == 0.0f)
                x = position.x;
            else if (rotation > 0.0f)
                x = position.x + length * -0.5f;
            else
                x = position.x + length * 0.5f;
            
            float y = position.y + length * 0.5f * Mathf.Sin(Mathf.Abs(rotation));

            return new Vector2(x, y);
        }

        /// <summary>
        /// Gets the left-most point on this collider face.
        /// </summary>
        /// <returns>Returns the left-most point on this collider face, in world space.</returns>
        public Vector2 LeftMostPoint()
        {
            float x = position.x + length * -0.5f;
            float y = position.y + length * 0.5f * Mathf.Sin(rotation);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Gets the right-most point on this collider face.
        /// </summary>
        /// <returns>Returns the right-most point on this collider face, in world space.</returns>
        public Vector2 RightMostPoint()
        {
            float x = position.x + length * 0.5f;
            float y = position.y + length * 0.5f * Mathf.Sin(-rotation);
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// Reference to the player that will be navigating the scene, 
    /// used for determining the radius, height, and etc. for where collider faces should be blocked.
    /// </summary>
    public CharacterController player;

    /// <summary>
    /// The end result of this class- a list of collider faces that contains each surface that the player is able to walk on.
    /// </summary>
    public List<ColliderFace> walkableFaces { get; private set; }
    
    /// <summary>
    /// The radius of the player capsule, used while checking the spaces in which the player can fit.
    /// </summary>
    float playerRadius = 0.5f;
    /// <summary>
    /// The diameter of the player capsule, used while checking the spaces in which the player can fit.
    /// </summary>
    public float playerDiameter { get; private set; }

    /// <summary>
    /// A layer mask that should be set to contain each layer that the player isn't able to move through,
    /// used while checking the spaces in which the player can fit.
    /// </summary>
    [HideInInspector]
    public int collisionLayerMask;

    /// <summary>
    /// The height that the player is able to jump.
    /// This isn't used by the WalkChecker at all- it's used by the jump checker, 
    /// however as jump checkers need to be set up on each platform that jumping is to be tested from,
    /// it seems like better design to access it from here instead.
    /// </summary>
    public float playerJumpHeight;

    /// <summary>
    /// The horizontal speed of the player in units/second.
    /// This isn't used by the WalkChecker at all- it's used by the jump checker,
    /// however as jump checkers need to be set up on each platform that jumping is to be tested from,
    /// it seems like better design to access it from here instead.
    /// </summary>
    public float playerHorizontalSpeed;

    /// <summary>
    /// The rate of downwards accelleration due to gravity of the player in units/second/second.
    /// This isn't used by the WalkChecker at all- it's used by the jump checker,
    /// however as jump checkers need to be set up on each platform that jumping is to be tested from,
    /// it seems like better design to access it from here instead.
    /// </summary>
    public float gravityStrength;

    /// <summary>
    /// Clears all walkable faces, and hence removes all displays for walkable faces.
    /// </summary>
    public void RemoveDebugSurfaces()
    {
        if (walkableFaces != null)
            walkableFaces.Clear();
    }

    /// <summary>
    /// The main function within the walkchecker, this iterates through each collider in the scene,
    /// and calls the other functions to process those colliders.
    /// The end result of this function is that the walkable faces list will be filled.
    /// </summary>
    public void DisplayDebugSurfaces()
    {
        if (walkableFaces == null)
            walkableFaces = new List<ColliderFace>();
        else
            walkableFaces.Clear();

        if (player)
        {
            playerRadius = player.radius;
            playerDiameter = playerRadius * 2.0f;
        }
        else
        {
            Debug.LogError("No player set in the walk checker!");
            return;
        }

        Collider[] colliders = FindObjectsOfType<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.enabled == false)
                continue;

            if (((1 << collider.gameObject.layer) & collisionLayerMask) == 0)   //If this collider isn't in a collision layer used by the environment.
                continue;

            //Going to have to handle this per collider type- for box collider, just check rotation/scale for walkable area, for mesh, will have to split into triangles?
            if (collider is BoxCollider)
            {
                List<ColliderFace> slopeCheckedFaces = BoxCheckSlope(collider as BoxCollider);
                foreach (ColliderFace face in slopeCheckedFaces)
                {
                    walkableFaces.AddRange(CheckCollisionsWithFace(face));
                }
            }
            else if (collider is MeshCollider)
            {
                Debug.Log("Mesh colliders are not yet implemented within the walk checker, skipping collider.");
            }
            else
            {
                Debug.Log("The specified collider type (" + collider.GetType().Name + ") is not yet implemented within the walk checker, skipping collider.");
            }
        }
    }

    /// <summary>
    /// Checks whether any of the faces of the box passed in as an argument are of a slope that the player can stand on.
    /// </summary>
    /// <param name="a_box">The box to be checked for walkable slopes.</param>
    /// <returns>A list containing each face that the player can stand on, with each face being represented as the normal vector of that face, multiplied by its distance to the center of that object.</returns>
    List<ColliderFace> BoxCheckSlope(BoxCollider a_box)
    {
        List<ColliderFace> returnFaces = new List<ColliderFace>();

        //Each box has 6 faces, need to check each of them.
        Vector3[] colliderPoints = new Vector3[6];
        colliderPoints[0] = Vector3.forward;
        colliderPoints[1] = Vector3.right;
        colliderPoints[2] = Vector3.up;
        colliderPoints[3] = Vector3.back;
        colliderPoints[4] = Vector3.left;
        colliderPoints[5] = Vector3.down;
        Vector3 colliderCenter = Vector3.zero;

        //Set up the transforms that we will be moving these points by.
        Matrix4x4 colliderTransform = new Matrix4x4();
        colliderTransform.SetTRS(a_box.center, Quaternion.identity, a_box.size);
        Matrix4x4 objectTransform = a_box.transform.localToWorldMatrix;

        float slopeLimit = player.slopeLimit * Mathf.Deg2Rad;

        //Convert center into world space.
        colliderCenter = colliderTransform.MultiplyPoint(colliderCenter);
        colliderCenter = objectTransform.MultiplyPoint(colliderCenter);

        for (int i = 0; i < 6; ++i)
        {
            //Convert collider point into world space.
            colliderPoints[i] /= 2.0f;
            colliderPoints[i] = colliderTransform.MultiplyPoint(colliderPoints[i]);
            colliderPoints[i] = objectTransform.MultiplyPoint(colliderPoints[i]);

            Vector3 toColliderPoint = colliderPoints[i] - colliderCenter;

            //Check which direction the collider normal is closest to- ignore if +-z axis, use if +-x or +-y.
            //This is a dot without having dot products because I am dotting against axes, which simplifies the calculations down a lot.
            float zDot = Mathf.Abs(toColliderPoint.z);
            float otherDot = Mathf.Max(Mathf.Abs(toColliderPoint.x), Mathf.Abs(toColliderPoint.y));

            if (otherDot >= zDot)
            {
                //Compare the 2D (XY plane) angle with the max slope rotation.
                float angle = Mathf.Atan2(toColliderPoint.y, toColliderPoint.x);
                
                //The angle calculated above is the angle of the normal, rather than the angle of elevation of the plane, which is the logic that makes the most sense to me.
                //As such, convert to use the angle of elevation.
                angle -= Mathf.PI / 2.0f;
                if (angle < -Mathf.PI)
                    angle += Mathf.PI * 2.0f;

                if (angle < slopeLimit && angle > -slopeLimit || angle > Mathf.PI - slopeLimit || angle < -Math.PI + slopeLimit)
                {
                    //Slope is valid.
                    ColliderFace face = new ColliderFace();
                    face.collider = a_box;
                    face.rotation = angle * Mathf.Rad2Deg;
                    face.position = colliderPoints[i];

                    //I think that there is likely a much more elegant way to do this than this switch statement, however I can't think of it.
                    //This doesn't work- for each face, there are two possible sides that could be used depending on the way that the face is rotated (see commented out sides);
                    switch (i)
                    {
                        case 0:
                            face.length = a_box.size.x * a_box.transform.lossyScale.x; // a_box.size.y * a_box.transform.lossyScale.y;
                            break;
                        case 1:
                            face.length = a_box.size.y * a_box.transform.lossyScale.y; //a_box.size.z * a_box.transform.lossyScale.z;
                            break;
                        case 2:
                            face.length = a_box.size.x * a_box.transform.lossyScale.x; //a_box.size.z * a_box.transform.lossyScale.z;
                            break;
                        case 3:
                            face.length = a_box.size.x * a_box.transform.lossyScale.x; //a_box.size.y * a_box.transform.lossyScale.y;
                            break;
                        case 4:
                            face.length = a_box.size.y * a_box.transform.lossyScale.y; //a_box.size.z * a_box.transform.lossyScale.z;
                            break;
                        case 5:
                            face.length = a_box.size.x * a_box.transform.lossyScale.x; //a_box.size.z * a_box.transform.lossyScale.z;
                            break;
                    }

                    //TODO: check if the face crosses the XY plane.
                    
                    //Faces come in sets of two (eg: top/bottom), both will get counted as having a viable slope, so remove the lower of the two (as it collides with itself).
                    //The opposite face for this point is always at i-3.
                    //We want the face facing upwards so either discard this face or the other depending on the height of the face.
                    //Maybe this should be moved earlier? Don't need to calculate scale/angle for this face if it is going to be discarded.
                    bool doNotAddToList = false;

                    if (i > 2)
                    {
                        for (int j = 0; j < returnFaces.Count; ++j)
                        {
                            if (returnFaces[j].position == colliderPoints[i - 3])
                            {
                                if (colliderPoints[i].y > colliderPoints[i - 3].y)
                                {
                                    returnFaces[j] = face;
                                }
                                doNotAddToList = true;
                                break;
                            }
                        }
                    }                  

                    if (!doNotAddToList)
                        returnFaces.Add(face);
                }
            }
        }

        return returnFaces;
    }

    /// <summary>
    /// Checks whether a face is colliding with anything, and either shortens it or splits it
    /// into multiple new faces to ensure that the face doesn't cover any area blocked by objects.
    /// Known issue: doesn't account for situations in which the entire area above the face is within a collider.
    /// Known issue: doesn't properly work in situations in which multiple objects block the same face.
    /// </summary>
    /// <param name="a_face">The walkable face to be checked for blocking objects.</param>
    /// <returns>A list of all non-blocked faces created by the function.</returns>
    List<ColliderFace> CheckCollisionsWithFace(ColliderFace a_face)
    {
        List<ColliderFace> returnFaces = new List<ColliderFace>();

        //Get all of the variables necessary for a capsule cast.
        float faceRot = a_face.rotation * Mathf.Deg2Rad;
        Vector3 faceEdgeOffset = (a_face.length / 2.0f) * new Vector3(Mathf.Cos(faceRot), Mathf.Sin(faceRot), 0.0f);
        Vector3 rightMostFacePoint = a_face.position + faceEdgeOffset;

        Vector3 rightMostCapsuleBasePoint = rightMostFacePoint + Vector3.up * playerRadius;
        Vector3 rightMostCapsuleTopPoint = rightMostCapsuleBasePoint + new Vector3(0, player.height - playerDiameter, 0);
        Vector3 leftMostCapsuleBasePoint = rightMostCapsuleBasePoint - faceEdgeOffset * 2.0f;
        Vector3 leftMostCapsuleTopPoint = leftMostCapsuleBasePoint + new Vector3(0, player.height - playerDiameter, 0);

        float checkDistance = faceEdgeOffset.magnitude;
        Vector3 toRightDirection = faceEdgeOffset/checkDistance;  //Normalizing the direction.
        checkDistance *= 2.0f;

        /*I'm thinking that the best way to determine where to split the object may be to raycast from both sides of the platform- 
          if a collision occurs, then I can check from the other side to find the other side of the object.
          The method I'm going to use for doing this is:

            Trace all both ways.
            if there are more hits in one than the other, then cut from the last impact point to the end in the one with more hits, then remove that hit.
            flip one of the hit arrays.
            Now at each index, one hit array will have the left side of an object and the other will have the right side.
        */

        //Remove the walkable surface's collider to prevent self-collision. 
        bool colliderDisabled = false;
        if (a_face.collider.enabled)
        {
            a_face.collider.enabled = false;
            colliderDisabled = true;
        }

        //Note: contrary to what the Unity documentation says, capsule casts seem to return a collision at position (0,0,0)
        //in situations in which the beginning of the capsule cast is within a collider.
        RaycastHit[] leftSideCollisions = Physics.CapsuleCastAll(leftMostCapsuleBasePoint, leftMostCapsuleTopPoint, playerRadius, toRightDirection, checkDistance, collisionLayerMask);
        RaycastHit[] rightSideCollisions = Physics.CapsuleCastAll(rightMostCapsuleBasePoint, rightMostCapsuleTopPoint, playerRadius, -toRightDirection, checkDistance, collisionLayerMask);

        if (colliderDisabled)
        {
            a_face.collider.enabled = true;
        }

        //Unity doesn't return the raycasts in a meaningful order,
        //so sort the raycast arrays based on the impact point distance from the start of the raycast.
        float[] leftSideCollisionKeys = new float[leftSideCollisions.Length];
        float[] rightSideCollisionKeys = new float[rightSideCollisions.Length];
        for (int i = 0; i < leftSideCollisions.Length; ++i)
        {
            leftSideCollisionKeys[i] = leftSideCollisions[i].distance;
        }
        for (int i = 0; i < rightSideCollisions.Length; ++i)
        {
            //This code could be done in the previous loop, cutting down on a loop, because leftSideCollisions and rightSideCollisions
            //*should* always be the same length. For safety reasons (just in case raycasts do some odd behaviour), it isn't.
            rightSideCollisionKeys[i] = rightSideCollisions[i].distance;
        }
        Array.Sort(leftSideCollisionKeys, leftSideCollisions);
        Array.Sort(rightSideCollisionKeys, rightSideCollisions);

        //This has been changed to check if the collision point is (0, 0, 0), rather than using array length (as specified in my psuedocode),
        //because capsule casts do not behave as specified in the Unity docs- they return a collision
        //at position (0, 0, 0) for situations in which the start of the trace is in a collider.
        if (rightSideCollisions.Length > 0 && NearlyEqual(rightSideCollisions[0].point, new Vector3(0, 0, 0)))
        {
            //The right side of the platform is inside another collider.
            //The base point x value is used in further calculations as a way of checking the x-location of the leftmost/rightmost points of the plane, so update it here.
            rightMostCapsuleBasePoint = new Vector3(leftSideCollisions[leftSideCollisions.Length - 1].point.x - playerRadius, rightMostCapsuleBasePoint.y, rightMostCapsuleBasePoint.z);

            RaycastHit[] newHits = new RaycastHit[leftSideCollisions.Length - 1];
            Array.Copy(leftSideCollisions, 0, newHits, 0, newHits.Length);
            leftSideCollisions = newHits;

            RaycastHit[] newRightHits = new RaycastHit[rightSideCollisions.Length - 1];
            Array.Copy(rightSideCollisions, 1, newRightHits, 0, newRightHits.Length);
            rightSideCollisions = newRightHits;
        }
        else if (leftSideCollisions.Length > 0 && NearlyEqual(leftSideCollisions[0].point, new Vector3(0, 0, 0)))
        {
            //The left side of the platform is inside another collider.
            //The base point x value is used in further calculations as a way of checking the x-location of the leftmost/rightmost points of the plane, so update it here.
            leftMostCapsuleBasePoint = new Vector3(rightSideCollisions[rightSideCollisions.Length - 1].point.x + playerRadius, leftMostCapsuleBasePoint.y, leftMostCapsuleBasePoint.z);

            RaycastHit[] newHits = new RaycastHit[rightSideCollisions.Length - 1];
            Array.Copy(rightSideCollisions, 0, newHits, 0, newHits.Length);
            rightSideCollisions = newHits;

            RaycastHit[] newLeftHits = new RaycastHit[leftSideCollisions.Length - 1];
            Array.Copy(leftSideCollisions, 1, newLeftHits, 0, newLeftHits.Length);
            leftSideCollisions = newLeftHits;
        }

        //Reverse so that each index refers to the same object for both arrays.
        Array.Reverse(rightSideCollisions);

        //Iterate through each lot of collisions, and split the faces for each one.
        for (int i = -1; i < leftSideCollisions.Length; ++i)
        {
            float leftPoint;
            if (i == -1)    //For the first iteration, start at the left side of the collider face.
                leftPoint = leftMostCapsuleBasePoint.x;
            else
                leftPoint = rightSideCollisions[i].point.x + playerRadius;
            
            float rightPoint;
            if (i == leftSideCollisions.Length - 1)     //For the last iteration, end at the right side of the collider face.
                rightPoint = rightMostCapsuleBasePoint.x;
            else
                rightPoint = leftSideCollisions[i + 1].point.x - playerRadius;

            returnFaces.Add(GetFacePortion(a_face, leftPoint, rightPoint));
        } 

        return returnFaces;
    }

    /// <summary>
    /// Creates a new face object from an existing collider face- 
    /// this new object should overlap the existing collider face completely, but not be as long.
    /// </summary>
    /// <param name="a_face">The face to generate a portion of.</param>
    /// <param name="a_leftPoint">The world-space x-position of the leftmost point on the new face portion.</param>
    /// <param name="a_rightPoint">The world-space x-position of the rightmost point on the new face portion.</param>
    /// <returns>The smaller collider face portion created.</returns>
    ColliderFace GetFacePortion(ColliderFace a_face, float a_leftPoint, float a_rightPoint)
    {
        ColliderFace returnFace = new ColliderFace();

        //The new x position should be halfway between a_leftPoint and a_rightPoint, and the dimension should be the difference between leftpoint and rightpoint.
        //The y position is harder- it's based on the rotation of the face.
        returnFace.rotation = a_face.rotation;

        float tan = Mathf.Tan(a_face.rotation * Mathf.Deg2Rad);
        float rightY = a_face.position.y + tan * (a_rightPoint - a_face.position.x);
        float leftY = a_face.position.y + tan * (a_leftPoint - a_face.position.x); //Not too sure about this line, haven't really thought it through.

        returnFace.length = new Vector2(a_rightPoint - a_leftPoint, rightY - leftY).magnitude;

        float x = a_leftPoint + (a_rightPoint - a_leftPoint) / 2.0f;
        float y = leftY + (rightY - leftY) / 2.0f;
        float z = a_face.position.z;

        returnFace.position = new Vector3(x, y, z);

        returnFace.collider = a_face.collider;

        return returnFace;
    }

    /// <summary>
    /// Draws the walkable collider faces.
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (walkableFaces != null)
        {
            for (int i = 0; i < walkableFaces.Count; ++i)
            {
                //For some reason, placing position information in the cube vs the transform matrix behaves differently- might this be doing transforms out of order? TRS v RST or etc.
                //I think that the x dimension here should likely just be replaced with a 1, to constrain the area highlighted to the plane that the player can move in.
                Gizmos.matrix = Matrix4x4.TRS(walkableFaces[i].position, Quaternion.Euler(new Vector3(0, 0, walkableFaces[i].rotation)), new Vector3(walkableFaces[i].length, 0.1f, playerDiameter));
                Gizmos.DrawCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            }
        }
    }

    /// <summary>
    /// Calculates if two vectors are nearly equal to each other (to prevent float equality issues).
    /// </summary>
    /// <param name="a_vec1">The first vector to compare.</param>
    /// <param name="a_vec2">The second vector to compare.</param>
    /// <returns>Whether the two vectors are nearly equal.</returns>
    bool NearlyEqual(Vector3 a_vec1, Vector3 a_vec2)
    {
        if ((a_vec1 - a_vec2).sqrMagnitude < 0.01f)
            return true;
        return false;
    }

}
