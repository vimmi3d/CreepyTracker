using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class CommonUtils
{
    internal const int decimalsRound = 3;

    internal static Vector3 _convertToVector3(Kinect.CameraSpacePoint p)
    {
        return new Vector3(p.X, p.Y, p.Z);
    }

    internal static string convertVectorToStringRPC(Vector3 v)
    {
        return "" + Math.Round(v.x, decimalsRound) + MessageSeparators.L3 + Math.Round(v.y, decimalsRound) + MessageSeparators.L3 + Math.Round(v.z, decimalsRound);
    }

    internal static string convertCameraDepthPointToStringRPC(Kinect.DepthSpacePoint p)
    {
        return "" + Math.Round(p.X, 3) + MessageSeparators.L3 + Math.Round(p.Y, 3) + MessageSeparators.L3 + 0;
    }

    internal static string convertQuaternionToStringRPC(Quaternion v)
    {
        return "" + v.w + MessageSeparators.L3 + v.x + MessageSeparators.L3 + v.y + MessageSeparators.L3 + v.y;
    }

    internal static Vector3 convertRpcStringToVector3(string v)
    {
        string[] p = v.Split(MessageSeparators.L3);
        return new Vector3(float.Parse(p[0].Replace(',','.')), float.Parse(p[1].Replace(',', '.')), float.Parse(p[2].Replace(',', '.')));
    }

    internal static string convertVectorToStringRPC(Kinect.CameraSpacePoint position)
    {
        return convertVectorToStringRPC(new Vector3(position.X, position.Y, position.Z));
    }

    internal static Quaternion convertRpcStringToQuaternion(string v)
    {
        string[] p = v.Split(MessageSeparators.L3);
        return new Quaternion(float.Parse(p[1].Replace(',', '.')), float.Parse(p[2].Replace(',', '.')), float.Parse(p[2].Replace(',', '.')), float.Parse(p[0].Replace(',', '.')));
    }

    internal static Vector3 CenterOfVectors(Vector3[] vectors)
    {
        Vector3 sum = Vector3.zero;
        if (vectors == null || vectors.Length == 0)
        {
            return sum;
        }

        foreach (Vector3 vec in vectors)
        {
            sum += vec;
        }
        return sum / vectors.Length;
    }

    internal static void changeGameObjectMaterial(GameObject go, Material mat)
    {
        if (go.GetComponent<Renderer>() != null) go.GetComponent<Renderer>().material = mat;
        foreach (Transform child in go.transform)
        {
            if (child.gameObject.GetComponent<Renderer>() != null && child.gameObject.tag != "nocolor") child.gameObject.GetComponent<Renderer>().material = mat;
        }
    }

    internal static GameObject newGameObject(Vector3 v)
    {
        GameObject go = new GameObject();
        go.transform.position = v;
        return go;
    }

    private static int userIDs = 0;
    public static int getNewID()
    {
        return ++userIDs;
    }

    internal static Vector3 pointKinectToUnity(Vector3 p)
    {
        return new Vector3(-p.x, p.y, p.z);
    }

    public static List<Color> colors = new List<Color>()
    {
        //Color.red,
        hexToColor("#e9b96e"),
        hexToColor("#fce94f"),
        hexToColor("#8ae234"),
        hexToColor("#fcaf3e"),
        hexToColor("#729fcf"),
        hexToColor("#ad7fa8"),
        hexToColor("#cc0000"),
        hexToColor("#4e9a06"),
        hexToColor("#ce5c00"),
        hexToColor("#204a87"),
        hexToColor("#5c3566")
    };

    internal static Color hexToColor(string hex)
    {
        hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
        byte a = 255;//assume fully visible unless specified in hex
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //Only use alpha if the string has enough characters
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }
}