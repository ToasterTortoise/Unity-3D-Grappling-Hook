using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A first person controller that uses a character controller.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    // Camera related
    public Vector2 mouseSensitivity;   // How sensitive is the camera
    public Vector2 verticalLookMinMax; // Limit to how far up and down we can look
    public bool lockCursor; // Do we want to hide the mouse cursor?
    Transform cameraT;
    float pitch;

    // Movement related variables
    float walkSpeed = 3f;
    float runSpeed  = 5f;
    float speedSmoothTime = 0.1f; // How long it takes to stop moving
    float speedSmoothVelocity;
    float currentSpeed;
    bool walking = false;

    // Jump/Gravity related variables
    float minJumpHeight = .25f; // Lowest we may jump
    float maxJumpHeight = 4f; // Highest we may jump
    float minJumpVel;
    float maxJumpVel;
    float timeToJumpApex = .4f; // Jump higher/longer with bigger values
    float velY;                 // Stores our velocity in the y direction
    float gravity;

    /// <summary>
    /// Control how much the player can control their movement in the air.
    /// </summary>
    [Tooltip("How much the player can control their movement in the air.")]
    [Range(0f, 1f)]public float airControlPercent;

    // Grappling hook related
    public Vector3 newGrappleHookPos;
    float maxHookDst = 50f;
    private float grappleMoveSpeed = 10f;
    bool hookAttached = false;
    bool isBeingMovedByHookShot = false;

    // Other
    private Vector3 velocity; // Our movement vector
    private Vector3 oldDir;   // Our old look direction

    private CharacterController cc;

    // Use this for initialization
    void Start ()
    {
        cc = GetComponent<CharacterController>();

        // Initialize jump and gravity
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVel = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVel = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        Debug.LogFormat("Gravity: {0}, Jump Velocity: {1}", gravity, maxJumpVel);

        // If camera is null, find the one inside our transform
        if(cameraT == null)
        {
            cameraT = GetComponentInChildren<Camera>().transform;
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        HandleCamera();
        MovePlayer();
        HandleHookShot();
        OnJumpInput();
	}

    /// <summary>
    /// How the player should move, not related to how the player should move with the hook shot.
    /// </summary>
    void MovePlayer()
    {
        // Take input and normalize it
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        walking = Input.GetKey(KeyCode.R); // TODO: Make walk input

        // Get our wanted speed and then smooth it
        float targetSpeed = ((walking) ? walkSpeed : runSpeed) * moveDir.magnitude;
        currentSpeed =
            Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

        if(isBeingMovedByHookShot == false)
        {
            // Get "gravity"
            velY += gravity * Time.deltaTime;

            // Store all our movement
            velocity = transform.TransformDirection(moveDir) * currentSpeed + Vector3.up * velY;

            // Apply movement
            cc.Move(velocity * Time.deltaTime);
        }

        // Helps prevent animation when moving into walls
        currentSpeed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;

        oldDir = moveDir;

        if (cc.isGrounded)
        {
            velY = 0;
        }
        else
        {
            moveDir = oldDir;
        }
    }

    /// <summary>
    /// Handles hookshot stuff
    /// </summary>
    void HandleHookShot()
    {
        Debug.DrawLine(cameraT.position, cameraT.forward * maxHookDst, Color.red);

        // TODO: Convert this to a hook object?
        // If the player presses the left mouse button or other set button
        if(Input.GetButtonDown("Fire1"))
        {
            // Reset hookshoot if we're already being moved
            if(isBeingMovedByHookShot == true)
            {
                ReturnHook();
            }

            RaycastHit hit;

            // TODO: Hand/fire position?
            if(Physics.Raycast(transform.position, cameraT.forward, out hit, maxHookDst))
            {
                // If our ray hit something, our hook has attached to something
                hookAttached = true;
                newGrappleHookPos = hit.point;
            }
            else
            {
                hookAttached = false;
            }
        }

        // TODO: Allow swinging?
        if(hookAttached)
        {
            isBeingMovedByHookShot = true;

            float dstToHookPoint = Vector3.Distance(transform.position, newGrappleHookPos);
            Vector3 moveTowardsVector = 
                Vector3.MoveTowards(
                    transform.position, 
                    newGrappleHookPos, 
                    grappleMoveSpeed // Make speed consistent
                );

            // Transform the point we hit to world space
            moveTowardsVector = transform.InverseTransformPoint(moveTowardsVector);

            // Then correct the direction
            moveTowardsVector = transform.TransformDirection(moveTowardsVector);

            // Return the hook if it's too close
            if (dstToHookPoint < 1f)
            {
                ReturnHook();
            }

            cc.Move(moveTowardsVector * Time.deltaTime);

            Debug.DrawLine(transform.position, newGrappleHookPos, Color.green); // Draw a line to where we hit
        }
    }

    /// <summary>
    /// Handles the camera.
    /// </summary>
    void HandleCamera()
    {
        // Take the mouse input
        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity.x);
        pitch += mouseInput.y * mouseSensitivity.y;
        pitch = ClampAngle(pitch, verticalLookMinMax.x, verticalLookMinMax.y);
        Quaternion yQuaternion = Quaternion.AngleAxis(pitch, Vector3.left);
        cameraT.localRotation = yQuaternion;
    }

    /// <summary>
    /// Handles how the character should jump in relation to player input.
    /// </summary>
    void OnJumpInput()
    {
        if(Input.GetButtonDown("Jump"))
        {
            // TODO: What about sliding down slopes?
            if (cc.isGrounded)
            {
                velY = maxJumpVel;
            }
        }

        // When the player releases the jump early, they will start falling
        else if(Input.GetButtonUp("Jump") && velY > minJumpVel)
        {
            velY = minJumpVel;
        }

        // If our hook is attached and we press jump, return hook
        else if(Input.GetButtonUp("Jump") && hookAttached == true)
        {
            ReturnHook();
        }
    }

    /// <summary>
    /// Resets hook variables.
    /// </summary>
    void ReturnHook()
    {
        hookAttached = false;
        isBeingMovedByHookShot = false;
    }

    /// <summary>
    /// Prevent going over rotation values.
    /// </summary>
    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
            angle += 360f;
        if (angle > 360f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if (cc.isGrounded)
        {
            return smoothTime;
        }

        if (airControlPercent == 0)
        {
            return float.MaxValue;
        }

        return smoothTime / airControlPercent;
    }
}
