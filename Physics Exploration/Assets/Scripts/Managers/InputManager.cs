using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    PlayerControls controls;
    Vector2 moveInput, lookInput;
    float jumpInput;
    // Start is called before the first frame update
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
    public Vector2 GetLookInput()
    {
        return lookInput;
    }
    public float GetJumpInput()
    {
        return jumpInput;
    }

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Locomotion.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Locomotion.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Locomotion.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Locomotion.Look.canceled += ctx => lookInput = Vector2.zero;
        controls.Locomotion.Jump.performed += ctx => jumpInput = ctx.ReadValue<float>();
        controls.Locomotion.Jump.canceled += ctx => jumpInput = 0f;
    }

    private void OnEnable()
    {
        controls.Locomotion.Enable();
    }
}
