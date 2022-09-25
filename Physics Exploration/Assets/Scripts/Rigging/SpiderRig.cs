using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Anatomy
{
    [Range(4, 50)]
    public int numLimbs;
    [Range(0.1f, 1000f)]
    public float limbLength;
    [Range(2, 10)]
    public int limbSegments;
    public Vector3 headPosition;
    public Vector3 headScale;
    public Vector3 bodyScale;
}
[System.Serializable]
public class Origins
{
    [Range(0f, 180f)]
    public float spacing;
    [Range(-90f, 90f)]
    public float height;
    [Range(-45f, 45f)]
    public float depth;
}
[System.Serializable]
public class Pose
{
    [Range(0f, 1f)]
    public float stance;
    [Range(0f, 1000f)]
    public float bend;
}

[System.Serializable]
public class Movement
{
    [Range(0f, 1000f)]
    public float widerStanceWhenAcceleration;
    [Range(0f, 1000f)]
    public float widerStanceWhenDeceleration;
    [Range(0f, 1000f)]
    public float braceWhenAcceleration;
    [Range(0f, 1000f)]
    public float braceWhenDeceleration;
    [Range(0f, 1000f)]
    public float pitchWhenAcceleration;
    [Range(0f, 1000f)]
    public float pitchWhenDeceleration;
    [Range(0f, 1000f)]
    public float rollWhenAcceleration;
    [Range(0f, 1000f)]
    public float rollWhenDeceleration;
    [Range(0f, 10f)]
    public float jumpTime;
    [Range(0f, 10f)]
    public float landingTime;
}

public class SpiderRig : MonoBehaviour
{

    public Anatomy anatomy;
    public Origins origins;
    public Pose pose;
    public Stride stride;
    public Movement movement;
    
    public SecondOrderDynamics3D bodyPosSOD, bodyUpSOD;
    public SecondOrderDynamics1D bodyBraceSOD, braceStanceSOD;
    public SecondOrderDynamics3D bodyYawSOD;
    public SecondOrderDynamics1D bodyPitchSOD, bodyRollSOD;
    private Leg[] leftLegs, rightLegs;
    private int halfNumLimbs;

    private Transform body, head, bodyTarget;
    private Vector3 bodySimulatedPosition, bodySimulatedUp, bodyAcceleration;
    private Vector3 goalVelocity, currentVelocity;
    private Quaternion bodySimulatedRotation;
    private float simulatedPitch, simulatedRoll, braceStance;
    private bool groundedMover;
    private float jumpingT, landingT;

    public void Init(Transform bodyTarget)
    {
        this.bodyTarget = bodyTarget;
        body = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        head = new GameObject().transform;
        Transform headMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        headMesh.parent = head;
        headMesh.localPosition = new Vector3(0f, 0.25f, 0.25f);
        braceStance = pose.stance;
        InitLimbs();
        // Movement and orientation
        bodySimulatedPosition = bodyTarget.position + bodyTarget.up * pose.bend;
        bodyPosSOD.Init(bodySimulatedPosition); bodyUpSOD.Init(bodyTarget.up); bodyYawSOD.Init(bodyTarget.forward);
        // Acceleration cues
        bodyBraceSOD.Init(0f); bodyPitchSOD.Init(0f); bodyRollSOD.Init(0f); braceStanceSOD.Init(braceStance);

    }
    // Update is called once per frame
    public void UpdateAnimation(float dt, bool grounded)
    {
        if (grounded && !groundedMover)
        {
            if (landingT < movement.landingTime)
                landingT += dt;
            else
            {
                groundedMover = true;
                landingT = 0f;
            }
        }
        if (!grounded && groundedMover)
        {
            if (jumpingT < movement.jumpTime)
                jumpingT += dt;
            else
            {
                groundedMover = false;
                jumpingT = 0f;
            }
        }

        UpdateBodySimulation(dt);
        UpdateHeadSimulation(dt);

        for (int i = 0; i < halfNumLimbs; i++)
        {
            Leg left = leftLegs[i];
            Vector3 newLeftLocalTargetGoal = CalculateLocalTargetGoal(i, true);
            left.setTargetGoal(newLeftLocalTargetGoal);
            RefreshLeg(left, i, true, dt);

            Leg right = rightLegs[i];
            Vector3 newRightLocalTargetGoal = CalculateLocalTargetGoal(i, false);
            left.setTargetGoal(newRightLocalTargetGoal);
            RefreshLeg(right, i, false, dt);
        }
    }

