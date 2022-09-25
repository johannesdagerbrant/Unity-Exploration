using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Walking
    public Settings walkSettings;
    private Walking walking;



    // Active locomotion type
    private Locomotion currentLocomotion;

    private SpiderRig rig;
    private bool playing = false;

    // Start is called before the first frame update
    void Start()
    {
        playing = true;
        walking = new Walking(transform, walkSettings);
        currentLocomotion = walking;
        rig = GetComponent<SpiderRig>();
        rig.Init(transform);
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        currentLocomotion.Update(dt);
        if (currentLocomotion.IsLocomotionAllowed())
            currentLocomotion.SetTransform(dt);
        else
            print("should change state");

        rig.UpdateAnimation(dt, true);
    }

    private void OnDrawGizmos()
    {
        if (playing)
        {

            //Gizmos.DrawWireSphere(currentLocomotion.GetDesiredPosition(), walkMoveSettings.radius);
        }
    }
    public Vector3 GetDesiredPosition()
    {
        return currentLocomotion.GetDesiredPosition();
    }
    public Vector2 GetDesiredAim()
    {
        return currentLocomotion.GetDesiredAim();
    }
}
