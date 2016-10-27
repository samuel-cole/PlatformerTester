using UnityEngine;
using System;
using System.Collections.Generic;

public class WalkChecker : MonoBehaviour
{
    public class ColliderFace
    {
        public Collider collider;
        public Vector3 position;
        //As this is designed for 2.5d games, only a float is required for rotation (the z axis).
        //Measured in degrees.
        public float rotation;
        public float length;
    }
    

    public CharacterController player;

    public List<ColliderFace> walkableFaces { get; private set; }
    float playerRadius = 0.5f;
    public float playerDiameter { get; private set; }

    [HideInInspector]
    public int collisionLayerMask;

    List<Vector3> DEBUG_lineStarts = null;
    List<Vector3> DEBUG_lineEnds = null;

    public void RemoveDebugSurfaces()
    {
        if (walkableFaces != null)
            walkableFaces.Clear();

        if (DEBUG_lineStarts != null)
        {
            DEBUG_lineStarts.Clear();
            DEBUG_lineEnds.Clear();
        }
    }

    public void DisplayDebugSurfaces()
    {
        if (DEBUG_lineStarts == null)
        {
            DEBUG_lineStarts = new List<Vector3>();
            DEBUG_lineEnds = new List<Vector3>();
        }
        else
        {
            DEBUG_lineStarts.Clear();
            DEBUG_lineEnds.Clear();
        }

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

        //Instantiate(testObject, transform.position, transform.rotation);
        Collider[] colliders = FindObjectsOfType<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.enabled == false)
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
                Debug.Log("The specified mesh type is not yet implemented within the walk checker, skipping collider.");
            }
        }
    }

    //Checks whether any of the faces of the box passed in as an argument are of a slope that the player can stand on.
    //Returns a list containing each face that the player can stand on, with each face being represented as the normal vector of that face, multiplied by its distance to the center of that object.
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

    //Checks whether a face is colliding with anything, and either shortens it or splits it into multiple new faces to ensure that the face doesn't cover any area blocked by objects.
    //Returns a list of all non-blocked faces.
    //Known issue: doesn't account for situations in which the entire area above the face is within a collider.
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
        
        //This should be changed to check if the collision point is (0, 0, 0), rather than using array length, given that capsule casts
        //do not behave as specified in the Unity docs- they return a collision at position (0, 0, 0) for situations in which the start
        //of the trace is in a collider.
        if (leftSideCollisions.Length > rightSideCollisions.Length)
        {
            //The right side of the platform is inside another collider.
            //The base point x value is used in further calculations as a way of checking the x-location of the leftmost/rightmost points of the plane, so update it here.
            rightMostCapsuleBasePoint = new Vector3(leftSideCollisions[leftSideCollisions.Length - 1].point.x, rightMostCapsuleBasePoint.y, rightMostCapsuleBasePoint.z);

            RaycastHit[] newHits = new RaycastHit[leftSideCollisions.Length - 1];
            Array.Copy(leftSideCollisions, 0, newHits, 0, newHits.Length);
            leftSideCollisions = newHits;
        }
        else if (rightSideCollisions.Length < leftSideCollisions.Length)
        {
            //The left side of the platform is inside another collider.
            //The base point x value is used in further calculations as a way of checking the x-location of the leftmost/rightmost points of the plane, so update it here.
            leftMostCapsuleBasePoint = new Vector3(rightSideCollisions[rightSideCollisions.Length - 1].point.x, leftMostCapsuleBasePoint.y, leftMostCapsuleBasePoint.z);

            RaycastHit[] newHits = new RaycastHit[rightSideCollisions.Length - 1];
            Array.Copy(rightSideCollisions, 0, newHits, 0, newHits.Length);
            rightSideCollisions = newHits;
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

    //Returns a smaller collider face portion created from part of a collider face.
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

        return returnFace;
    }

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

        if (DEBUG_lineStarts != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.identity;
            for (int i = 0; i < DEBUG_lineEnds.Count; ++i)
            {
                Gizmos.DrawLine(DEBUG_lineStarts[i], DEBUG_lineEnds[i]);
            }
        }
    }

}
