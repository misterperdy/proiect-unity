using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Dash : MonoBehaviour
{
    private Rigidbody _rigidBody;
    private TrailRenderer _trailRenderer;


    [Header("Dashing")]
    [SerializeField] private float _dashVelocity = 1.2f;
    [SerializeField] private float _dashingTime = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;
    private Vector3 _dashingDir;
    private bool _isDashing = false;
    private bool _canDash = true;


    // Start is called before the first frame update
    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _trailRenderer = GetComponent<TrailRenderer>();

    }


    // Update is called once per frame
    void Update()
    {
        var dashInput = Input.GetButtonDown("Dash");

        if (dashInput && _canDash)
        {
            _isDashing = true;
            _canDash = false;
            _trailRenderer.emitting = true;
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            _dashingDir = new Vector3(horizontal, 0, vertical);

            if (_dashingDir == Vector3.zero)
            {
                _canDash = false;
            }

            StartCoroutine(StopDashing());
        }

        if (_isDashing)
        {
            _rigidBody.MovePosition(_rigidBody.position + _dashingDir.normalized * _dashVelocity * Time.fixedDeltaTime);
            return;
        }

    }

    private IEnumerator StopDashing()
    {
        yield return new WaitForSeconds(_dashingTime);
        _isDashing = false;
        _trailRenderer.emitting = false;
        _rigidBody.velocity = Vector3.zero;
        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }
}
