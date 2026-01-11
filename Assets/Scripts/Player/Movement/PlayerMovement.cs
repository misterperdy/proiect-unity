using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script to handle basic player movement
public class PlayerMovement : MonoBehaviour
{
    public float normalSpeed = 8f;
    public float runningSpeed = 16f;
    public float movementSpeed;
    public float speedCheckCooldown = 3f;
    private float speedTimer = 0f;
    public float normalAnimSpeed = 1f;
    public float fastAnimSpeed = 2f;

    private bool isActionPressed;


    private Rigidbody rb; 
    private Vector3 moveVector;

    public Animator animator;

    public LayerMask floorLayerMask; // asign in inspector

    public float heightOffset = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
        rb = GetComponent<Rigidbody>();
    }
    void ReadInput()
    {
        isActionPressed =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetMouseButton(0);
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal"); // A,D keys or left,right arrows
        float moveZ = Input.GetAxisRaw("Vertical"); // W,S keys or up,down arrows

        
        //create a vector with the inputs from the 2 axis
        moveVector = new Vector3(moveX, 0f, moveZ).normalized;
        ReadInput();

        bool isActionPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(0);


        speedTimer += Time.deltaTime;

        if (isActionPressed)
        {
            animator.SetBool("isRunning", false);
            movementSpeed = normalSpeed;
            speedTimer = 0f;
        }
        else if (speedTimer >= speedCheckCooldown)
        {
            animator.SetBool("isRunning", true);
            movementSpeed = runningSpeed;
        }






        //pass variables to animator
        Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 localMove = Quaternion.Inverse(flatRotation) * moveVector;
        if (localMove.magnitude < 0.05f)
        {
            localMove = Vector3.zero;
        }
        animator.SetFloat("InputX", localMove.x, 0.15f, Time.deltaTime);
        animator.SetFloat("InputZ", localMove.z, 0.15f, Time.deltaTime);
    }

    //function to handle physics that runs constantly
    void FixedUpdate()
    {
        //move the rigidbody of the player
        rb.velocity = new Vector3(moveVector.x * movementSpeed, rb.velocity.y, moveVector.z * movementSpeed);

        //cast a forward ray that checks if its something the player should go over with, and handles Y changing
        RaycastHit hit;

        Vector3 castOrigin = transform.position;
        if (moveVector.magnitude > 0.1f)
        {
            Vector3 dir = new Vector3(moveVector.x, 0, moveVector.z).normalized;
            castOrigin += dir * 0.4f;
        }

        castOrigin.y += 2.0f;

        Debug.DrawRay(castOrigin, Vector3.down * 4.0f, Color.red);

        if (Physics.Raycast(castOrigin, Vector3.down, out hit, 4.0f, floorLayerMask))
        {
            float targetY = hit.point.y + heightOffset;

            if (Mathf.Abs(transform.position.y - targetY) > 0.01f)
            {
                Vector3 newPos = transform.position;
                newPos.y = Mathf.Lerp(newPos.y, targetY, Time.deltaTime * 15f);
                transform.position = newPos;
            }
        }
    }
}
