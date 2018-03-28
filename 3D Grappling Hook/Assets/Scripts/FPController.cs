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
    float moveSpeed = 5f;

    // Jump/Gravity related variables
    float minJumpHeight = .25f; // Lowest we may jump
    float maxJumpHeight = 4f; // Highest we may jump
    float minJumpVel;
    float maxJumpVel;
    float timeToJumpApex = .4f; // Jump higher/longer with bigger values
    float velY;                 // Stores our velocity in the y direction
    float gravity;

    // Grappling hook related
    private Vector3 newGrappleHookPos;
    float maxHookDist = 50f;
    private float grappleMoveSpeed = 3f;
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

        // Get "gravity"
        velY += gravity * Time.deltaTime;

        // Store all our movement
        velocity = transform.TransformDirection(moveDir) * moveSpeed + Vector3.up * velY;

        // Apply movement
        if(isBeingMovedByHookShot == false)
        {
            cc.Move(velocity * Time.deltaTime);
        }

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
        Debug.DrawLine(cameraT.position, cameraT.forward * maxHookDist);

        // If the player presses the left mouse button or other set button
        if(Input.GetButtonDown("Fire1"))
        {
            RaycastHit hit;
            if(Physics.Raycast(cameraT.position, cameraT.forward, out hit, maxHookDist))
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

        if(hookAttached)
        {
            isBeingMovedByHookShot = true;
            velocity = Vector3.MoveTowards(transform.position, newGrappleHookPos, grappleMoveSpeed * Time.deltaTime);
            cc.Move(-velocity * Time.deltaTime);
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
}
