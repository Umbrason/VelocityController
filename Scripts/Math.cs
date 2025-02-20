using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal static class Math
{
    public static float sMin(float a, float b, float k)
    {
        float h = MathF.Max(k - MathF.Abs(a - b), 0.0f) / k;
        return MathF.Min(a, b) - h * h * k * (.25f);
    }

    public static float MaxMagnitude(float a, float b) => Mathf.Abs(a) > Mathf.Abs(b) ? a : b;
    public static float MinMagnitude(float a, float b) => Mathf.Abs(a) < Mathf.Abs(b) ? a : b;

}

internal static class VectorMath
{

    public static Vector3 Min(this IEnumerable<Vector3> vectors)
    {
        var result = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        vectors.Select((x) => result = Vector3.Min(x, result));
        return result;
    }
    public static Vector3 Max(this IEnumerable<Vector3> vectors)
    {
        var result = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        vectors.Select((x) => result = Vector3.Max(x, result));
        return result;
    }

    public static Vector2 Min(this IEnumerable<Vector2> vectors)
    {
        var result = new Vector3(Mathf.Infinity, Mathf.Infinity);
        vectors.Select((x) => result = Vector2.Min(x, result));
        return result;
    }
    public static Vector2 Max(this IEnumerable<Vector2> vectors)
    {
        var result = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        vectors.Select((x) => result = Vector2.Max(x, result));
        return result;
    }
    public static Vector3 RoundToDigits(Vector3 value, int digits)
    {
        value *= Mathf.Pow(10, digits);
        value = Vector3Int.RoundToInt(value);
        value /= Mathf.Pow(10, digits);
        return value;
    }
    public static Vector2 Sum(this IEnumerable<Vector2> enumerable)
    {
        var sum = new Vector2();
        foreach (var v2 in enumerable)
            sum += v2;
        return sum;
    }

    public static Vector2 ComponentDivide(this Vector2 a, Vector2 b)
    {
        return new Vector2(
            a.x / b.x,
            a.y / b.y
        );
    }

    public static Vector3 ComponentDivide(this Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.x / b.x,
            a.y / b.y,
            a.z / b.z
        );
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, Vector3 t) => new(Mathf.Lerp(a.x, b.x, t.x), Mathf.Lerp(a.y, b.y, t.y), Mathf.Lerp(a.z, b.z, t.z));

    public static Vector3 MaxMagnitude(Vector3 a, Vector3 b) => new(Math.MaxMagnitude(a.x, b.x), Math.MaxMagnitude(a.y, b.y), Math.MaxMagnitude(a.z, b.z));
    public static Vector3 MinMagnitude(Vector3 a, Vector3 b) => new(Math.MinMagnitude(a.x, b.x), Math.MinMagnitude(a.y, b.y), Math.MinMagnitude(a.z, b.z));

    public static Vector3 Scale(this Matrix4x4 matrix)
    {
        return new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude
        );
    }
}