    private void UpdateBodySimulation(float dt)
    {
        Vector3 tPos = bodyTarget.position;
        Vector3 tUp = bodyTarget.up;
        Vector3 tForward = bodyTarget.forward;
        Vector3 tRight = bodyTarget.right;
        // Access and compare velocities.
        goalVelocity = bodyPosSOD.GetXDerivative() * dt;
        currentVelocity = bodyPosSOD.GetYDerivative() * dt;
        bodyAcceleration = goalVelocity - currentVelocity;
        bool isAccelerating = goalVelocity.magnitude > currentVelocity.magnitude;

        // Update second order dynamic driven heading rotation (Yaw is based on heading direction, pitch and roll is used to emphazise changes is movement).
        Vector3 simulatedAimDirection = bodyYawSOD.Update(dt, tForward);
        bodySimulatedUp = bodyUpSOD.Update(dt, tUp);
        bodySimulatedRotation = Quaternion.LookRotation(simulatedAimDirection, bodySimulatedUp);

        float forwardAcceleration = Vector3.Dot(bodyAcceleration, tForward);
        float pitchMultiplier = isAccelerating ? movement.pitchWhenAcceleration : movement.pitchWhenDeceleration;
        float pitchGoal = forwardAcceleration * pitchMultiplier * 10f * dt;
        simulatedPitch = bodyPitchSOD.Update(dt, pitchGoal);
        bodySimulatedRotation *= Quaternion.Euler(simulatedPitch, 0f, 0f);

        float sideAcceleration = Vector3.Dot(bodyAcceleration, -tRight); // bodyTarget.left or bodyTarget.right?
        float rollMultiplier = isAccelerating ? movement.rollWhenAcceleration : movement.rollWhenDeceleration;
        float rollGoal = sideAcceleration * rollMultiplier * 10f * dt;
        simulatedRoll = bodyRollSOD.Update(dt, rollGoal);
        bodySimulatedRotation *= Quaternion.Euler(0f, 0f, simulatedRoll);

        if (groundedMover)
        {
            // Update second order dynamic driven position
            bodySimulatedPosition = bodyPosSOD.Update(dt, tPos);
            // Update bracing and leaning that emphasizes acceleration and deceleration of the simulated positions movement.
            float braceMultiplier = isAccelerating ? movement.braceWhenAcceleration : movement.braceWhenDeceleration;
            float braceGoal = Mathf.Lerp(pose.bend, anatomy.bodyScale.y * 0.5f, bodyAcceleration.magnitude * braceMultiplier * dt); // TODO: Replace 0f with ground beneath body
            float braceT = bodyBraceSOD.Update(dt, braceGoal);
            bodySimulatedPosition += bodySimulatedUp * braceT;

            // Force simulated position to be above the ground.
            if (false) //(Vector3.Dot(tUp, (bodySimulatedPosition - tPos).normalized) < 0f)
            {
                bodySimulatedPosition = Vector3.ProjectOnPlane(bodySimulatedPosition, tUp);
            }
        }
        else
        {
            bodyPosSOD.Update(dt, tPos);
            bodySimulatedPosition = tPos;
        }

        float widerStanceMultiplier = isAccelerating ? movement.widerStanceWhenAcceleration : movement.widerStanceWhenDeceleration;
        float braceStanceGoal = pose.stance - bodyAcceleration.magnitude * widerStanceMultiplier * dt;
        braceStance = braceStanceSOD.Update(dt, braceStanceGoal);
        // Apply new simulated values to the body transform
        body.SetPositionAndRotation(bodySimulatedPosition, bodySimulatedRotation);
        body.localScale = anatomy.bodyScale;
    }
    private void UpdateHeadSimulation(float dt)
    {
        head.position = body.TransformPoint(anatomy.headPosition);
        head.rotation = Camera.main.transform.rotation * Quaternion.Euler(0f, 0f, simulatedRoll * 0.5f);
        head.localScale = anatomy.headScale;
        //head.forward = headTarget.forward;
    }
    private void RefreshLeg(Leg l, int i, bool isLeft, float dt)
    {
        l.RefreshOrigin();
        if (groundedMover)
        {
            bool movable = IsLegMovable(i, isLeft);
            Vector3 angularVelocity = bodyYawSOD.GetYDerivative();
            l.Walk(dt, currentVelocity, angularVelocity, movable);
        }
        else
        {
            l.Hover();
        }

        l.DebugDraw();
    }

    private bool IsLegMovable(int i, bool isLeft)
    {
        if (i == 0)
        {
            if (isLeft)
                return rightLegs[i].IsGrounded();
            else
                return leftLegs[i].IsGrounded();
        }
        if (isLeft)
            return rightLegs[i].IsGrounded() && leftLegs[i-1].IsGrounded();
        else
            return leftLegs[i].IsGrounded() && rightLegs[i-1].IsGrounded();
    }


    private void InitLimbs()
    {
        // Number of limbs must be an even number
        if (anatomy.numLimbs % 2 != 0)
            anatomy.numLimbs--;
        halfNumLimbs = anatomy.numLimbs / 2;

        leftLegs = new Leg[halfNumLimbs];
        rightLegs = new Leg[halfNumLimbs];

        for (int i = 0; i < halfNumLimbs; i++)
        {
            // Left legs
            Vector3 localOrigin = CalcualteLocalOrigin(i, true);
            Vector3 localTargetGoal = CalculateLocalTargetGoal(i, true);
            leftLegs[i] = new Leg(localOrigin, localTargetGoal, bodyTarget, body, anatomy, stride);
            // Right legs
            localOrigin = CalcualteLocalOrigin(i, false);
            localTargetGoal = CalculateLocalTargetGoal(i, false);
            rightLegs[i] = new Leg(localOrigin, localTargetGoal, bodyTarget, body, anatomy, stride);
        }
    }
    private Vector3 CalcualteLocalOrigin(int i, bool isLeft)
    {
        float angleRange = 180f - origins.spacing;
        float offsetX = ((angleRange / (halfNumLimbs - 1)) * ((halfNumLimbs - 1) - i));
        float originAngleX = origins.spacing * 0.5f + offsetX;
        float depthAngle = origins.depth;
        Vector3 upDown = Vector3.back;
        float heightAngle = origins.height;
        if (!isLeft)
        {
            originAngleX *= -1;
            depthAngle *= -1;
            upDown *= -1;
            heightAngle *= -1;
        }
        return ((Quaternion.Euler(0, originAngleX + depthAngle, 0) * Vector3.forward) + upDown + (Quaternion.Euler(heightAngle, 0, 0) * -upDown)).normalized * 0.5f;
    }

    private Vector3 CalculateLocalTargetGoal(int i, bool isLeft)
    {
        float offsetX = ((180f / (halfNumLimbs - 1)) * ((halfNumLimbs - 1) - i));
        float originAngleX = isLeft ? 0.5f + offsetX : 0.5f - offsetX;
        return (Quaternion.Euler(0, originAngleX, 0) * Vector3.forward).normalized * anatomy.limbLength * (1 - braceStance);
    }
}