using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

public class RBF : MonoBehaviour
{
    // Based on http://www.marcuskrautwurst.com/2017/12/rbf-node.html
    public static float[,] Weights(Vector3[] keys, Vector3[] values, string interpolationType = null, float radius = 0.5f)
    {
        int n = keys.Length;
        if (n != values.Length)
            Debug.LogError("Could not get an RBF weight matrix. Keys and values were of different lengths.");

        // A represents a graph of euclidian distances between keys.
        Matrix<float> a = Matrix<float>.Build.Dense(n, n);
        Matrix<float> b = Matrix<float>.Build.Dense(n, n);
        for (int i = 0; i < n; i++)
        {
            float[] newKeyRow = new float[n];
            for (int j = 0; j < n; j++)
                newKeyRow[j] = Vector3.Distance(keys[i], keys[j]);

            a.SetColumn(i, newKeyRow);

            float[] valueRow = new float[n];
            valueRow[0] = values[i][0]; valueRow[1] = values[i][1]; valueRow[2] = values[i][2];
            
            b.SetColumn(i, valueRow);
        }

        // Linear solve for Ax = B to get the weight matrix (x).
        Matrix<float> a_inverse = a.Inverse();
        Matrix<float> x = b * a_inverse;

        return x.ToArray();
    }
    public static Vector3 Solve(float[,] weights, Vector3[] keys, Vector3 point, int n)
    {
        /*
        Solve for the current postiion to interpolate from. This needs to be
        calculated runtime whenever the point value changed (usually every frame).
        */

        // iterate through the length of a distance and multiply it by its total weight.
        Vector3 result = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            float pointDist = Vector3.Distance(point, keys[i]);
            float[] weightCol = GetColumn(weights, i);
            result += new Vector3(weightCol[0], weightCol[1], weightCol[2]) * pointDist;
        }
        return result;
    }

    public static float NDimentionalDistance(float[] a, float[] b)
    {
        int aLength = a.Length;
        // If b does not have identical amont of elements as a, get length of a.
        if (aLength != b.Length)
            b = new float[aLength];

        float sq_result = 0f;
        for (int i = 0; i < aLength; i++)
            sq_result += Mathf.Pow(a[i] - b[i], 2);

        return Mathf.Sqrt(sq_result);
    }


    static float[] GetColumn(float[,] m, int col)
    {
        return Enumerable.Range(0, m.GetLength(1)).Select(x => m[x, col]).ToArray();
    }
}
