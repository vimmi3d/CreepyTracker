using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static class MessageSeparators {
    public const char L0 = '$'; // header separator
    public const char L1 = '#'; // top level separator -> bodies
	public const char L2 = '/'; // -> body attributes
	public const char L3 = ':'; // -> 3D values
	public const char SET = '=';
}

public enum BodyJointType
{
	head,
	
	neck,
	spineShoulder,
	spineMid,
	spineBase,
	
	leftShoulder,
	leftElbow,
	leftWrist,
	leftHand,
	leftThumb,
	leftHandTip,
	
	leftHip,
	leftKnee,
	leftAnkle,
	leftFoot,
	
	rightShoulder,
	rightElbow,
	rightWrist,
	rightHand,
	rightThumb,
	rightHandTip,
	
	rightHip,
	rightKnee,
	rightAnkle,
	rightFoot
}

public enum BodyPropertiesType
{
	UID,
	HandLeftState,
	HandLeftConfidence,
	HandRightState,
	HandRightConfidence
}

public class Body
{
	private Dictionary<BodyJointType, Vector3> _joints;
	public Dictionary<BodyJointType, Vector3> Joints { get { return _joints; } }

	private Dictionary<BodyPropertiesType, string> _properties;
	public Dictionary<BodyPropertiesType, string> Properties { get { return _properties; } }

	private void _start()
	{
		_joints = new Dictionary<BodyJointType, Vector3>();
		_properties = new Dictionary<BodyPropertiesType, string>();
	}

	public Body()
	{
		_start();
	}

	public Body(string pdu)
	{
		_start();

		List<string> bodyAttributes = new List<string>(pdu.Split(MessageSeparators.L2));
		foreach (string attr in bodyAttributes)
		{
			string [] statement = attr.Split(MessageSeparators.SET);
			if (statement.Length == 2)
			{
				if (Enum.IsDefined(typeof(BodyPropertiesType), statement[0]))
				{
					_properties[((BodyPropertiesType)Enum.Parse(typeof(BodyPropertiesType), statement[0]))] = statement[1];

				}

				if (Enum.IsDefined(typeof(BodyJointType), statement[0]))
				{
					_joints[((BodyJointType)Enum.Parse(typeof(BodyJointType), statement[0]))] = _convertBodyJointStringToVector3(statement[1]);
				}
			}
		}
	}

	private static Vector3 _convertBodyJointStringToVector3(string strJoint)
	{
		string[] p = strJoint.Split(MessageSeparators.L3);
		return new Vector3(float.Parse(p[0].Replace(',','.')), float.Parse(p[1].Replace(',', '.')), float.Parse(p[2].Replace(',', '.')));
	}
}
