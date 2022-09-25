using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Range(0.001f, 1f)]
    public float positionSmoothness = 0.3f;
    [Range(0.001f, 100f)]
    public float smoothRotationOnInput = 0.1f;
    [Range(0.001f, 100f)]
    public float smoothRotationNoInput = 0.6f;



    private Transform playerTm, cameraTm;
    private PlayerController pc;
    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 up, upVelocity = Vector3.zero;
    private Vector3 forward, forwardVelocity = Vector3.zero;
    private Vector3 defaultCamOffset;

    // Start is called before the first frame update
    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        playerTm = p.transform;
        pc = p.GetComponent<PlayerController>();
        cameraTm = Camera.main.transform;
        defaultCamOffset = cameraTm.position;

    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        UpdateCameraHandle(dt);
        UpdateCamera(dt);
    }

    private void UpdateCameraHandle(float dt)
    {
        Vector2 lookInput = Manager.input.GetLookInput();
        float usedSmoothRotation = lookInput.magnitude > 0f ? smoothRotationOnInput : smoothRotationNoInput;
        Quaternion localRotation = playerTm.rotation * Quaternion.Euler(pc.GetDesiredAim().y, 0f, 0f);
        Vector3 targetUp = localRotation * Vector3.up;
        Vector3 targetForward = localRotation * Vector3.forward;
        up = Vector3.SmoothDamp(up, targetUp, ref upVelocity, usedSmoothRotation);
        forward = Vector3.SmoothDamp(forward, targetForward, ref forwardVelocity, usedSmoothRotation);

        Quaternion newRotation = Quaternion.LookRotation(up, forward) * Quaternion.Euler(-90f, 0f, 180f);
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, pc.GetDesiredPosition(), ref positionVelocity, positionSmoothness);

        transform.SetPositionAndRotation(newPosition, newRotation);

    }

    private void UpdateCamera(float dt)
    {
        cameraTm.localPosition = defaultCamOffset;
    }
}
