using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct HitData
{
    public bool hit { get; private set; }
    public Vector3 normal { get; private set; }
    public Vector3 closestEdge { get; private set; }
    public float reflectBounce { get; private set; }
    public float normalBounce { get; private set; }
    public HitData(bool hit, Vector3 normal, Vector3 closestEdge, float reflectBounce, float normalBounce)
    {
        this.hit = hit;
        this.normal = normal;
        this.closestEdge = closestEdge;
        this.reflectBounce = reflectBounce;
        this.normalBounce = normalBounce;
    }
}
public abstract class ClothCollider : MonoBehaviour
{
    [Range(0f, 1f)]
    public float reflectBounce = 0.75f;
    [Range(0f, 1f)]
    public float normalBounce = 0.75f;
    [Range(1f, 300f)]
    public float radius = 150f;

    private void OnValidate()
    {
        float scaleAxes = radius * 2;
        transform.localScale = new Vector3(scaleAxes, scaleAxes, scaleAxes);
    }

    public abstract HitData GetCollision(Vector3 position, float pointRadius);

}
