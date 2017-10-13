using UnityEngine;
using System.Collections.Generic;
using System;

public class Human
{
    public int ID;
    public List<SensorBody> bodies;
    public GameObject gameObject;
    public DateTime timeOfDeath;
    public string seenBySensor;
    private HumanSkeleton skeleton;
    public HumanSkeleton Skeleton
    {
        get { return skeleton; }
    }

    private Vector3 position;
    public Vector3 Position
    {
        get { return position; }
        set { position = value; gameObject.transform.position = position; }
    }

    public Human(GameObject gameObject, Tracker tracker)
    {
        ID = CommonUtils.getNewID();
        bodies = new List<SensorBody>();
        this.gameObject = gameObject;
        this.gameObject.name = "Human " + ID;

        skeleton = this.gameObject.GetComponent<HumanSkeleton>();
        skeleton.tracker = tracker;
        skeleton.ID = ID;
        skeleton.updateSkeleton();
    }

    internal void updateSkeleton()
    {
        skeleton.updateSkeleton();
		Vector3 leftKneeAcum = new Vector3 ();
		Vector3 rightKneeAcum = new Vector3 ();
        
		foreach (SensorBody body in bodies) {
            Sensor sensor = skeleton.tracker.Sensors[body.sensorID];
            leftKneeAcum += sensor.pointSensorToScene(CommonUtils.pointKinectToUnity(body.skeleton.jointsPositions[Windows.Kinect.JointType.KneeLeft]));
            rightKneeAcum += sensor.pointSensorToScene(CommonUtils.pointKinectToUnity(body.skeleton.jointsPositions[Windows.Kinect.JointType.KneeRight]));
		}

		leftKneeAvg = leftKneeAcum / (float) bodies.Count;
		rightKneeAvg = rightKneeAcum / (float)bodies.Count;
    }

    internal string getPDU()
    {
        string rightKneeStr = CommonUtils.convertVectorToStringRPC(rightKneeAvg);
        string leftKneeStr = CommonUtils.convertVectorToStringRPC(leftKneeAvg);

        /*string rightKneeStr2 = CommonUtils.convertVectorToStringRPC(bodies[0].skeleton.jointsPositions[Windows.Kinect.JointType.KneeRight]);
        string leftKneeStr2 = CommonUtils.convertVectorToStringRPC(bodies[0].skeleton.jointsPositions[Windows.Kinect.JointType.KneeLeft]);
        */
        //Debug.Log("RIGHTKNEE=" + rightKneeStr + "   AVG=");

        string specialKneesStr = "leftKneeAvg" + MessageSeparators.SET + leftKneeStr + MessageSeparators.L2 + "rightKneeAvg" + MessageSeparators.SET + rightKneeStr + MessageSeparators.L2;
        return "Sensor" + MessageSeparators.SET + seenBySensor + MessageSeparators.L2 + Skeleton.getPDU() + MessageSeparators.L2 + specialKneesStr;
	}
}