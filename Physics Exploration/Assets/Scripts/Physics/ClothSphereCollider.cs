using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSphereCollider : ClothCollider
{
    public override HitData GetCollision(Vector3 position, float pointRadius)
    {
        if (Vector3.Distance(position, transform.position) < radius + pointRadius)
        {
            Vector3 normal = Vector3.Normalize(position - transform.position);
            Vector3 closestEdge = transform.position + normal * (radius + pointRadius);
            return new HitData(true, normal, closestEdge, reflectBounce, normalBounce);
        }
        return new HitData();
    }
}
