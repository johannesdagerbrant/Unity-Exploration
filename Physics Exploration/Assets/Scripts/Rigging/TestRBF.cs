using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRBF : MonoBehaviour
{

    public Transform[] targets;
    public bool usingIMQB = true;
    public bool usingGaussian = false;
    Vector3[] positions, scales, rotations;
    int n;
    // Start is called before the first frame update
    void Start()
    {
        n = targets.Length;
        positions = new Vector3[n];
        rotations = new Vector3[n];
        scales = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            positions[i] = targets[i].position;
            rotations[i] = targets[i].rotation.eulerAngles;
            scales[i] = targets[i].localScale;
        }
    }

    // Update is called once per frame
    void Update()
    {
        string interpolationType = usingIMQB ? "IMQB" : null;
        interpolationType = usingGaussian ? "Gaussian" : interpolationType;
        float[,] rotWeights = RBF.Weights(positions, rotations, interpolationType);
        Vector3 eulerRotation = RBF.Solve(rotWeights, positions, transform.position, n);
        transform.rotation = Quaternion.Euler(eulerRotation);

        float[,] scaleWeights = RBF.Weights(positions, scales, interpolationType);
        Vector3 scale = RBF.Solve(scaleWeights, positions, transform.position, n);
        transform.localScale = scale;
    }
}
