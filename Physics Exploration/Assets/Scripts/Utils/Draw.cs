using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Draw
{
    public static void Locator(Vector3 position, float scale)
    {
        Color color = Color.red;
        Debug.DrawLine(position + Vector3.up * scale, position + Vector3.down * scale, color);
        Debug.DrawLine(position + Vector3.left * scale, position + Vector3.right * scale, color);
        Debug.DrawLine(position + Vector3.forward * scale, position + Vector3.back * scale, color);
    }
    public static void Locator(Vector3 position, float scale, Color color)
    {
        Debug.DrawLine(position + Vector3.up * scale, position + Vector3.down * scale, color);
        Debug.DrawLine(position + Vector3.left * scale, position + Vector3.right * scale, color);
        Debug.DrawLine(position + Vector3.forward * scale, position + Vector3.back * scale, color);
    }
    public static void Arrow(Vector3 a, Vector3 b)
    {
        Color color = Color.red;
        Debug.DrawLine(a, b, color);
    }
    public static void Arrow(Vector3 a, Vector3 b, Color color)
    {
        Debug.DrawLine(a, b, color);
    }
}
