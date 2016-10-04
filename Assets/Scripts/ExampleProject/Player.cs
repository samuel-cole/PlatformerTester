using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    CharacterController controller;
    public float speed;
    public float jumpSpeed;
    public float gravity;
    float vSpeed;

    // Use this for initialization
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
	
	// Update is called once per frame
	void Update ()
    {

        //Handle movement.
        Vector3 moveDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
            moveDirection += Vector3.left * speed;
        if (Input.GetKey(KeyCode.D))
            moveDirection += Vector3.right * speed;
        if (controller.isGrounded)
        {
            vSpeed = 0;
            if (Input.GetKey(KeyCode.Space))
                vSpeed = jumpSpeed;
        }

        vSpeed -= gravity * Time.deltaTime;
        moveDirection = new Vector3(moveDirection.x, vSpeed, 0);
                
        controller.Move(moveDirection * Time.deltaTime);
    }
}
