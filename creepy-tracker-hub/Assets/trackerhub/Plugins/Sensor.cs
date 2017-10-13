using UnityEngine;
using System.Collections.Generic;
using System;

public class Sensor
{
	public string SensorID;
	public BodiesMessage lastBodiesMessage;

	public Dictionary<string, SensorBody> bodies;

	public PointCloudSimple lastCloud;
	private GameObject _sensorGameObject;

	public GameObject SensorGameObject { get { return _sensorGameObject; } }

	private bool _active;

	private DateTime _lastUpdate;

	private Vector3 center1;
	private Vector3 center2;
	private Vector3 up1;
	private Vector3 up2;

	private List<Vector3> _floorValues;

	private static int materialIndex = 0;
	private static List<Material> _materials;

	private Material _material;

	private List<Vector3> _bestFitPlanePoints;

	public bool Active
    {
		get
        {
			return _active;
		}

		set
        {
			_active = value;
			_sensorGameObject.SetActive (_active);
		}
	}

	public Vector3 CalibAuxPoint
    {
		get
        {
			return center1;
		}
	}

	public Material Material
    {
		get
        {
			return _material;
		}

		set
        {
			_material = value;
		}
	}

	public Sensor (string sensorID, GameObject sensorGameObject)
	{
		bodies = new Dictionary<string, SensorBody> ();
		_active = true;
		SensorID = sensorID;
		lastBodiesMessage = null;
		_sensorGameObject = sensorGameObject;
		SensorGameObject.name = sensorID;
		center1 = new Vector3 ();
		center2 = new Vector3 ();
		up1 = new Vector3 ();
		up2 = new Vector3 ();
		_floorValues = new List<Vector3> ();
		_bestFitPlanePoints = new List<Vector3> ();

		_material = _chooseMaterial ();
		CommonUtils.changeGameObjectMaterial (_sensorGameObject, _material);
		GameObject cloudobj = new GameObject ("PointCloud");
		cloudobj.transform.SetParent(sensorGameObject.transform);
		cloudobj.transform.localPosition = Vector3.zero;
		cloudobj.transform.localRotation = Quaternion.identity;
		cloudobj.transform.localScale = new Vector3 (-1, 1, 1);
		cloudobj.AddComponent<PointCloudSimple> ();
		lastCloud = cloudobj.GetComponent<PointCloudSimple> ();
	}

	internal Vector3 pointSensorToScene (Vector3 p)
	{
		return SensorGameObject.transform.localToWorldMatrix.MultiplyPoint (p);
	}
    
	internal void updateBodies ()
	{
		BodiesMessage bodiesMessage = lastBodiesMessage;

		if (bodiesMessage == null)
			return;

		foreach (KeyValuePair<string, SensorBody> sb in bodies)
        {
			sb.Value.updated = false;
		}

		// refresh bodies position
		foreach (Skeleton sk in bodiesMessage.Bodies)
        {
			SensorBody b;

			if (int.Parse (sk.bodyProperties [BodyPropertiesTypes.Confidence]) < TrackerProperties.Instance.confidenceTreshold)
            {
				if (bodies.ContainsKey (sk.ID))
                {
					b = bodies [sk.ID];
					b.updated = true;
					b.lastUpdated = DateTime.Now;
				}
				continue;
			}

			if (bodies.ContainsKey (sk.ID)) {   //existing bodies
				b = bodies [sk.ID];
			}
            else  // new bodies
            {  
				b = new SensorBody (sk.ID, SensorGameObject.transform);
				b.gameObject.GetComponent<Renderer> ().material = Material;
				bodies [sk.ID] = b;
				b.sensorID = SensorID;
			}

			b.LocalPosition = CommonUtils.pointKinectToUnity (sk.jointsPositions [Windows.Kinect.JointType.SpineBase]);
			b.updated = true;
			b.lastUpdated = DateTime.Now;
			b.skeleton = sk;
		}

		// remove bodies no longer present
		List<string> keysToRemove = new List<string> ();
		foreach (KeyValuePair<string, SensorBody> sb in bodies)
        {
			if (!sb.Value.updated)
            {
				GameObject.Destroy (sb.Value.gameObject);
				keysToRemove.Add (sb.Key);
			}
		}
		foreach (string key in keysToRemove)
        {
			bodies.Remove (key);
		}
	}

