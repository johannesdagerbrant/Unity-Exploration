using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IkTest : MonoBehaviour
{
    public Transform targetTm, pvTm;
    public float length;
    public int limbSegments;
    private PlaneFABRIK ik;
    // Start is called before the first frame update
    void Start()
    {
        ik = new PlaneFABRIK(length, limbSegments);
    }

    // Update is called once per frame
    void Update()
    {
        ik.origin = transform.position;
        ik.poleVector = pvTm.position;
        ik.target = targetTm.position;
        ik.Solve();

        Vector3[] results = ik.GetResults();
        for (int i = 0; i < results.Length; i++)
        {
            if (i > 0)
            {
                Debug.DrawLine(results[i - 1], results[i], Color.magenta);
            }
        }
    }
}
