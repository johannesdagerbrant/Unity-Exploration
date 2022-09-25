using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Settings
{
    [System.Serializable]
    public class moveSettings
    {
        [Range(0.001f, 1000f)]
        public float speed;
        [Range(0.001f, 1000f)]
        public float radius;
    }
    [System.Serializable]
    public class AimSettings
    {
        [Range(1f, 1000f)]
        public float horizontalSpeed;
        [Range(1f, 1000f)]
        public float verticalSpeed;
        [Range(0f, 90f)]
        public float upCap = 75f;
        [Range(-90f, 0f)]
        public float downCap = -75f;
    }
    public moveSettings move;
    public AimSettings aim;

}
public abstract class Locomotion
{
    protected Settings settings;
    protected Transform tm, predictionTm;
    protected Vector2 aimVelocity, desiredAim;
    protected Vector3 moveDirection, desiredMovement, desiredPosition;
    protected RaycastHit raycastHit;
    protected Vector3 lastRayNormal;
    protected LayerMask groundLayer;


    protected Locomotion()
    {
        groundLayer = LayerMask.GetMask("Ground");
        lastRayNormal = Vector3.up;
    }

    public void Update(float dt)
    {
        ProcessAimImput(dt);
        ProcessMoveInput(dt);
        UpdateRaycasts();
    }

    public Vector3 GetDesiredPosition()
    {
        return desiredPosition;
    }
    public Vector2 GetDesiredAim()
    {
        return desiredAim;
    }
    public abstract bool IsLocomotionAllowed();

    public abstract void SetTransform(float dt);
    protected abstract void ProcessAimImput(float dt);
    protected abstract void ProcessMoveInput(float dt);
    protected abstract void UpdateRaycasts();
}

public class Walking : Locomotion
{
    public Walking(Transform tm, Settings settings) : base()
    {
        this.tm = tm;
        this.settings = settings;
        moveDirection = tm.forward;
    }
    public override bool IsLocomotionAllowed()
    {
        return raycastHit.collider != null;
    }

    protected override void ProcessAimImput(float dt)
    {
        Vector2 aimInput = Manager.input.GetLookInput();
        aimVelocity.x = aimInput.x * settings.aim.horizontalSpeed * dt;
        aimVelocity.y = aimInput.y * settings.aim.verticalSpeed * dt;
        desiredAim += aimVelocity;

        if (desiredAim.x < -180)
            desiredAim.x += 360f;
        if (desiredAim.x > 180)
            desiredAim.x -= 360f;

        desiredAim.y = Mathf.Max(desiredAim.y, settings.aim.downCap);
        desiredAim.y = Mathf.Min(desiredAim.y, settings.aim.upCap);
    }
    protected override void ProcessMoveInput(float dt)
    {
        Vector2 moveInput = Manager.input.GetMoveInput();
        Vector3 moveForward = tm.forward * moveInput.y;
        Vector3 moveSides = tm.right * moveInput.x;

        desiredMovement = (moveForward + moveSides) * settings.move.speed * dt;
        desiredPosition = tm.position + desiredMovement;
        if (desiredMovement.magnitude > 0f)
        {
            moveDirection = desiredMovement.normalized;
        }
    }

    protected override void UpdateRaycasts()
    {
        // First make an atempt to raycast straight forward, slightly above the desired position.
        bool isForwardHit = Physics.Raycast(tm.position + tm.up * settings.move.radius, moveDirection, out raycastHit, desiredMovement.magnitude, groundLayer);
        Debug.DrawRay(tm.position + tm.up * settings.move.radius, desiredMovement, Color.red);
        if (isForwardHit)
        {
            //Debug.Log("isForwardHit");
            return;
        }

        // Then make an atempt to raycast forward and down past the desired position.
        bool isForwardDownHit = Physics.Raycast(desiredPosition + (tm.up - moveDirection) * settings.move.radius, moveDirection - tm.up, out raycastHit, settings.move.radius*2, groundLayer);
        Debug.DrawRay(desiredPosition + (tm.up - moveDirection) * settings.move.radius, moveDirection - tm.up * settings.move.radius*2, Color.green);
        if (isForwardDownHit)
        {
            //Debug.Log("isForwardDownHit");
            return;
        }

        // Then make an atempt to raycast backward and down past the desired position.
        bool isBackwardDownHit = Physics.Raycast(desiredPosition + (tm.up + moveDirection) * settings.move.radius, -(moveDirection + tm.up), out raycastHit, settings.move.radius*2, groundLayer);
        Debug.DrawRay(desiredPosition + (tm.up + moveDirection) * settings.move.radius, -(moveDirection + tm.up) * settings.move.radius * 2, Color.blue);
        if (isBackwardDownHit)
        {
            Debug.Log("isBackwardDownHit");
            return;
        }
    }

    public override void SetTransform(float dt)
    {
        Vector3 oldPosition = tm.position;
        Vector3 newPosition = Vector3.MoveTowards(oldPosition,  raycastHit.point, settings.move.speed * dt);

        float normalAngle = Vector3.Angle(lastRayNormal, raycastHit.normal);
        Vector3 normalCross = Vector3.Cross(lastRayNormal, raycastHit.normal);
        lastRayNormal = raycastHit.normal;
        Vector3 newForward = Quaternion.AngleAxis(normalAngle, normalCross) * tm.forward;
        Quaternion newRotation = Quaternion.LookRotation(newForward, raycastHit.normal) * Quaternion.AngleAxis(aimVelocity.x, Vector3.up);
        tm.SetPositionAndRotation(newPosition, newRotation);
    }
}