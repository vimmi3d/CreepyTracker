using UnityEngine;
using System;
using Windows.Kinect;

public class HumanSkeleton : MonoBehaviour
{
	private GameObject head;
	private GameObject leftShoulder;
	private GameObject rightShoulder;
	private GameObject leftElbow;
	private GameObject rightElbow;
	private GameObject leftHand;
	private GameObject rightHand;
	private GameObject spineMid;
	private GameObject leftHip;
	private GameObject rightHip;
	private GameObject leftKnee;
	private GameObject rightKnee;
	private GameObject leftFoot;
	private GameObject rightFoot;

    private string handStateLeft = HandState.Unknown.ToString();
    private string handStateRight = HandState.Unknown.ToString();
 
    private AdaptiveDoubleExponentialFilterVector3 headFiltered;
	private AdaptiveDoubleExponentialFilterVector3 neckFiltered;
	private AdaptiveDoubleExponentialFilterVector3 spineShoulderFiltered;
	private AdaptiveDoubleExponentialFilterVector3 spineMidFiltered;
	private AdaptiveDoubleExponentialFilterVector3 spineBaseFiltered;

	private AdaptiveDoubleExponentialFilterVector3 leftShoulderFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftElbowFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftWristFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftHandFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftThumbFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftHandTipFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftHipFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftKneeFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftAnkleFiltered;
	private AdaptiveDoubleExponentialFilterVector3 leftFootFiltered;
    private AdaptiveDoubleExponentialFilterVector3 leftHandScreenSpaceFiltered;
    
    private AdaptiveDoubleExponentialFilterVector3 rightShoulderFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightElbowFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightWristFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightHandFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightThumbFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightHandTipFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightHipFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightKneeFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightAnkleFiltered;
	private AdaptiveDoubleExponentialFilterVector3 rightFootFiltered;
    private AdaptiveDoubleExponentialFilterVector3 rightHandScreenSpaceFiltered;

    public Tracker tracker;
	public int ID;

	private bool canSend = false;

	private bool mirror = false;
	private Vector3 lastForward;

	//private GameObject forwardGO;
	private GameObject floorForwardGameObject;

	void Awake ()
	{
		CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider> ();
		collider.radius = 0.25f;
		collider.height = 1.75f;

		head = createSphere ("head", 0.3f);
		leftShoulder = createSphere ("leftShoulder");
		rightShoulder = createSphere ("rightShoulder");
		leftElbow = createSphere ("leftElbow");
		rightElbow = createSphere ("rightElbow");
		leftHand = createSphere ("leftHand");
		rightHand = createSphere ("rightHand");
		spineMid = createSphere ("spineMid", 0.2f);
		leftHip = createSphere ("leftHip");
		rightHip = createSphere ("rightHip");
		leftKnee = createSphere ("leftKnee");
		rightKnee = createSphere ("rightKnee");
		leftFoot = createSphere ("leftFoot");
		rightFoot = createSphere ("rightFoot");

		headFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		neckFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		spineShoulderFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		spineMidFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		spineBaseFiltered = new AdaptiveDoubleExponentialFilterVector3 ();

		leftShoulderFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftElbowFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftWristFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftHandFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftThumbFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftHandTipFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftHipFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftKneeFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftAnkleFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		leftFootFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
        leftHandScreenSpaceFiltered = new AdaptiveDoubleExponentialFilterVector3 ();

		rightShoulderFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightElbowFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightWristFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightHandFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightThumbFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightHandTipFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightHipFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightKneeFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightAnkleFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
		rightFootFiltered = new AdaptiveDoubleExponentialFilterVector3 ();
        rightHandScreenSpaceFiltered = new AdaptiveDoubleExponentialFilterVector3 ();

		canSend = true;
		lastForward = Vector3.zero;

		//forwardGO = new GameObject();
		//forwardGO.name = "ForwardOld";
		//forwardGO.transform.parent = transform;
		//GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		//cylinder.transform.localScale = new Vector3(0.05f, 0.25f, 0.05f);
		//cylinder.transform.position += new Vector3(0, 0, 0.25f);
		//cylinder.transform.up = Vector3.forward;
		//cylinder.transform.parent = forwardGO.transform;

		floorForwardGameObject = (GameObject)Instantiate (Resources.Load ("Prefabs/FloorForwardPlane"));
		floorForwardGameObject.name = "Forward";
		floorForwardGameObject.tag = "nocolor";
		floorForwardGameObject.transform.parent = transform;
	}

	private GameObject createSphere (string name, float scale = 0.1f)
	{
		GameObject gameObject = GameObject.CreatePrimitive (PrimitiveType.Sphere);
		gameObject.GetComponent<SphereCollider> ().enabled = false;
		gameObject.transform.parent = transform;
		gameObject.transform.localScale = new Vector3 (scale, scale, scale);
		gameObject.name = name;
		return gameObject;
	}

