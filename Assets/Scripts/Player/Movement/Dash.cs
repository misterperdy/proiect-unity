using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Dash : MonoBehaviour
{
    private Rigidbody _rigidBody;
    public TrailRenderer _trailRenderer;
    private PlayerHealth _playerHealth;
    private Animator animator;

    [Header("Dashing")]
    [SerializeField] private float _dashVelocity = 15f; // Am marit putin valoarea, 1.2 e foarte mic pt MovePosition
    [SerializeField] private float _dashingTime = 0.2f;
    [SerializeField] private float _dashCooldown = 1f;
    private Vector3 _dashingDir;
    private bool _isDashing = false;
    private bool _canDash = true;


    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();

        if(_trailRenderer == null)
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        _playerHealth = GetComponent<PlayerHealth>();
        // Atentie: Daca scriptul e pe parinte si animatorul pe copil, foloseste GetComponentInChildren
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (PauseManager.IsPaused) return;

        var dashInput = Input.GetButtonDown("Dash");

        // 1. Verificam Input-ul
        if (dashInput && _canDash)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            _dashingDir = new Vector3(x, 0, z).normalized;

            // OPTIONAL: Daca nu apasa nimic, dam dash in fata (transform.forward)
            // Sau daca vrei sa nu faca nimic daca nu apasa, lasi un return aici.
            if (_dashingDir == Vector3.zero)
            {
                // Varianta A: Dash in fata daca sta pe loc
                _dashingDir = transform.forward;

                // Varianta B (cea din codul tau vechi): Nu face dash
                // return; 
            }

            // Setam parametrii pentru Blend Tree ca sa stie animatia directia
            animator.SetFloat("InputX", x);
            animator.SetFloat("InputZ", z);

            StartCoroutine(PerformDash());
        }
    }

    // Folosim FixedUpdate pentru miscarea fizica (Rigidbody)
    void FixedUpdate()
    {
        if (_isDashing)
        {
            // Aici mutam efectiv jucatorul
            _rigidBody.MovePosition(_rigidBody.position + _dashingDir * _dashVelocity * Time.fixedDeltaTime);
        }
    }

    private IEnumerator PerformDash()
    {
        _canDash = false;
        _isDashing = true;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.playerDashSfx);
        }

        // AICI ESTE FIX-UL: Declansam animatia o singura data, la inceput
        animator.SetTrigger("t_dash");

        _trailRenderer.emitting = true;

        // Dezactivam coliziunile si damage-ul
        Physics.IgnoreLayerCollision(7, 8, true);
        if (_playerHealth != null) _playerHealth.canTakeDamage = false;

        // Asteptam cat dureaza dash-ul (timp in care FixedUpdate il muta)
        yield return new WaitForSeconds(_dashingTime);

        // Oprim Dash-ul
        _isDashing = false;
        _trailRenderer.emitting = false;
        _rigidBody.velocity = Vector3.zero; // Oprim inertia

        // Reactivam coliziunile si damage-ul
        Physics.IgnoreLayerCollision(7, 8, false);
        if (_playerHealth != null) _playerHealth.canTakeDamage = true;

        // Asteptam cooldown-ul
        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }
}