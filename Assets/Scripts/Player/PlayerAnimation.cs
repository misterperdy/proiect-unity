using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        // Animator is on the model child (Axe_Warrior)
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("Animator not found! Make sure the model has the Animator component.");
    }

    public void UpdateMovementAnimations(Vector3 moveVector)
    {
        float speed = moveVector.magnitude;

        animator.SetFloat("speed", speed);
        animator.SetBool("isRunning", speed > 0.1f);
    }
}