	internal void calibrationStep1 ()
	{
		center1 = CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.centerJoint]);
		up1 = CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.upJointB]) - CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.upJointA]);

		_floorValues.Add (CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [Windows.Kinect.JointType.FootLeft]));
		_floorValues.Add (CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [Windows.Kinect.JointType.FootRight]));
	}

	internal void calibrationStep2 ()
	{
		center2 = CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.centerJoint]);
		up2 = CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.upJointB]) - CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [TrackerProperties.Instance.upJointA]);

		_floorValues.Add (CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [Windows.Kinect.JointType.FootLeft]));
		_floorValues.Add (CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [Windows.Kinect.JointType.FootRight]));

		// Begin calibration calculations

		Vector3 up = (up1 + up2) / 2.0f;
		Vector3 forward = center2 - center1;
		Vector3 right = Vector3.Cross (up, forward);
		forward = Vector3.Cross (right, up);

		Debug.Log (up);

		doCalibCalcs (up, forward, right);
	}

	internal void calibrationStep3 ()
	{
		Vector3 p = CommonUtils.pointKinectToUnity (lastBodiesMessage.Bodies [0].jointsPositions [Windows.Kinect.JointType.Head]);
		if (_bestFitPlanePoints.Count == 0 || p != _bestFitPlanePoints [_bestFitPlanePoints.Count - 1]) {
			_bestFitPlanePoints.Add (p);

			GameObject go = GameObject.CreatePrimitive (PrimitiveType.Cube);
			go.transform.localScale = new Vector3 (0.05f, 0.05f, 0.05f);
			go.transform.position = p;
		}
	}

	internal void calibrationStep4 ()
	{
		Vector3 normal = computeBestFitNormal (_bestFitPlanePoints.ToArray (), _bestFitPlanePoints.Count);//new Vector3 (nx, ny, nz).normalized;
		if (normal.y < 0)
			normal = -normal;

		Debug.Log (normal);

		// Begin calibration calculations

		Vector3 forward = center2 - center1;
		Vector3 right = Vector3.Cross (normal, forward);
		forward = Vector3.Cross (right, normal);

		doCalibCalcs (normal, forward, right);

		// end

		_bestFitPlanePoints.Clear ();
	}

	// Thanks Sara :)
	private Vector3 computeBestFitNormal (Vector3[] v, int n)
	{
		// Zero out sum
		Vector3 result = Vector3.zero;

		// Start with the ‘‘ previous’’ vertex as the last one.
		// This avoids an if−statement in the loop
		Vector3 p = v [n - 1];

		// Iterate through the vertices
		for (int i = 0; i < n; ++i) {

			// Get shortcut to the ‘‘current’’ vertex
			Vector3 c = v [i];

			// Add in edge vector products appropriately
			result.x += (p.z + c.z) * (p.y - c.y);
			result.y += (p.x + c.x) * (p.z - c.z);
			result.z += (p.y + c.y) * (p.x - c.x);

			// Next vertex, please
			p = c;
		}

		// Normalize the result and return it
		result.Normalize ();
		return result;
	}

	private void doCalibCalcs (Vector3 up, Vector3 forward, Vector3 right)
	{
		SensorGameObject.transform.position = Vector3.zero;
		SensorGameObject.transform.rotation = Quaternion.identity;

		GameObject centerGO = new GameObject ();
		centerGO.transform.parent = SensorGameObject.transform;
		centerGO.transform.rotation = Quaternion.LookRotation (forward, up);
		centerGO.transform.position = center1;

		centerGO.transform.parent = null;
		SensorGameObject.transform.parent = centerGO.transform;
		centerGO.transform.position = Vector3.zero;
		centerGO.transform.rotation = Quaternion.identity;

		SensorGameObject.transform.parent = null;
		GameObject.Destroy (centerGO);

		Vector3 minv = new Vector3 ();
		float min = float.PositiveInfinity;

		foreach (Vector3 v in _floorValues)
        {
			Vector3 tmp = pointSensorToScene (v);

			if (tmp.y < min)
				minv = tmp;
		}

		move (new Vector3 (0, -minv.y, 0));
	}

	internal void move (Vector3 positiondelta)
	{
		SensorGameObject.transform.position += positiondelta;
	}

	private static Material _chooseMaterial ()
	{
		if (_materials == null) {
			System.Random rng = new System.Random ();
			int n = CommonUtils.colors.Count;
			while (n > 1) {
				n--;
				int k = rng.Next (n + 1);
				Color value = CommonUtils.colors [k];
				CommonUtils.colors [k] = CommonUtils.colors [n];
				CommonUtils.colors [n] = value;
			}

			_materials = new List<Material> ();
			for (int i = 0; i < CommonUtils.colors.Count; i++) {
				Material aux = new Material (Shader.Find ("Standard"));
				aux.color = CommonUtils.colors [i];
				_materials.Add (aux);
			}
		}
		Material m = _materials [materialIndex];
		materialIndex = (materialIndex + 1) % _materials.Count;
		return m;
	}
}