	private Vector3 calcUnfilteredForward ()
	{
		Vector3 spineRight = (mirror ? tracker.getJointPosition (ID, JointType.ShoulderLeft, Vector3.zero) : tracker.getJointPosition (ID, JointType.ShoulderRight, Vector3.zero)) - tracker.getJointPosition (ID, JointType.SpineShoulder, Vector3.zero);
		Vector3 spineUp = tracker.getJointPosition (ID, JointType.SpineShoulder, Vector3.zero) - tracker.getJointPosition (ID, JointType.SpineMid, Vector3.zero);

		return Vector3.Cross (spineRight, spineUp);
	}

	private Vector3 calcForward ()
	{
		Vector3 spineRight = rightShoulderFiltered.Value - spineShoulderFiltered.Value;
		Vector3 spineUp = spineShoulderFiltered.Value - spineMidFiltered.Value;

		return Vector3.Cross (spineRight, spineUp);
	}

	public void updateSkeleton ()
	{
		if (tracker.humanHasBodies(ID))
        {
			// Update Forward (mirror or not to mirror?)
			Vector3 forward = calcUnfilteredForward ();

			if (lastForward != Vector3.zero)
            {
				Vector3 projectedForward = new Vector3 (forward.x, 0, forward.z);
				Vector3 projectedLastForward = new Vector3 (lastForward.x, 0, lastForward.z);

				if (Vector3.Angle (projectedLastForward, projectedForward) > 90) //if (Vector3.Angle(projectedLastForward, -projectedForward) < Vector3.Angle(projectedLastForward, projectedForward)) // the same as above
                {              
					mirror = !mirror;
					forward = calcUnfilteredForward ();
					projectedForward = new Vector3 (forward.x, 0, forward.z);
				}

				// Front for sure?

				Vector3 elbowHand1 = tracker.getJointPosition (ID, JointType.HandRight, rightHandFiltered.Value) - tracker.getJointPosition (ID, JointType.ElbowRight, rightElbowFiltered.Value);
				Vector3 elbowHand2 = tracker.getJointPosition (ID, JointType.HandLeft, leftHandFiltered.Value) - tracker.getJointPosition (ID, JointType.ElbowLeft, leftElbowFiltered.Value);

				if (Vector3.Angle (elbowHand1, -projectedForward) < 30 || Vector3.Angle (elbowHand2, -projectedForward) < 30)
                {
					mirror = !mirror;
					forward = calcUnfilteredForward ();
				}
			}

			lastForward = forward;

            handStateLeft = tracker.getHandState(ID, BodyPropertiesTypes.HandLeftState);
            handStateRight = tracker.getHandState(ID, BodyPropertiesTypes.HandRightState);

            // Update Joints
            try
            {
				headFiltered.Value = tracker.getJointPosition (ID, JointType.Head, headFiltered.Value);
				neckFiltered.Value = tracker.getJointPosition (ID, JointType.Neck, neckFiltered.Value);
				spineShoulderFiltered.Value = tracker.getJointPosition (ID, JointType.SpineShoulder, spineShoulderFiltered.Value);
				spineMidFiltered.Value = tracker.getJointPosition (ID, JointType.SpineMid, spineMidFiltered.Value);
				spineBaseFiltered.Value = tracker.getJointPosition (ID, JointType.SpineBase, spineBaseFiltered.Value);

                leftHandScreenSpaceFiltered.Value = tracker.getHandScreenSpace(ID, HandScreenSpace.HandLeftPosition);
                rightHandScreenSpaceFiltered.Value = tracker.getHandScreenSpace(ID, HandScreenSpace.HandRightPosition);

                if (mirror)
                {
					rightShoulderFiltered.Value = tracker.getJointPosition (ID, JointType.ShoulderLeft, rightShoulderFiltered.Value);
					rightElbowFiltered.Value = tracker.getJointPosition (ID, JointType.ElbowLeft, rightElbowFiltered.Value);
					rightWristFiltered.Value = tracker.getJointPosition (ID, JointType.WristLeft, rightWristFiltered.Value);
					rightHandFiltered.Value = tracker.getJointPosition (ID, JointType.HandLeft, rightHandFiltered.Value);
					rightThumbFiltered.Value = tracker.getJointPosition (ID, JointType.ThumbLeft, rightThumbFiltered.Value);
					rightHandTipFiltered.Value = tracker.getJointPosition (ID, JointType.HandTipLeft, rightHandTipFiltered.Value);
					rightHipFiltered.Value = tracker.getJointPosition (ID, JointType.HipLeft, rightHipFiltered.Value);
					rightKneeFiltered.Value = tracker.getJointPosition (ID, JointType.KneeLeft, rightKneeFiltered.Value);
					rightAnkleFiltered.Value = tracker.getJointPosition (ID, JointType.AnkleLeft, rightAnkleFiltered.Value);
					rightFootFiltered.Value = tracker.getJointPosition (ID, JointType.FootLeft, rightFootFiltered.Value);

					leftShoulderFiltered.Value = tracker.getJointPosition (ID, JointType.ShoulderRight, leftShoulderFiltered.Value);
					leftElbowFiltered.Value = tracker.getJointPosition (ID, JointType.ElbowRight, leftElbowFiltered.Value);
					leftWristFiltered.Value = tracker.getJointPosition (ID, JointType.WristRight, leftWristFiltered.Value);
					leftHandFiltered.Value = tracker.getJointPosition (ID, JointType.HandRight, leftHandFiltered.Value);
					leftThumbFiltered.Value = tracker.getJointPosition (ID, JointType.ThumbRight, leftThumbFiltered.Value);
					leftHandTipFiltered.Value = tracker.getJointPosition (ID, JointType.HandTipRight, leftHandTipFiltered.Value);
					leftHipFiltered.Value = tracker.getJointPosition (ID, JointType.HipRight, leftHipFiltered.Value);
					leftKneeFiltered.Value = tracker.getJointPosition (ID, JointType.KneeRight, leftKneeFiltered.Value);
					leftAnkleFiltered.Value = tracker.getJointPosition (ID, JointType.AnkleRight, leftAnkleFiltered.Value);
					leftFootFiltered.Value = tracker.getJointPosition (ID, JointType.FootRight, leftFootFiltered.Value);
				}
                else
                {
					leftShoulderFiltered.Value = tracker.getJointPosition (ID, JointType.ShoulderLeft, leftShoulderFiltered.Value);
					leftElbowFiltered.Value = tracker.getJointPosition (ID, JointType.ElbowLeft, leftElbowFiltered.Value);
					leftWristFiltered.Value = tracker.getJointPosition (ID, JointType.WristLeft, leftWristFiltered.Value);
					leftHandFiltered.Value = tracker.getJointPosition (ID, JointType.HandLeft, leftHandFiltered.Value);
					leftThumbFiltered.Value = tracker.getJointPosition (ID, JointType.ThumbLeft, leftThumbFiltered.Value);
					leftHandTipFiltered.Value = tracker.getJointPosition (ID, JointType.HandTipLeft, leftHandTipFiltered.Value);
					leftHipFiltered.Value = tracker.getJointPosition (ID, JointType.HipLeft, leftHipFiltered.Value);
					leftKneeFiltered.Value = tracker.getJointPosition (ID, JointType.KneeLeft, leftKneeFiltered.Value);
					leftAnkleFiltered.Value = tracker.getJointPosition (ID, JointType.AnkleLeft, leftAnkleFiltered.Value);
					leftFootFiltered.Value = tracker.getJointPosition (ID, JointType.FootLeft, leftFootFiltered.Value);

					rightShoulderFiltered.Value = tracker.getJointPosition (ID, JointType.ShoulderRight, rightShoulderFiltered.Value);
					rightElbowFiltered.Value = tracker.getJointPosition (ID, JointType.ElbowRight, rightElbowFiltered.Value);
					rightWristFiltered.Value = tracker.getJointPosition (ID, JointType.WristRight, rightWristFiltered.Value);
					rightHandFiltered.Value = tracker.getJointPosition (ID, JointType.HandRight, rightHandFiltered.Value);
					rightThumbFiltered.Value = tracker.getJointPosition (ID, JointType.ThumbRight, rightThumbFiltered.Value);
					rightHandTipFiltered.Value = tracker.getJointPosition (ID, JointType.HandTipRight, rightHandTipFiltered.Value);
                    rightHipFiltered.Value = tracker.getJointPosition (ID, JointType.HipRight, rightHipFiltered.Value);
					rightKneeFiltered.Value = tracker.getJointPosition (ID, JointType.KneeRight, rightKneeFiltered.Value);
					rightAnkleFiltered.Value = tracker.getJointPosition (ID, JointType.AnkleRight, rightAnkleFiltered.Value);
					rightFootFiltered.Value = tracker.getJointPosition (ID, JointType.FootRight, rightFootFiltered.Value);
				}

				head.transform.position = headFiltered.Value;
				leftShoulder.transform.position = leftShoulderFiltered.Value;
				rightShoulder.transform.position = rightShoulderFiltered.Value;
				leftElbow.transform.position = leftElbowFiltered.Value;
				rightElbow.transform.position = rightElbowFiltered.Value;
				leftHand.transform.position = leftHandFiltered.Value;
				rightHand.transform.position = rightHandFiltered.Value;
				spineMid.transform.position = spineMidFiltered.Value;
				leftHip.transform.position = leftHipFiltered.Value;
				rightHip.transform.position = rightHipFiltered.Value;
				leftKnee.transform.position = leftKneeFiltered.Value;
				rightKnee.transform.position = rightKneeFiltered.Value;
				leftFoot.transform.position = leftFootFiltered.Value;
				rightFoot.transform.position = rightFootFiltered.Value;

				// update forward
				Vector3 fw = calcForward ();
				Vector3 pos = spineMid.transform.position;

				//forwardGO.transform.forward = fw;
				//forwardGO.transform.position = pos;
				floorForwardGameObject.transform.forward = new Vector3 (fw.x, 0, fw.z);
				floorForwardGameObject.transform.position = new Vector3 (pos.x, 0.001f, pos.z);
				floorForwardGameObject.transform.parent = transform;
			}
            catch (Exception e)
            {
				Debug.Log (e.Message + "\n" + e.StackTrace);
			}
		}
	}

