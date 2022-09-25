using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SecondOrderDynamics
{
    protected float _w, _z, _d, k1, k2, k3;
    protected float k1Stable, k2Stable;
    protected float tCrit;
    float pi = Mathf.PI;

    protected void ComputeConstants(float f, float z, float r)
    {
        _w = 2 * pi * f;
        _z = z;
        _d = _w * Mathf.Sqrt(Mathf.Abs(z * z - 1));
        k1 = z / (pi * f);
        k2 = 1 / ((2 * pi * f) * (2 * pi * f));
        k3 = r * z / (2 * pi * f);
        tCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);

    }

    protected void StabilizeK1K2(float T)
    {
        if (_w * T < _z)
        {
            k1Stable = k1;
            k2Stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);
        }
        else
        {
            float t1 = Mathf.Exp(-_z * _w * T);
            float alpha = 2 * t1 * (_z <= 1 ? Mathf.Cos(T * _d) : Mathf.Cos(T * _d));
            float beta = t1 * t1;
            float t2 = T / (1 + beta - alpha);
            k1Stable = (1 - beta) * t2;
            k2Stable = T * t2;
        }
    }

    public void DrawCurve()
    {
        float timeStep = 1f / 120f;
        float T = 0f;
        float fx = 0.1f;
        float fxd;
        float fxp = fx;
        float fy = 0.1f;
        float fyd = 0f;
        float fyp = fy;
        StabilizeK1K2(timeStep);
        Debug.DrawLine(new Vector2(0f, 0f), new Vector2(0f, 100f), Color.white);
        Debug.DrawLine(new Vector2(0f, 0f), new Vector2(200f, 0f), Color.white);
        for (int i = 0; i < 240; i++)
        {
            T += timeStep;
            if (i == 30)
            {
                fx = 0.5f;
            }

            Debug.DrawLine(new Vector2((T - timeStep) * 100f, fxp * 100f), new Vector2(T * 100f, fx * 100f), Color.blue);

            fxd = (fx - fxp) / timeStep;
            fxp = fx;

            fy += timeStep * fyd;
            fyd += timeStep * (fx + k3 * fxd - fy - k1Stable * fyd) / k2Stable;

            Debug.DrawLine(new Vector2((T - timeStep) * 100f, fyp * 100f), new Vector2(T * 100f, fy * 100f), Color.red);
            fyp = fy;
        }
    }
}

[System.Serializable]
public class SecondOrderDynamics1D : SecondOrderDynamics
{
    [Range(0.01f, 10f)]
    public float f;
    [Range(0f, 2f)]
    public float z;
    [Range(-10f, 10f)]
    public float r;
    public bool draw = false;
    public bool computeEveryFrame = false;
    public bool stabilizeTime = false;

    private float xp, xd, y, yd;

    public void Init(float x0)
    {
        ComputeConstants(f, z, r);
        xp = x0;
        xd = 0f;
        y = x0;
        yd = 0f;
    }

    public float Update(float T, float x)
    {
        if(computeEveryFrame)
            ComputeConstants(f, z, r);
        if (draw)
            DrawCurve();

        xd = (x - xp) / T;
        xp = x;

        StabilizeK1K2(T);
        if (stabilizeTime)
        {
            int iterations = (int)Mathf.Ceil(T / tCrit);
            T = T / iterations;
            for (int i = 0; i < iterations; i++)
            {
                y += T * yd;
                yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
            }
        }
        else
        {
            y += T * yd;
            yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
        }
        return y;
    }

    public void SetF(float newF)
    {
        f = newF;
        ComputeConstants(f, z, r);
    }

    public float GetXDerivative()
    {
        return xd;
    }

    public float GetYDerivative()
    {
        return yd;
    }
}


[System.Serializable]
public class SecondOrderDynamics2D : SecondOrderDynamics
{
    [Range(0.01f, 10f)]
    public float f;
    [Range(0f, 2f)]
    public float z;
    [Range(-10f, 10f)]
    public float r;
    public bool draw = false;
    public bool computeEveryFrame = false;
    public bool stabilizeTime = false;

    private Vector2 xp, xd, y, yd;
    public void Init(Vector2 x0)
    {
        // compute constants
        ComputeConstants(f, z, r);
        xp = x0;
        xd = Vector2.zero;
        y = x0;
        yd = Vector2.zero;
    }

    public Vector2 Update(float T, Vector2 x)
    {
        if (computeEveryFrame)
            ComputeConstants(f, z, r);
        if (draw)
            DrawCurve();

        xd = (x - xp) / T;
        xp = x;

        StabilizeK1K2(T);
        if (stabilizeTime)
        {
            int iterations = (int)Mathf.Ceil(T / tCrit);
            T = T / iterations;
            for (int i = 0; i < iterations; i++)
            {
                y += T * yd;
                yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
            }
        }
        else
        {
            y += T * yd;
            yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
        }
        return y;
    }

    public void SetF(float newF)
    {
        f = newF;
        ComputeConstants(f, z, r);
    }

    public Vector2 GetXDerivative()
    {
        return xd;
    }

    public Vector2 GetYDerivative()
    {
        return yd;
    }
}


[System.Serializable]
public class SecondOrderDynamics3D : SecondOrderDynamics
{
    [Range(0.01f, 10f)]
    public float f;
    [Range(0f, 2f)]
    public float z;
    [Range(-10f, 10f)]
    public float r;
    public bool draw = false;
    public bool reComputeConstants = false;
    public bool stabilizeTime = false;

    private Vector3 xp, xd, y, yd;
    public void Init(Vector3 x0)
    {
        // compute constants
        ComputeConstants(f, z, r);
        xp = x0;
        xd = Vector3.zero;
        y = x0;
        yd = Vector3.zero;
    }

    public Vector3 Update(float T, Vector3 x)
    {
        if (reComputeConstants)
            ComputeConstants(f, z, r);
        if (draw)
            DrawCurve();

        xd = (x - xp) / T;
        xp = x;
        StabilizeK1K2(T);
        if (stabilizeTime)
        {
            int iterations = (int)Mathf.Ceil(T / tCrit);
            T = T / iterations;
            for (int i = 0; i < iterations; i++)
            {
                y += T * yd;
                yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
            }
        }
        else
        {
            y += T * yd;
            yd += T * (x + k3 * xd - y - k1Stable * yd) / k2Stable;
        }
        return y;
    }

    public void SetF(float newF)
    {
        f = newF;
        ComputeConstants(f, z, r);
    }

    public Vector3 GetXDerivative()
    {
        return xd;
    }

    public Vector3 GetYDerivative()
    {
        return yd;
    }
    public void SetYDerivative(Vector3 newYd)
    {
        yd = newYd;
    }
}