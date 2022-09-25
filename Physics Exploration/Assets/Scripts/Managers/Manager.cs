using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public static InputManager input;
    // Start is called before the first frame update
    void Awake()
    {
        input = gameObject.GetComponent<InputManager>();
    }
}
