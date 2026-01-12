using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script for basic movement
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
    private PlayerHealth playerHealth;
    private bool isInBossRoom;

    public void SetBossRoomState(bool state)
    {
        isInBossRoom = state;
    }


    private Rigidbody rb;
    private Vector3 moveVector;

    public Animator animator;

    public LayerMask floorLayerMask; // set in inspector

    public float heightOffset = 0.0f;

    [Header("SFX")]
    public AudioClip walkingSfx;
    public AudioClip runningSfx;
    public float walkingSfxVolumeMultiplier = 1f;
    public float runningSfxVolumeMultiplier = 1f;
    public float walkingMoveThreshold = 0.1f;
    private AudioSource walkingSource;

    // Start is called before the first frame update
    void Start()
    {

        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();


        if (walkingSfx == null && MusicManager.Instance != null && MusicManager.Instance.playerWalkingSfx != null)
        {
            walkingSfx = MusicManager.Instance.playerWalkingSfx;
        }
        if (walkingSfx == null) walkingSfx = MusicManager.FindClipByName("fix_sfx_player_is_walking");
        if (walkingSfx == null) walkingSfx = MusicManager.FindClipByName("sfx_player_is_walking");

        if (runningSfx == null && MusicManager.Instance != null && MusicManager.Instance.playerRunningSfx != null)
        {
            runningSfx = MusicManager.Instance.playerRunningSfx;
        }
        if (runningSfx == null) runningSfx = MusicManager.FindClipByName("sfx_player_is_running");
        if (runningSfx == null) runningSfx = MusicManager.FindClipByName("fix_sfx_player_is_running");

        // audio source just for footsteps
        walkingSource = gameObject.AddComponent<AudioSource>();

        walkingSource.playOnAwake = false;
        walkingSource.loop = true;
        walkingSource.spatialBlend = 0f; // 2d sound
        walkingSource.clip = walkingSfx;
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
        float moveX = Input.GetAxisRaw("Horizontal"); // A D keys
        float moveZ = Input.GetAxisRaw("Vertical"); // W S keys


        // create vector from input
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

        if (playerHealth != null && playerHealth.IsHurt)
        {
            if (walkingSource != null && walkingSource.isPlaying) walkingSource.Stop();
            animator.SetBool("isRunning", true);
            speedTimer = 0f;
            movementSpeed = normalSpeed;
            animator.SetBool("isRunning", false);
            return;
        }

        if (isInBossRoom)
        {
            if (walkingSource != null && walkingSource.isPlaying) walkingSource.Stop();
            animator.SetBool("isRunning", true);
            speedTimer = 0f;
            movementSpeed = normalSpeed;
            animator.SetBool("isRunning", false);
            return;
        }








        // set params to animator
        Quaternion flatRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 localMove = Quaternion.Inverse(flatRotation) * moveVector;
        if (localMove.magnitude < 0.05f)
        {
            localMove = Vector3.zero;
        }
        animator.SetFloat("InputX", localMove.x, 0.15f, Time.deltaTime);
        animator.SetFloat("InputZ", localMove.z, 0.15f, Time.deltaTime);

        UpdateWalkingSfx();
    }

    private void UpdateWalkingSfx()
    {
        if (walkingSource == null) return;

        // retry if null
        if (walkingSfx == null)
        {
            if (MusicManager.Instance != null && MusicManager.Instance.playerWalkingSfx != null)
            {
                walkingSfx = MusicManager.Instance.playerWalkingSfx;
            }
            if (walkingSfx == null) walkingSfx = MusicManager.FindClipByName("fix_sfx_player_is_walking");
            if (walkingSfx == null) walkingSfx = MusicManager.FindClipByName("sfx_player_is_walking");
            walkingSource.clip = walkingSfx;
        }

        // retry if null
        if (runningSfx == null)
        {
            if (MusicManager.Instance != null && MusicManager.Instance.playerRunningSfx != null)
            {
                runningSfx = MusicManager.Instance.playerRunningSfx;
            }
            if (runningSfx == null) runningSfx = MusicManager.FindClipByName("sfx_player_is_running");
            if (runningSfx == null) runningSfx = MusicManager.FindClipByName("fix_sfx_player_is_running");
        }

        if (walkingSfx == null) return;

        // stop if paused
        if (Time.timeScale == 0f)
        {
            if (walkingSource.isPlaying) walkingSource.Stop();
            return;
        }

        bool isMoving = moveVector.magnitude > walkingMoveThreshold;

        bool isRunning = movementSpeed > normalSpeed + 0.01f;
        AudioClip desiredClip = (isRunning && runningSfx != null) ? runningSfx : walkingSfx;
        float desiredMultiplier = isRunning ? runningSfxVolumeMultiplier : walkingSfxVolumeMultiplier;

        if (isMoving)
        {
            // keep volume sync
            float baseVol = (MusicManager.Instance != null) ? MusicManager.Instance.sfxVolume : 1f;
            walkingSource.volume = baseVol * desiredMultiplier;

            if (walkingSource.clip != desiredClip)
            {
                bool wasPlaying = walkingSource.isPlaying;
                walkingSource.Stop();
                walkingSource.clip = desiredClip;
                if (wasPlaying) walkingSource.Play();
            }
            if (!walkingSource.isPlaying) walkingSource.Play();
        }
        else
        {
            if (walkingSource.isPlaying) walkingSource.Stop();
        }
    }

    // physics function
    void FixedUpdate()
    {
        // move rigidbody
        rb.velocity = new Vector3(moveVector.x * movementSpeed, rb.velocity.y, moveVector.z * movementSpeed);

        // raycast to check ground and adjust Y height
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