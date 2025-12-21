using TMPro;
using UnityEngine;

public class PlayerLookAtCursor : MonoBehaviour
{
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    private Camera cam;

    public LayerMask aimLayers;

    private Vector3 targetDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimLayers))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = transform.position.y;

            targetDirection = targetPosition - transform.position;
        }
    }

    private void FixedUpdate()
    {
        //apply physics here, rotate Rigidbody instead of transform
        if(targetDirection!= Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion nextRotation = Quaternion.Lerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(nextRotation);
        }
    }
}
