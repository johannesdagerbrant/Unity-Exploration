using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Based on https://arrowinmyknee.com/2021/03/15/some-math-about-capsule-collision/

public class ClothCapsuleCollider : ClothCollider
{
    [Range(1f, 300f)]
    public float height = 150f;
    public Transform cylinder;
    public Transform sphereA;
    public Transform sphereB;

    private Vector3 closestCenter = new Vector3();
    private void OnValidate()
    {
        float scaleAxes = radius * 2;
        Vector3 sphereOffset = new Vector3(0f, height, 0f);
        Vector3 sphereScale = new Vector3(scaleAxes, scaleAxes, scaleAxes);
        sphereA.localPosition = sphereOffset;
        sphereB.localPosition = -sphereOffset;
        sphereA.localScale = sphereScale;
        sphereB.localScale = sphereScale;
        cylinder.localScale = new Vector3(scaleAxes, height, scaleAxes);
    }
    public override HitData GetCollision(Vector3 position, float pointRadius)
    {
        Vector3 up = transform.up;
        Vector3 a = transform.position + up * height;
        Vector3 b = transform.position - up * height;
        Vector3 ab = b - a;
        Vector3 ac = position - a;
        Vector3 bc = position - b;
        float e = Vector3.Dot(ac, ab);
        bool closestToA = e <= 0.0f;
        float f = Vector3.Dot(ab, ab);
        bool closestToB = e >= f;
        float dist;
        if (closestToA)
        {
            dist = Vector3.Dot(ac, ac);
            closestCenter = a;
        }
        else if (closestToB)
        {
            dist = Vector3.Dot(bc, bc);
            closestCenter = b;
        }
        else
        {
            dist = Vector3.Dot(ac, ac) - e * e / f;
            closestCenter = a + ab * Vector3.Dot(ab.normalized, ac.normalized * (ac.magnitude / ab.magnitude));
        }

        
        float combinedRadius = radius + pointRadius;

        if (dist <= combinedRadius * combinedRadius)
        {
            Vector3 normal = Vector3.Normalize(position - closestCenter);
            Vector3 closestEdge = closestCenter + normal * combinedRadius;
            return new HitData(true, normal, closestEdge, reflectBounce, normalBounce);
        }
        return new HitData();
    }
    private float SqDistPointSegment(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 bc = c - b;
        float e = Vector3.Dot(ac, ab);
        // Handle cases where c projects outside ab
        if (e <= 0.0f)
        {
            return Vector3.Dot(ac, ac);
        }
        float f = Vector3.Dot(ab, ab);
        if (e >= f)
        {
            return Vector3.Dot(bc, bc);
        }
        // Handle cases where c projects onto ab
        return Vector3.Dot(ac, ac) - e * e / f;
    }
}
