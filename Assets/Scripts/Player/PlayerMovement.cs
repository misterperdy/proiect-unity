using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script to handle basic player movement
public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 8f;
    
    private Rigidbody rb;

    private float moveX;
    private float moveZ;
    private Vector3 moveVector;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); // A,D keys or left,right arrows
        float moveZ = Input.GetAxis("Vertical"); // W,S keys or up,down arrows

        //create a vector with the inputs from the 2 axis
        moveVector = new Vector3(moveX, 0f, moveZ).normalized; // we don't move on Y yet
    }

    //function to handle physics that runs constantly
    void FixedUpdate()
    {
        //move the rigidbody of the player
        rb.MovePosition(rb.position + moveVector * movementSpeed * Time.fixedDeltaTime);
    }
}
