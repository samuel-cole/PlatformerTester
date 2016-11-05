using UnityEngine;
using System.Collections;

/// <summary>
/// Player class, used in the example platformer game used for demonstrating the platformer tester.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// The character controller used for the player.
    /// </summary>
    CharacterController controller;

    /// <summary>
    /// The player's movement speed.
    /// </summary>
    public float speed;
    /// <summary>
    /// The initial vertical speed that the player gains upon jumping.
    /// </summary>
    public float jumpSpeed;
    /// <summary>
    /// The rate at which the player's vertical speed decreases.
    /// </summary>
    public float gravity;
    /// <summary>
    /// The player's current vertical speed.
    /// </summary>
    float vSpeed;

    /// <summary>
    /// Sets up the character controller for the player.
    /// </summary>
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
	
    /// <summary>
    /// Handles movement and input for the player, including moving the player left or right based on player input,
    /// and managing the effects of gravity/jumping on vertical speed.
    /// </summary>
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