	internal string getPDU ()
	{
        if (canSend)
        {
            string pdu = BodyPropertiesTypes.UID.ToString() + MessageSeparators.SET + ID + MessageSeparators.L2;

            pdu += BodyPropertiesTypes.HandLeftState.ToString() + MessageSeparators.SET + handStateLeft + MessageSeparators.L2;
            pdu += BodyPropertiesTypes.HandLeftConfidence.ToString() + MessageSeparators.SET + "Null" + MessageSeparators.L2;
            pdu += BodyPropertiesTypes.HandRightState.ToString() + MessageSeparators.SET + handStateRight + MessageSeparators.L2;
            pdu += BodyPropertiesTypes.HandRightConfidence.ToString() + MessageSeparators.SET + "Null" + MessageSeparators.L2;

            pdu += HandScreenSpace.HandLeftPosition.ToString() + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftHandScreenSpaceFiltered.Value) + MessageSeparators.L2;
            pdu += HandScreenSpace.HandRightPosition.ToString() + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightHandScreenSpaceFiltered.Value) + MessageSeparators.L2;

            pdu += "head" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(headFiltered.Value) + MessageSeparators.L2;
            pdu += "neck" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(neckFiltered.Value) + MessageSeparators.L2;
            pdu += "spineShoulder" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(spineShoulderFiltered.Value) + MessageSeparators.L2;
            pdu += "spineMid" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(spineMidFiltered.Value) + MessageSeparators.L2;
            pdu += "spineBase" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(spineBaseFiltered.Value) + MessageSeparators.L2;

