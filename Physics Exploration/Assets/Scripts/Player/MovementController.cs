using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [System.Serializable]
    public class Ground
    {
        [Range(1f, 1000f)]
        public float moveSpeed;
        [Range(1f, 1000f)]
        public float raycastArea;
        [Range(0f, 1000f)]
        public float raycastLength;
        [Range(2, 10)]
        public int raycastAmount = 8;
    }

    [System.Serializable]
    public class Air
    {
        [Range(1f, 1000f)]
        public float fallingMoveSpeed;
        [Range(1f, 100000f)]
        public float fallingMaxSpeed;
        [Range(0f, 100f)]
        public float jumpForce;
    }
    [System.Serializable]
    public class Aim
    {

        [Range(1f, 1000f)]
        public float lookHorizontalSpeed;
        [Range(1f, 1000f)]
        public float lookVerticalSpeed;
        [Range(0f, 90f)]
        public float lookUpCap = 75f;
        [Range(-90f, 0f)]
        public float lookDownCap = -75f;

    }
    public Ground ground;
    public Air air;
    public Aim aim;
    public bool drawRaycasts = false;


    public Vector2 desiredLook;
    public RaycastHit moverHit, rigHit;



    private Vector2 lookVelocity;

    private Transform rigTm;
    private SpiderRig rig;
    private InputManager im;
    private Vector3 moveDirection, desiredMovement, desiredPosition, lastDesiredPosition, lastMoverNormal;
    private LayerMask groundLayer;
    private bool grounded = false;
    private bool jumpedOnFrame = false;
    private bool landedOnFrame = false;

    private void Awake()
    {
        rigTm = new GameObject().transform;
        rigTm.tag = "PlayerRig";
    }
    void Start()
    {
        im = GameObject.FindWithTag("Manager").GetComponent<InputManager>();
        rig = GetComponent<SpiderRig>();
        rig.Init(rigTm);
        groundLayer = LayerMask.GetMask("Ground");
        moveDirection = transform.forward;
        lastMoverNormal = transform.up;

    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        UpdateLook(dt);
        UpdateMovement(dt);
        SetMoverTransform();
        SetRigTransform();
        rig.UpdateAnimation(dt, grounded);
        Draw.Locator(transform.position, 0.5f, Color.blue);
    }

    public bool IsGrounded()
    {
        return grounded;
    }
    private void UpdateMovement(float dt)
    {
        Vector2 moveInput = im.GetMoveInput();
        Vector3 moveForward = transform.forward * moveInput.y * dt;
        Vector3 moveSides = transform.right * moveInput.x * dt;
        if (grounded)
        {
            float jumpInput = im.GetJumpInput();
            desiredMovement = (moveForward + moveSides) * ground.moveSpeed;
            if (jumpInput > 0f)
            {
                float angularUpDiff = Vector3.Angle(Vector3.up, moverHit.normal) / 180f;
                Vector3 upDirection = Vector3.Lerp(Vector3.up, moverHit.normal, angularUpDiff);
                desiredMovement += upDirection * jumpInput * air.jumpForce;
                Draw.Locator(transform.position + desiredMovement, 1f);
                jumpedOnFrame = true;
            }
            lastDesiredPosition = desiredPosition;
            desiredPosition = transform.position + desiredMovement;
        }
        else
        {
            Vector3 positionBeforeUpdate = desiredPosition;
            desiredMovement = (moveForward + moveSides) * air.fallingMoveSpeed; // movement
            desiredMovement += Physics.gravity * dt; // gravity
            desiredMovement += (desiredPosition - lastDesiredPosition);
            
            desiredPosition += desiredMovement * 0.98f; // Continuous movement with friction
            lastDesiredPosition = positionBeforeUpdate;
        }
        if (desiredMovement.magnitude > 0f)
            moveDirection = desiredMovement.normalized;

    }
    private void UpdateLook(float dt)
    {
        Vector2 lookInput = im.GetLookInput();
        lookVelocity.x = lookInput.x * aim.lookHorizontalSpeed * dt;
        lookVelocity.y = lookInput.y * aim.lookVerticalSpeed * dt;
        desiredLook += lookVelocity;

        if (desiredLook.x < -180)
            desiredLook.x += 360f;
        if (desiredLook.x > 180)
            desiredLook.x -= 360f;

        desiredLook.y = Mathf.Max(desiredLook.y, aim.lookDownCap);
        desiredLook.y = Mathf.Min(desiredLook.y, aim.lookUpCap);
    }

    private void SetMoverTransform()
    {
        // Straight forward to detect when moving into walls
        Debug.DrawRay(transform.position + transform.up * rig.pose.bend, desiredMovement, Color.cyan);
        Physics.Raycast(transform.position + transform.up * rig.pose.bend, moveDirection, out moverHit, desiredMovement.magnitude, groundLayer);

        if (moverHit.collider == null)
        {
            // forward+down, then backward+down. Seem to catch all sorts of variation in steepness. 
            Vector3[] rayLocalOrigins = {
            (transform.up - moveDirection).normalized,
            (transform.up + moveDirection).normalized
        };
            float halfRaycastLength = ground.raycastLength * 0.5f;
            foreach (Vector3 rayLocalOrigin in rayLocalOrigins)
            {
                Vector3 rayStart = desiredPosition + rayLocalOrigin * halfRaycastLength;
                Vector3 rayDirection = (desiredPosition - rayStart).normalized;
                Debug.DrawRay(rayStart, rayDirection * ground.raycastLength, Color.red);
                if (Physics.Raycast(rayStart, rayDirection, out moverHit, ground.raycastLength, groundLayer))
                    break;
            }
        }
        grounded = moverHit.collider != null && !jumpedOnFrame;
        // Apply the new mover transform
        if (grounded)
        {
            transform.position = moverHit.point;
            float normalAngle = Vector3.Angle(lastMoverNormal, moverHit.normal);
            Vector3 normalCross = Vector3.Cross(lastMoverNormal, moverHit.normal);
            lastMoverNormal = moverHit.normal;
            Vector3 newForward = Quaternion.AngleAxis(normalAngle, normalCross) * transform.forward;
            moveDirection = Quaternion.AngleAxis(normalAngle, normalCross) * moveDirection;
            transform.rotation = Quaternion.LookRotation(newForward, moverHit.normal) * Quaternion.AngleAxis(lookVelocity.x, Vector3.up);
        }
        else
        {
            transform.position = desiredPosition;
            float normalAngle = Vector3.Angle(lastMoverNormal, Vector3.up);
            Vector3 normalCross = Vector3.Cross(lastMoverNormal, Vector3.up);
            lastMoverNormal = Vector3.up;
            Vector3 newForward = Quaternion.AngleAxis(normalAngle, normalCross) * transform.forward;
            moveDirection = Quaternion.AngleAxis(normalAngle, normalCross) * moveDirection;
            transform.rotation = Quaternion.LookRotation(newForward, moverHit.normal) * Quaternion.AngleAxis(lookVelocity.x, Vector3.up);
            jumpedOnFrame = false;
        }
    }

    private void SetRigTransform()
    {
        Vector3 rigPosition = Vector3.zero;
        int hitAmount = 0;
        Vector3 rigNormal = Vector3.zero;
        float halfRaycastLength = ground.raycastLength * 0.5f;
        Vector3 offset = moveDirection * ground.raycastArea;
        Vector3 rayStart, rayDirection;
        for (int i = 0; i < ground.raycastAmount; i++)
        {
            rayStart = desiredPosition + transform.up;
            rayDirection = ((rayStart - offset) - rayStart).normalized;
            if (Physics.Raycast(rayStart, rayDirection, out rigHit, ground.raycastLength, groundLayer) && drawRaycasts)
                Debug.DrawRay(rayStart, rayDirection * ground.raycastLength, Color.red);
            else
            {
                rayStart = desiredPosition + transform.up * halfRaycastLength;
                rayDirection = ((desiredPosition - offset - rayStart) - transform.up * halfRaycastLength).normalized;
                if (Physics.Raycast(rayStart, rayDirection, out rigHit, ground.raycastLength, groundLayer) && drawRaycasts)
                    Debug.DrawRay(rayStart, rayDirection * ground.raycastLength, Color.green);
                else
                {
                    rayStart = (desiredPosition  - offset) + transform.up * halfRaycastLength;
                    rayDirection = ((desiredPosition - rayStart) - transform.up * halfRaycastLength).normalized;
                    if (Physics.Raycast(rayStart, rayDirection, out rigHit, ground.raycastLength, groundLayer) && drawRaycasts)
                        Debug.DrawRay(rayStart, rayDirection * ground.raycastLength, Color.blue);
                }
            }
            if (rigHit.collider != null)
            {
                hitAmount++;
                rigNormal += rigHit.normal;
                rigPosition += rigHit.point;
            }
            else
                rigNormal += transform.up;
            offset = Quaternion.AngleAxis(360f / ground.raycastAmount, transform.up) * offset;
        }
        Quaternion rigRotation;
        if (hitAmount > 0)
        {
            rigPosition = new Vector3(rigPosition[0] / hitAmount, rigPosition[1] / hitAmount, rigPosition[2] / hitAmount);
            rigNormal.Normalize();
            rigRotation = Quaternion.LookRotation(rigNormal, transform.forward) * Quaternion.Euler(-90f, 0f, 180f);
        }
        else
        {
            rigPosition = transform.position;
            rigRotation = transform.rotation;
        }
        rigTm.SetPositionAndRotation(rigPosition, rigRotation);
    }
}
