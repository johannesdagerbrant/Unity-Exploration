using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stride
{
    [Range(0f, 2f)]
    public float duration;
    [Range(0f, 100f)]
    public float durationVelocityMultiplier;
    [Range(0f, 100f)]
    public float durationAngularVelocityMultiplier = 0.5f;
    [Range(0.1f, 100f)]
    public float targetGoalVelocityMultiplier = 15f;
    [Range(0.01f, 1f)]
    public float settleTime = 0.5f;
    [Range(0f, 100f)]
    public float length = 2f;
    [Range(0f, 100f)]
    public float lift = 2f;
    public AnimationCurve liftCurve;
    [Range(0f, 100f)]
    public float pull = 2f;
    public AnimationCurve pullCurve;
}
public class Leg
{
    private PlaneFABRIK ik;
    private Vector3 localOrigin, worldOrigin, localTargetGoal, worldTargetGoal, worldTarget, lastWorldTarget;
    private Vector3 smoothNormalFactor, poleVectorVelocity = Vector3.zero;
    private Transform target, body;
    private Anatomy anatomy;
    private Stride stride;
    private float strideTimer, relativeStrideLift, relativeStridePull;
    private bool grounded;
    private float settleTimer, strideSettleTime;
    private RaycastHit hit;
    private LayerMask groundLayer;
    private Transform[] meshes;


    public Leg(Vector3 localOrigin, Vector3 localTargetGoal, Transform target, Transform body, Anatomy anatomy, Stride stride)
    {
        this.anatomy = anatomy;
        this.stride = stride;
        this.target = target;
        this.body = body;
        this.localOrigin = localOrigin;
        this.localTargetGoal = localTargetGoal;
        worldOrigin = body.TransformPoint(localOrigin);
        worldTargetGoal = worldOrigin + body.rotation * localTargetGoal;
        worldTarget = worldTargetGoal;
        grounded = false;
        ik = new PlaneFABRIK(anatomy.limbLength, anatomy.limbSegments);
        ik.origin = worldOrigin;
        ik.poleVector = body.up + localOrigin.normalized;
        ik.target = worldTarget;
        settleTimer = 0f;
        groundLayer = LayerMask.GetMask("Ground");

        meshes = new Transform[anatomy.limbSegments];
        for (int i = 0; i < anatomy.limbSegments; i++)
        {
            meshes[i] = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            float meshLength = (anatomy.limbLength / anatomy.limbSegments) * 0.5f;
            float meshWidth = meshLength / 10f;
            meshes[i].localScale = new Vector3(meshWidth, meshLength, meshWidth);
        }


    }

    public bool IsGrounded()
    {
        return grounded;
    }

    public void SetOrigin(Vector3 newLocalOrigin)
    {
        localOrigin = newLocalOrigin;
    }
    public void setTargetGoal(Vector3 newLocalTargetGoal)
    {
        localTargetGoal = newLocalTargetGoal;
    }

    public void RefreshOrigin()
    {
        worldOrigin = body.TransformPoint(localOrigin);
        ik.origin = worldOrigin;
    }

    private void StartNewStride(float diff)
    {
        lastWorldTarget = worldTarget;
        float relativeToStrideLength = Mathf.Min(diff / stride.length, 1f);
        relativeStrideLift = stride.lift * relativeToStrideLength;
        relativeStridePull = stride.pull * relativeToStrideLength;
        strideTimer = 0f;
        settleTimer = 0f;
        grounded = false;
    }
    public void Walk(float dt, Vector3 currentVelocity, Vector3 angularVelocity, bool movable)
    {
        worldTargetGoal = worldOrigin + target.rotation * localTargetGoal + currentVelocity / (dt * stride.targetGoalVelocityMultiplier);
        worldTargetGoal = target.position + Vector3.ProjectOnPlane(worldTargetGoal - target.position, target.up);

        // Raycast
        Vector3 rayStart = worldTargetGoal + target.up * stride.lift;
        Vector3 rayDirection = (worldTargetGoal - rayStart).normalized;
        if (!Physics.Raycast(rayStart, rayDirection, out hit, stride.lift * 2, groundLayer))
        {
            rayStart = worldTargetGoal + (worldTargetGoal - target.position) + target.up * stride.lift;
            rayDirection = (worldTargetGoal - rayStart).normalized;
            rayStart += rayDirection * stride.lift;
            Physics.Raycast(rayStart, rayDirection, out hit, stride.lift * 6, groundLayer);
        }

        if (hit.collider != null)
        {
            worldTargetGoal = hit.point;
        }
        if (grounded)
        {
            if (movable)
            {
                float diff = (worldTargetGoal - worldTarget).magnitude;

                if (diff > stride.length * 0.1f)
                    settleTimer += dt;

                if (diff > stride.length || settleTimer > strideSettleTime)
                    StartNewStride(diff);
            }
        }
        else // moving
        {
            // speed up the stride timer by the greater boost type of either movement speed or turning speed.
            float angularTimerBooster = angularVelocity.magnitude * stride.durationAngularVelocityMultiplier * dt;
            float movementTimerBooster = (currentVelocity.magnitude / stride.length) * stride.durationVelocityMultiplier * dt;
            strideTimer += dt + Mathf.Max(angularTimerBooster, movementTimerBooster);
            float t = strideTimer / stride.duration;
            if (t > 1)
            {
                worldTarget = worldTargetGoal;
                ik.target = worldTarget;
                grounded = true;
            }
            else
            {
                Vector3 linear = Vector3.Lerp(lastWorldTarget, worldTargetGoal, t);
                Vector3 pullDirection = (worldOrigin - worldTargetGoal).normalized * relativeStridePull * stride.pullCurve.Evaluate(t);
                Vector3 liftDirection = ik.poleVector.normalized * relativeStrideLift * stride.liftCurve.Evaluate(t);
                ik.target = linear + pullDirection + liftDirection;
            }
        }
        float currentSmoothSpeed = grounded ? 0.6f : 0.2f;
        smoothNormalFactor = Vector3.SmoothDamp(smoothNormalFactor, hit.normal * (1 - Mathf.Max(Vector3.Dot(body.up, hit.normal), 0f)), ref poleVectorVelocity, currentSmoothSpeed);
        ik.poleVector = worldOrigin + (body.up + (worldOrigin - body.position).normalized + smoothNormalFactor * 3f).normalized * 100f;
        ik.Solve();
    }

    public void Hover()
    {
        worldTargetGoal = worldOrigin + body.rotation * localTargetGoal;
        ik.target = worldTargetGoal;
        ik.poleVector = worldOrigin + (body.up * 2f + (worldOrigin - body.position).normalized).normalized * 100f;
        ik.Solve();
    }

    public void DebugDraw()
    {
        Vector3[] results = ik.GetResults();
        for (int i = 0; i < results.Length; i++)
        {
            if (i > 0)
            {
                meshes[i-1].position = Vector3.Lerp(results[i - 1], results[i], 0.5f);
                meshes[i-1].up = results[i - 1] - results[i];
            }
        }

        // Debug draw goal pos
        Draw.Locator(worldTargetGoal, 0.1f, Color.red);
    }
}