using UnityEngine;

public class PlaneFABRIK
{
    public Vector3 origin;
    public Vector3 target;
    public Vector3 poleVector;
    public float biasAngle, biasMagnitude;
    private float length, segmentLength, currentDistance;
    private Vector2[] points;
    private Vector3[] results;
    private int numSegments;
    private Matrix4x4 space;
    private Vector2 endEffector;
    private float errorMargin;
    private int maxItterations = 30;



    public PlaneFABRIK(float length, int numSegments)
    {
        this.length = length;
        errorMargin = length * 0.1f;
        segmentLength = length / numSegments;
        this.numSegments = numSegments;
        results = new Vector3[numSegments+1];
        points = new Vector2[numSegments+1];
    }

    public void Solve()
    {
        RefreshSpace();
        Vector3 dir = target - origin;
        currentDistance = dir.magnitude;
        if (length < currentDistance)
        {
            for (int i = 0; i < numSegments+1; i++)
            {
                points[i].Set(segmentLength * i, 0f);
            }

        }
        else
        {
            endEffector.Set(currentDistance, 0f);
            Vector2 up2d = (Vector2)Matrix4x4.Inverse(space).MultiplyPoint(poleVector);
            for (int i = 0; i < numSegments+1; i++)
            {
                points[i].Set(segmentLength * i + up2d.x - length * 0.5f, up2d.y);
            }

            for (int i = 0; i < maxItterations; i++)
            {

                if ((endEffector - points[0]).magnitude < errorMargin)
                    break;

                // Backward
                Vector2 prevPoint = Vector2.zero;
                for (int j = 0; j < numSegments+1; j++)
                {
                    points[j] = prevPoint;
                    if (j < numSegments)
                        prevPoint += (points[j + 1] - prevPoint).normalized * segmentLength;
                }
                // Forward
                prevPoint = endEffector;
                for (int j = numSegments; j >= 0; j--)
                {
                    points[j] = prevPoint;
                    if (j > 0)
                        prevPoint += (points[j - 1] - prevPoint).normalized * segmentLength;
                }
            }
        }



        // Get result vectors
        for (int i = 0; i < numSegments+1; i++)
        {
            results[i] = space.MultiplyPoint((Vector3)points[i]);
        }
    }

    public Vector3[] GetResults()
    {
        return results;
    }

    public float GetLength()
    {
        return length;
    }

    private void RefreshSpace()
    {
        Vector4 x = (Vector4)(target - origin).normalized;
        Vector4 z = (Vector4)Vector3.Cross(poleVector - origin, x).normalized;
        Vector4 y = (Vector4)Vector3.Cross(x, z).normalized;
        space.SetColumn(0, x); // X axis
        space.SetColumn(1, y); // Y axis
        space.SetColumn(2, z); // Z axis
        space.SetColumn(3, new Vector4(origin.x, origin.y, origin.z, 1f));
    }
}