            pdu += "leftShoulder" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftShoulderFiltered.Value) + MessageSeparators.L2;
            pdu += "leftElbow" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftElbowFiltered.Value) + MessageSeparators.L2;
            pdu += "leftWrist" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftWristFiltered.Value) + MessageSeparators.L2;
            pdu += "leftHand" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftHandFiltered.Value) + MessageSeparators.L2;
            pdu += "leftThumb" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftThumbFiltered.Value) + MessageSeparators.L2;
            pdu += "leftHandTip" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftHandTipFiltered.Value) + MessageSeparators.L2;
            pdu += "leftHip" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftHipFiltered.Value) + MessageSeparators.L2;
            pdu += "leftKnee" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftKneeFiltered.Value) + MessageSeparators.L2;
            pdu += "leftAnkle" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftAnkleFiltered.Value) + MessageSeparators.L2;
            pdu += "leftFoot" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(leftFootFiltered.Value) + MessageSeparators.L2;

            pdu += "rightShoulder" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightShoulderFiltered.Value) + MessageSeparators.L2;
            pdu += "rightElbow" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightElbowFiltered.Value) + MessageSeparators.L2;
            pdu += "rightWrist" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightWristFiltered.Value) + MessageSeparators.L2;
            pdu += "rightHand" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightHandFiltered.Value) + MessageSeparators.L2;
            pdu += "rightThumb" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightThumbFiltered.Value) + MessageSeparators.L2;
            pdu += "rightHandTip" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightHandTipFiltered.Value) + MessageSeparators.L2;
            pdu += "rightHip" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightHipFiltered.Value) + MessageSeparators.L2;
            pdu += "rightKnee" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightKneeFiltered.Value) + MessageSeparators.L2;
            pdu += "rightAnkle" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightAnkleFiltered.Value) + MessageSeparators.L2;
            pdu += "rightFoot" + MessageSeparators.SET + CommonUtils.convertVectorToStringRPC(rightFootFiltered.Value);

            return pdu;
        }
        else
        {
            throw new Exception("Human not initalized.");
        }
	}

	public Vector3 getHead ()
	{
		return headFiltered.Value;
	}
}