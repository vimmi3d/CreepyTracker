using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public static class MessageSeparators
{
    public const char L0 = '$'; // header separator
    public const char L1 = '#'; // top level separator -> bodies
    public const char L2 = '/'; // -> body attributes
    public const char L3 = ':'; // -> 3D values
    public const char SET = '=';
}*/

public static class CommonUtils
{
    public static Vector3 networkStringToVector3(string value, char messageSeparator)
    {
        Vector3 v3 = Vector3.zero;

        string[] values = value.Split(messageSeparator);

        if (values.Length != 3)
        {
            throw new Exception("You received a stupid surface");
        }

        v3.x = float.Parse(values[0]);
        v3.y = float.Parse(values[1]);
        v3.z = float.Parse(values[2]);

        return v3;
    }

    public static void drawSurface(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Color color)
    {
        Debug.DrawLine(A, B, color);
        Debug.DrawLine(B, C, color);
        Debug.DrawLine(C, D, color);
        Debug.DrawLine(D, A, color);
    }
}
