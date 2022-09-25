using UnityEditor;
using UnityEngine;

public class Link
{
    public readonly string type;

    private readonly Point a;
    private readonly Point b;
    private float length;
    private bool dead = false;
    private bool canSelfRepair = false;
    private float extensibility;

    public Link(Point a, Point b, float length, float extensibility, string type)
    {
        this.a = a;
        this.b = b;
        this.length = length;
        this.extensibility = extensibility;
        this.type = type;
    }

    public Vector3 getPositionA()
    {
        return a.position;
    }
    public Vector3 getPositionB()
    {
        return b.position;
    }
    public void SetExtensibility(float newExtensibility)
    {
        extensibility = newExtensibility;
    }
    public void SetSelfRepair(bool newCanSelfRepair)
    {
        canSelfRepair = newCanSelfRepair;
    }
    public void SetLength(float newLength)
    {
        length = newLength;
    }

    public void Solve(float weight = 1.0f)
    {
        //Debug.DrawLine(a.position, b.position);
        Vector3 stickDir = (a.position - b.position);
        if (!dead || canSelfRepair)
        {
            dead = stickDir.magnitude > length * extensibility;
        }
        if (dead)
        {
            return;
        }

        stickDir.Normalize();
        Vector3 stickCenter = (a.position + b.position) / 2;
        if (!a.locked && !a.IsSelected())
        {
            a.position = Vector3.Lerp(a.position, stickCenter + stickDir * length / 2, weight);
        }
        if (!b.locked && !b.IsSelected())
        {
            
            b.position = Vector3.Lerp(b.position, stickCenter - stickDir * length / 2, weight);
        }
    }

    public bool HasPoint(Point p)
    {
        return p == a || p == b;
    }

    public Point GetPointB()
    {
        return b;
    }
}
