using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script to handle basic player movement
public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 8f;
    
    private Rigidbody rb;
    private Vector3 moveVector;

    public Animator animator;


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
        moveVector = new Vector3(moveX, 0f, moveZ).normalized;

        //pass variables to animator
        Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 localMove = Quaternion.Inverse(flatRotation) * moveVector;
        if (localMove.magnitude < 0.05f)
        {
            localMove = Vector3.zero;
        }
        animator.SetFloat("InputX", localMove.x, 0.01f, Time.deltaTime);
        animator.SetFloat("InputZ", localMove.z, 0.01f, Time.deltaTime);
    }

    //function to handle physics that runs constantly
    void FixedUpdate()
    {
        //move the rigidbody of the player
        rb.velocity = new Vector3(moveVector.x * movementSpeed, rb.velocity.y, moveVector.z * movementSpeed);
    }
}
