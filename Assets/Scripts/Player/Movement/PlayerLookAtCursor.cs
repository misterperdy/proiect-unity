using TMPro;
using UnityEngine;
using UnityEngine.U2D;

public class PlayerLookAtCursor : MonoBehaviour
{
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    private Camera cam;

    //rotation for bow
    private Animator animator;
    public Transform spineBone;
    public float archeryRotationOffset = 90f;
    public float blendSpeed = 10f;

    private float currentWeight;

    public LayerMask aimLayers;

    private Vector3 targetDirection;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

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

    private void LateUpdate()
    {
        //rotation of torso with the bow animation taht rotaset the torso to the right but we bring it back to be wher eyou are looking
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);
        AnimatorTransitionInfo transInfo = animator.GetAnimatorTransitionInfo(1);

        bool shouldRotate = (stateInfo.IsName("HumanM@BowShot01 - Load") || stateInfo.IsName("HumanM@BowShot01 - Release")) || transInfo.IsName(" -> HumanM@BowShot01 - Load") || transInfo.IsName(" -> HumanM@BowShot01 - Release");

        float targetWeight = shouldRotate ? 1f : 0f;

        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);

        if (currentWeight > 0.01f)
        {
            spineBone.localEulerAngles += new Vector3(0, archeryRotationOffset * currentWeight, 0);
        }
    }
}
