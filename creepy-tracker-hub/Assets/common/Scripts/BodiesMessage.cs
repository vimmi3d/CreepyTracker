using UnityEngine;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using System;

public static class MessageSeparators
{
	public const char L0 = '$'; // header separator
    public const char L1 = '#'; // top level separator -> bodies
    public const char L2 = '/'; // -> body attributes
    public const char L3 = ':'; // -> 3D values
    public const char SET = '=';
}

public enum HandScreenSpace
{
    HandLeftPosition,
    HandRightPosition
}

public enum BodyPropertiesTypes
{
    UID,
    HandLeftState,
    HandLeftConfidence,
    HandRightState,
    HandRightConfidence,
    Confidence
}

public class Skeleton
{
    public Dictionary<BodyPropertiesTypes, string> bodyProperties;
    public Dictionary<Kinect.JointType, Vector3> jointsPositions;
    public Dictionary<HandScreenSpace, Vector3> handScreenPositions;
    public string Message;
    public string ID
    {
        get
        {
            return bodyProperties[BodyPropertiesTypes.UID];
        }
    }

    public string sensorID;


    public void _start()
    {
        bodyProperties = new Dictionary<BodyPropertiesTypes, string>();
        jointsPositions = new Dictionary<Windows.Kinect.JointType, Vector3>();
        handScreenPositions = new Dictionary<HandScreenSpace, Vector3>();
    }

    public Skeleton(string body)
    {
        _start();

        Message = body;
        List<string> bodyAttributes = new List<string>(body.Split(MessageSeparators.L2));
        foreach (string attr in bodyAttributes)
        {
            string[] statement = attr.Split(MessageSeparators.SET);
            if (statement.Length == 2)
            {
                if (Enum.IsDefined(typeof(BodyPropertiesTypes), statement[0]))
                {
                    bodyProperties[((BodyPropertiesTypes)Enum.Parse(typeof(BodyPropertiesTypes), statement[0]))] = statement[1];
                }

                if (Enum.IsDefined(typeof(Windows.Kinect.JointType), statement[0]))
                {
                    jointsPositions[((Windows.Kinect.JointType)Enum.Parse(typeof(Windows.Kinect.JointType), statement[0]))] = CommonUtils.convertRpcStringToVector3(statement[1]);
                }

                if (Enum.IsDefined(typeof(HandScreenSpace), statement[0]))
                {
                    handScreenPositions[((HandScreenSpace)Enum.Parse(typeof(HandScreenSpace), statement[0]))] = CommonUtils.convertRpcStringToVector3(statement[1]);
                }
            }
        }
    }

    private int BodyConfidence(Kinect.Body body)
    {
        int confidence = 0;

        foreach (Kinect.Joint j in body.Joints.Values)
        {
            if (j.TrackingState == Windows.Kinect.TrackingState.Tracked)
                confidence += 1;
        }

        return confidence;
    }
}

public class BodiesMessageException : Exception
{
    public BodiesMessageException(string message)
        : base(message) { }
}

public class BodiesMessage
{
    public string Message { get; internal set; }
    public string KinectId { get; internal set; }

    public List<Skeleton> _bodies;
    public int NumberOfBodies { get { return _bodies.Count; } }
    public List<Skeleton> Bodies { get { return _bodies; } }

    private void _start()
    {
        _bodies = new List<Skeleton>();
    }

    public BodiesMessage(string bodies)
    {
        _start();
        Message = bodies;

        try
        {
            List<string> pdu = new List<string>(bodies.Split(MessageSeparators.L1));
            KinectId = pdu[0];
            pdu.RemoveAt(0);

            foreach (string b in pdu)
            {
                if (b != "None") _bodies.Add(new Skeleton(b));
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.StackTrace);
            throw new BodiesMessageException("Cannot instantiate BodiesMessage: " + e.Message);
        }
    }
}