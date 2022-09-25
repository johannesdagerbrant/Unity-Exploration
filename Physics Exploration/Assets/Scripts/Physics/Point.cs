using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Point
{
    public bool locked;
    public Vector3 position;
    public List<Link> links = new List<Link>();

    private Vector3 lastPosition;
    private bool resetNextUnlockedFrame = false;
    private List<Link> incomingLinks = new List<Link>();
    private GameObject shape;
    private bool selectable;
    private float airFriction = 0.999f;
    private float collidingFriction = 0.8f;
    private float currentFriction, radius;

    public Point(Vector3 position, GameObject shape, bool locked, bool selectable)
    {
        this.position = position;
        lastPosition = position;
        this.shape = shape;
        this.locked = locked;
        this.selectable = selectable;
        currentFriction = airFriction;
        radius = (shape.transform.localScale.x + shape.transform.localScale.y + shape.transform.localScale.z) * 0.5f * 0.33333333333333333f;
    }

    public void LinkTo(Point p, float length, float extensibility, string type)
    {
        Link l = new Link(this, p, length, extensibility, type);
        links.Add(l);
        p.incomingLinks.Add(l);
    }

    public void Unlink(Point p)
    {
        for (int i = 0; i < links.Count; i++)
        {
            if (links[i].HasPoint(p))
            {
                links.RemoveAt(i);
            }
        }
    }

    public bool IsSelected()
    {
        return false;//selectable && Selection.Contains(shape);
    }
    public GameObject GetShape()
    {
        return shape;
    }
    public Link GetLink(int i)
    {
        if (i > -1 && i < links.Count)
        {
            return links[i];
        }
        return null;
    }

    public void UpdateForces(Vector3 force, float dt)
    {
        if (locked || IsSelected())
        {
            if (!resetNextUnlockedFrame)
            {
                resetNextUnlockedFrame = true;
            }
        }
        else
        {
            Vector3 positionBeforeUpdate = position;
            Vector3 velocity = Vector3.zero;
            if (resetNextUnlockedFrame)
            {
                resetNextUnlockedFrame = false;
            }
            else
            {
                velocity = (position - lastPosition) * currentFriction;
            }
            position += velocity;
            position += force * dt;
            lastPosition = positionBeforeUpdate;
        }
    }

    public void UpdateCollisions(ClothCollider[] collders)
    {
        bool hitByAnything = false;
        foreach (ClothCollider c in collders)
        {
            HitData hitData = c.GetCollision(position, radius);
            if (hitData.hit && !hitByAnything)
            {
                ReflectCollision(hitData);
                hitByAnything = true;
            }
        }

        if(hitByAnything)
        {
            currentFriction = collidingFriction;
        }
        else
        {
            currentFriction = airFriction;
        }
    }

    private void ReflectCollision(HitData hitData)
    {
        Vector3 lastPosDif = lastPosition - position;
        Vector3 reflectedLastPosDif = Vector3.Reflect(lastPosDif, hitData.normal);
        lastPosition = position + reflectedLastPosDif * hitData.reflectBounce + hitData.normal * lastPosDif.magnitude * hitData.normalBounce;
        position = hitData.closestEdge;
    }
    public void UpdateSpacing(float spacing, string excludeType = null, string onlyType = null)
    {
        foreach (Link link in links)
        {
            if (onlyType == link.type)
            {
                link.SetLength(spacing);

            }
            if (onlyType == null && link.type != excludeType)
            {
                link.SetLength(spacing);
            }
        }

    }
    public void SolveLinks(float weight, string excludeType = null, string onlyType = null)
    {
        foreach (Link link in links)
        {
            if (onlyType == link.type)
            {
                link.Solve(weight);

            }
            if (onlyType == null && link.type != excludeType)
            {
                link.Solve(weight);
            }
        }
    }
    public void SetSelfRepair(bool newCanSelfRepair)
    {
        foreach (Link link in links)
        {
            link.SetSelfRepair(newCanSelfRepair);
        }
    }

    public void UpdateShapePosition()
    {
        if (IsSelected())
        {
            position = shape.transform.position;
        }
        else
        {
            shape.transform.position = position;
        }
    }

    public void UpdateShapeRotation()
    {
        Vector3 forward = GetTypeDirection("row");
        Vector3 upward = GetTypeDirection("column");
        Vector3 cross = Vector3.Cross(forward, upward);
        float perpendicularDiff = Vector3.Angle(forward, upward) - 90;
        // adjust upward and forward vectors equally to become perpendicular to each other.
        forward = Quaternion.AngleAxis(perpendicularDiff, cross) * forward;
        upward = Quaternion.AngleAxis(-perpendicularDiff, cross) * upward;
        Matrix4x4 m = new Matrix4x4();
        m.SetColumn(0, new Vector4(cross.x, cross.y, cross.z, 0.0f)); // X axis
        m.SetColumn(1, new Vector4(forward.x, forward.y, forward.z, 0.0f)); // Y axis
        m.SetColumn(2, new Vector4(upward.x, upward.y, upward.z, 0.0f)); // Z axis
        m.SetColumn(3, new Vector4(0f,0f,0f,1f));

        shape.transform.rotation = m.rotation;
    }

    Vector3 GetTypeDirection(string type)
    {
        Vector3 inPos = position;
        Vector3 outPos = position;
        foreach (Link l in links)
        {
            if (l.type == type)
            {
                outPos = l.getPositionB();
            }
        }
        foreach (Link l in incomingLinks)
        {
            if (l.type == type)
            {
                inPos = l.getPositionA();
            }
        }

        Vector3 dir;
        if (inPos == outPos)
        {
            dir = Vector3.up;
        }
        else
        {
            dir = outPos - inPos;
        }

        return dir.normalized;
    }

    Point GetDiagonalNeighbour(bool useIncomingSubLinks = false)
    {   
        foreach (Link l in links)
        {
            if (l.type == "row")
            {
                return l.GetPointB().GetNeighbour("column", useIncomingSubLinks);
            }
        }
        return null;
    }

    public Point GetNeighbour(string type, bool useIncomingLinks = false)
    {
        List<Link> linkList = useIncomingLinks ? incomingLinks : links;
        foreach (Link l in linkList)
        {
            if (l.type == type)
            {
                return l.GetPointB();
            }
        }
        return null;
    }
}
