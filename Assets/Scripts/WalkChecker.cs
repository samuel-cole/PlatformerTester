using UnityEngine;
using System;
using System.Collections.Generic;

public class WalkChecker : MonoBehaviour
{
    class ColliderFace
    {
        //public Collider collider;
        //public Vector3 localPosition;
        public Vector3 position;
        //As this is designed for 2.5d games, only a float is required for rotation (the z axis).
        //Measured in degrees.
        public float rotation;
        public float length;
    }
    

    public CharacterController player;


    List<ColliderFace> walkableFaces;

    public void RemoveDebugSurfaces()
    {
        if (walkableFaces != null)
            walkableFaces.Clear();
    }

    public void DisplayDebugSurfaces()
    {
        if (walkableFaces == null)
            walkableFaces = new List<ColliderFace>();
        else
            walkableFaces.Clear();

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
    List<ColliderFace> CheckCollisionsWithFace(ColliderFace a_face)
    {
        List<ColliderFace> returnFaces = new List<ColliderFace>();

        //Placeholder for logic.
        returnFaces.Add(a_face);

        //TODO: fill in sweep start position, direction, and distance.
        Vector3 sweepStartPosition = Vector3.zero;
        Vector3 direction = Vector3.right;
        float checkDistance = 0.0f;

        float pointHeight = player.height / 2.0f - player.radius;
        Vector3 capsuleBasePoint = sweepStartPosition + player.center - new Vector3(0, pointHeight, 0);
        Vector3 capsuleTopPoint = capsuleBasePoint + new Vector3(0, pointHeight * 2.0f, 0);

        RaycastHit[] collisions = Physics.CapsuleCastAll(capsuleBasePoint, capsuleTopPoint, player.radius, direction, checkDistance);
        foreach (RaycastHit hit in collisions)
        {
            //TODO: Split the face.
        }

        return returnFaces;
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
                Gizmos.matrix = Matrix4x4.TRS(walkableFaces[i].position, Quaternion.Euler(new Vector3(0, 0, walkableFaces[i].rotation)), new Vector3(walkableFaces[i].length, 0.1f, 1.0f));
                Gizmos.DrawCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            }

        }

    }

}
