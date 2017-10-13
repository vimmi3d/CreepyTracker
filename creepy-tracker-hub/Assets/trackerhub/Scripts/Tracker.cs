using UnityEngine;
using System.Collections.Generic;
using System;
using Windows.Kinect;
using System.Net.Sockets;
using System.Net;
using System.Text;

public enum CalibrationProcess
{
	None,
	FindCenter,
	FindForward,
	GetPlane,
	CalcNormal
}

public class Tracker : MonoBehaviour
{

	private Dictionary<string, Sensor> _sensors;

	public Dictionary<string, Sensor> Sensors
	{
		get { return _sensors; }
	}

	private CalibrationProcess _calibrationStatus;
	public CalibrationProcess CalibrationStatus 
    {
		get { return _calibrationStatus; }
		set { _calibrationStatus = value; }
	}

    private List<Surface> _surfaces;

	private Dictionary<int, Human> _humans;

	private List<Human> _deadHumans;
	private List<Human> _humansToKill;

	private UdpBroadcast _udpBroadcast;

	public Material WhiteMaterial;

	public string[] UnicastClients { get { return _udpBroadcast.UnicastClients; } }

	public int showHumanBodies = -1;

	public bool colorHumans;

    public Material SurfaceMaterial;

    void Start ()
	{
        _surfaces = new List<Surface>();
        _sensors = new Dictionary<string, Sensor> ();
		_humans = new Dictionary<int, Human> ();
		_deadHumans = new List<Human> ();
		_humansToKill = new List<Human> ();
		_calibrationStatus = CalibrationProcess.FindCenter;

		_udpBroadcast = new UdpBroadcast (TrackerProperties.Instance.broadcastPort);

		_loadConfig ();
		_loadSavedSensors ();
    }

	void FixedUpdate ()
	{
		if (Input.GetKeyDown (KeyCode.C))
			colorHumans = !colorHumans;

		foreach (Sensor s in _sensors.Values)
        {
			s.updateBodies ();
		}

		mergeHumans ();

		List<Human> deadHumansToRemove = new List<Human> ();
		foreach (Human h in _deadHumans)
        {
			if (DateTime.Now > h.timeOfDeath.AddMilliseconds (1000))
				deadHumansToRemove.Add (h);
		}

		foreach (Human h in deadHumansToRemove)
        {
			Destroy (h.gameObject);
			_deadHumans.Remove (h);
		}

		foreach (Human h in _humansToKill)
        {
			Destroy (h.gameObject);
		}
		_humansToKill.Clear ();

		// udp broadcast

		string strToSend = "" + _humans.Count;

		foreach (Human h in _humans.Values)
        {
            // update Human Skeleton
            h.updateSkeleton();

			// get PDU
			try {
				strToSend += MessageSeparators.L1 + h.getPDU ();
			} catch (Exception /*e*/) { /***/ }
		}

		foreach (Human h in _deadHumans)
        {
			try {
				strToSend += MessageSeparators.L1 + h.getPDU ();
			} catch (Exception /*e*/) { /***/ }
		}

		_udpBroadcast.send (strToSend);

		// set human material

		foreach (Human h in _humans.Values)
        {
			if (h.seenBySensor != null && colorHumans)
				CommonUtils.changeGameObjectMaterial (h.gameObject, Sensors [h.seenBySensor].Material);
			else if (!colorHumans)
				CommonUtils.changeGameObjectMaterial (h.gameObject, WhiteMaterial);
		}

		// show / hide human bodies

		if (showHumanBodies != -1 && !_humans.ContainsKey (showHumanBodies))
			showHumanBodies = -1;

		foreach (Human h in _humans.Values)
        {
			CapsuleCollider collider = h.gameObject.GetComponent<CapsuleCollider> ();
			if (collider != null)
				collider.enabled = (showHumanBodies == -1);

			foreach (Transform child in h.gameObject.transform)
            {
				if (child.gameObject.GetComponent<Renderer> () != null)
					child.gameObject.GetComponent<Renderer> ().enabled = (showHumanBodies == -1);
			}

			foreach (SensorBody b in h.bodies)
            {
				b.gameObject.GetComponent<Renderer> ().enabled = (showHumanBodies == h.ID);
			}
		}
	}

    public void setHumansVisibility(bool invisible)
    {
        foreach (Human h in _humans.Values)
        {
            h.gameObject.SetActive(!invisible);
        }
    }

    private void mergeHumans ()
	{
		List<SensorBody> alone_bodies = new List<SensorBody> ();

		// refresh existing bodies
		foreach (Sensor s in Sensors.Values)
        {
			if (s.Active) {
				foreach (KeyValuePair<string, SensorBody> sensorBody in s.bodies)
                {
					bool alone = true;

					foreach (KeyValuePair<int, Human> h in _humans)
                    {
						foreach (SensorBody humanBody in h.Value.bodies)
                        {
							if (sensorBody.Value.sensorID == humanBody.sensorID && sensorBody.Value.ID == humanBody.ID) {
								humanBody.LocalPosition = sensorBody.Value.LocalPosition;
								humanBody.lastUpdated = sensorBody.Value.lastUpdated;
								humanBody.updated = sensorBody.Value.updated;

								alone = false;
								break;
							}
						}

						if (!alone)
							break;
					}

					if (alone)
						alone_bodies.Add (sensorBody.Value);
				}
			}
		}

		// refresh existing humans
		foreach (KeyValuePair<int, Human> h in _humans)
        {
			Vector3 position = new Vector3 ();
			int numberOfBodies = 0;
			List<SensorBody> deadBodies = new List<SensorBody> ();

			foreach (SensorBody b in h.Value.bodies)
            {
				if (b.updated && Sensors [b.sensorID].Active)
					position = (position * (float)numberOfBodies++ + b.WorldPosition) / (float)numberOfBodies;
				else
					deadBodies.Add (b);
			}

			foreach (SensorBody b in deadBodies)
            {
				h.Value.bodies.Remove (b);
			}

			if (h.Value.bodies.Count == 0)
            {
				h.Value.timeOfDeath = DateTime.Now;
				_deadHumans.Add (h.Value);
			}
            else
            {
				h.Value.Position = position;
			}
		}
		foreach (Human h in _deadHumans)
        {
			_humans.Remove (h.ID);
		}

		// new bodies
		foreach (SensorBody b in alone_bodies)
        {
			bool hasHuman = false;

			// try to fit in existing humans
			foreach (KeyValuePair<int, Human> h in _humans)
            {
				if (calcHorizontalDistance (b.WorldPosition, h.Value.Position) < TrackerProperties.Instance.mergeDistance)
                {
					h.Value.Position = (h.Value.Position * (float)h.Value.bodies.Count + b.WorldPosition) / (float)(h.Value.bodies.Count + 1);
					h.Value.bodies.Add (b);
					hasHuman = true;
					break;
				}
			}

			if (!hasHuman) {
				// try to fit in dead humans
				foreach (Human h in _deadHumans)
                {
					if (calcHorizontalDistance (b.WorldPosition, h.Position) < TrackerProperties.Instance.mergeDistance)
                    {
						h.Position = (h.Position * (float)h.bodies.Count + b.WorldPosition) / (float)(h.bodies.Count + 1);
						h.bodies.Add (b);
						hasHuman = true;
						break;
					}
				}

				if (!hasHuman)
                {
					// create new human
					Human h = new Human ((GameObject)Instantiate (Resources.Load ("Prefabs/Human")), this);

					h.bodies.Add (b);
					h.Position = b.WorldPosition;

					_humans [h.ID] = h;
				}
			}
		}

		// bring back to life selected dead humans
		List<Human> undeadHumans = new List<Human> ();
		foreach (Human h in _deadHumans)
        {
			if (h.bodies.Count > 0)
            {
				_humans [h.ID] = h;
				undeadHumans.Add (h);
			}
		}
		foreach (Human h in undeadHumans)
        {
			_deadHumans.Remove (h);
		}

		// merge humans
		List<Human> mergedHumans = new List<Human> ();
		foreach (KeyValuePair<int, Human> h1 in _humans)
        {
			foreach (KeyValuePair<int, Human> h2 in _humans)
            {
				if (h1.Value.ID != h2.Value.ID && !mergedHumans.Contains (h2.Value))
                {
					if (calcHorizontalDistance (h1.Value.Position, h2.Value.Position) < TrackerProperties.Instance.mergeDistance)
                    {
						Vector3 position = (h1.Value.Position * (float)h1.Value.bodies.Count + h2.Value.Position * (float)h2.Value.bodies.Count) / (float)(h1.Value.bodies.Count + h2.Value.bodies.Count);

						if (h1.Value.ID < h2.Value.ID)
                        {
							h1.Value.Position = position;
							foreach (SensorBody b in h2.Value.bodies)
                            {
								h1.Value.bodies.Add (b);
							}
							mergedHumans.Add (h2.Value);
						}
                        else
                        {
							h2.Value.Position = position;
							foreach (SensorBody b in h1.Value.bodies)
                            {
								h2.Value.bodies.Add (b);
							}
							mergedHumans.Add (h1.Value);
						}
						break;
					}
				}
			}
		}
		foreach (Human h in mergedHumans)
        {
			_humansToKill.Add (h);
			_humans.Remove (h.ID);
		}
	}

    private float calcHorizontalDistance (Vector3 a, Vector3 b)
	{
		Vector3 c = new Vector3 (a.x, 0, a.z);
		Vector3 d = new Vector3 (b.x, 0, b.z);
		return Vector3.Distance (c, d);
	}

	internal void addUnicast (string address, string port)
	{
		_udpBroadcast.addUnicast (address, int.Parse (port));
	}

	internal void removeUnicast (string key)
	{
		_udpBroadcast.removeUnicast (key);
	}

    //FOR TCP
    internal void setNewCloud(string KinectID, byte[] data, int size, uint id)
    {

        // tirar o id da mensagem que é um int
        
        if (Sensors.ContainsKey(KinectID))
        {
            Sensors[KinectID].lastCloud.setPoints(data, 0, id, size);
            Sensors[KinectID].lastCloud.setToView();
        }
    }

    internal void setCloudToView(string KinectID)
    {
        Sensors[KinectID].lastCloud.setToView();
    }

    internal void setNewCloud (CloudMessage cloud)
	{
        //if (!Sensors.ContainsKey (cloud.KinectId)) {
        //	Vector3 position = new Vector3 (Mathf.Ceil (Sensors.Count / 2.0f) * (Sensors.Count % 2 == 0 ? -1.0f : 1.0f), 1, 0);
        //  Sensors [cloud.KinectId] = new Sensor (cloud.KinectId, (GameObject)Instantiate (Resources.Load ("Prefabs/KinectSensorPrefab"), position, Quaternion.identity));
        //  }
        //  Sensors[cloud.KinectId].lastCloudupdateCloud (cloud);
        int step = cloud.headerSize+3; // TMA: Size in bytes of heading: "CloudMessage" + L0 + 2 * L1. Check the UDPListener.cs from the Client.
        string[] pdu = cloud.message.Split(MessageSeparators.L1);
       
        string KinectId = pdu[0]; 
        uint  id = uint.Parse(pdu[1]);
        step += pdu[0].Length + pdu[1].Length;

        if (Sensors.ContainsKey(KinectId))
        {
            if (pdu[2] == "") {
                Sensors[KinectId].lastCloud.setToView();
            }
            else { 
                Sensors[KinectId].lastCloud.setPoints(cloud.receivedBytes,step,id,cloud.receivedBytes.Length);
            }
        }
	}

	internal void setNewFrame (BodiesMessage bodies)
	{
		if (!Sensors.ContainsKey (bodies.KinectId)) {
			Vector3 position = new Vector3 (Mathf.Ceil (Sensors.Count / 2.0f) * (Sensors.Count % 2 == 0 ? -1.0f : 1.0f), 1, 0);

			Sensors [bodies.KinectId] = new Sensor (bodies.KinectId, (GameObject)Instantiate (Resources.Load ("Prefabs/KinectSensorPrefab"), position, Quaternion.identity));
		}

		Sensors [bodies.KinectId].lastBodiesMessage = bodies;
	}

	internal bool CalibrationStep1 ()
	{
		bool cannotCalibrate = false;
		foreach (Sensor sensor in _sensors.Values) {
			if (sensor.Active) {
				if (sensor.lastBodiesMessage != null && sensor.lastBodiesMessage.Bodies.Count == 1) {
					sensor.calibrationStep1 ();
				} else
					cannotCalibrate = true;
			}
		}

		if (cannotCalibrate) {
			DoNotify n = gameObject.GetComponent<DoNotify> ();
			n.notifySend (NotificationLevel.IMPORTANT, "Calibration error", "Incorrect user placement!", 5000);
		}

		return !cannotCalibrate;
	}

	internal void CalibrationStep2 ()
	{

		Vector3 avgCenter = new Vector3 ();
		int sensorCount = 0;

		foreach (Sensor sensor in _sensors.Values) {
			if (sensor.lastBodiesMessage != null && sensor.lastBodiesMessage.Bodies.Count == 1 && sensor.Active) {
				sensor.calibrationStep2 ();

				avgCenter += sensor.pointSensorToScene (sensor.CalibAuxPoint);
				sensorCount += 1;
			}
		}

		avgCenter /= sensorCount;

		foreach (Sensor sensor in _sensors.Values)
        {
			if (sensor.lastBodiesMessage.Bodies.Count == 1 && sensor.Active)
            {
				sensor.move (avgCenter - sensor.pointSensorToScene (sensor.CalibAuxPoint));   
			}
		}

		_saveConfig ();

		DoNotify n = gameObject.GetComponent<DoNotify> ();
		n.notifySend (NotificationLevel.INFO, "Calibration complete", "Config file updated", 5000);
	}

	internal void CalibrationStep3 ()
	{
		foreach (Sensor sensor in _sensors.Values)
        {
			if (sensor.lastBodiesMessage != null && sensor.lastBodiesMessage.Bodies.Count == 1 && sensor.Active)
            {
				sensor.calibrationStep3 ();
			}
		}
	}

	internal void CalibrationStep4 ()
	{
		foreach (Sensor sensor in _sensors.Values)
        {
			if (sensor.lastBodiesMessage != null && sensor.lastBodiesMessage.Bodies.Count == 1 && sensor.Active)
            {
				sensor.calibrationStep4 ();
			}
		}

		_saveConfig ();

		DoNotify n = gameObject.GetComponent<DoNotify> ();
		n.notifySend (NotificationLevel.INFO, "Calibration complete", "Config file updated", 5000);
	}

    internal Vector3 getHandScreenSpace(int id, HandScreenSpace type)
    {
        Human h = _humans[id];
        SensorBody s = h.bodies[0];

        return s.skeleton.handScreenPositions[type];
    }

    internal string getHandState(int id, BodyPropertiesTypes type)
    {
        Human h = _humans[id];
        SensorBody s = h.bodies[0];

        return s.skeleton.bodyProperties[type];
    }

    internal Vector3 getJointPosition (int id, JointType joint, Vector3 garbage)
	{
		Human h = _humans [id];
		SensorBody bestBody = h.bodies [0];
		int confidence = bestBody.Confidence;
		int lastSensorConfidence = 0;
		SensorBody lastSensorBody = null;

		foreach (SensorBody b in h.bodies)
        {
			int bConfidence = b.Confidence;
			if (bConfidence > confidence)
            {
				confidence = bConfidence;
				bestBody = b;
			}

			if (b.sensorID == h.seenBySensor)
            {
				lastSensorConfidence = bConfidence;
				lastSensorBody = b;
			}
		}

		if (lastSensorBody == null || (bestBody.sensorID != h.seenBySensor && confidence > (lastSensorConfidence + 1)))
			h.seenBySensor = bestBody.sensorID;
		else
			bestBody = lastSensorBody;

		return _sensors [bestBody.sensorID].pointSensorToScene (CommonUtils.pointKinectToUnity (bestBody.skeleton.jointsPositions [joint]));
	}

	internal bool humanHasBodies (int id)
	{
		return _humans.ContainsKey (id) && _humans [id].bodies.Count > 0;
	}

	void OnGUI ()
	{
		//int n = 1;

		if (showHumanBodies == -1)
        {
			foreach (Human h in _humans.Values)
            {
				//GUI.Label(new Rect(10, Screen.height - (n++ * 50), 1000, 50), "Human " + h.ID + " as seen by " + h.seenBySensor);

				Vector3 p = Camera.main.WorldToScreenPoint (h.Skeleton.getHead () + new Vector3 (0, 0.2f, 0));
				if (p.z > 0) {
					GUI.Label (new Rect (p.x, Screen.height - p.y - 25, 100, 25), "" + h.ID);
				}
			}
		}

		foreach (Sensor s in Sensors.Values) {
			if (s.Active) {
				Vector3 p = Camera.main.WorldToScreenPoint (s.SensorGameObject.transform.position + new Vector3 (0, 0.05f, 0));
				if (p.z > 0) {
					GUI.Label (new Rect (p.x, Screen.height - p.y - 25, 100, 25), "" + s.SensorID);
				}
			}
		}
	}

	private void _saveConfig ()
	{
		string filePath = Application.dataPath + "/" + TrackerProperties.Instance.configFilename;
		ConfigProperties.clear (filePath);

		ConfigProperties.writeComment (filePath, "Config File created in " + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"));

		// save properties
		ConfigProperties.save (filePath, "udp.listenport", "" + TrackerProperties.Instance.listenPort);
		ConfigProperties.save (filePath, "udp.broadcastport", "" + TrackerProperties.Instance.broadcastPort);
        ConfigProperties.save (filePath, "udp.sensor.listener", "" + TrackerProperties.Instance.sensorListenPort);
        ConfigProperties.save (filePath, "udp.sendinterval", "" + TrackerProperties.Instance.sendInterval);
		ConfigProperties.save (filePath, "tracker.mergedistance", "" + TrackerProperties.Instance.mergeDistance);
		ConfigProperties.save (filePath, "tracker.confidencethreshold", "" + TrackerProperties.Instance.confidenceTreshold);

		// save sensors
		foreach (Sensor s in _sensors.Values)
        {
			if (s.Active)
            {
				Vector3 p = s.SensorGameObject.transform.position;
				Quaternion r = s.SensorGameObject.transform.rotation;
				ConfigProperties.save (filePath, "kinect." + s.SensorID, "" + s.SensorID + ";" + p.x + ";" + p.y + ";" + p.z + ";" + r.x + ";" + r.y + ";" + r.z + ";" + r.w);
			}
		}
	}

	private void _loadConfig ()
	{
		string filePath = Application.dataPath + "/" + TrackerProperties.Instance.configFilename;

		string port = ConfigProperties.load (filePath, "udp.listenport");
		if (port != "")
        {
			TrackerProperties.Instance.listenPort = int.Parse (port);
		}
		resetListening ();

		port = ConfigProperties.load (filePath, "udp.broadcastport");
		if (port != "")
        {
			TrackerProperties.Instance.broadcastPort = int.Parse (port);
		}
		resetBroadcast ();

        port = ConfigProperties.load(filePath, "udp.sensor.listener");
        if (port != "")
        {
            TrackerProperties.Instance.sensorListenPort = int.Parse(port);
        }

        string aux = ConfigProperties.load(filePath, "udp.sendinterval");
        if (aux != "")
        {
            TrackerProperties.Instance.sendInterval = int.Parse(aux);
        }

        aux = ConfigProperties.load (filePath, "tracker.mergedistance");
		if (aux != "")
        {
			TrackerProperties.Instance.mergeDistance = float.Parse (aux);
		}

		aux = ConfigProperties.load (filePath, "tracker.confidencethreshold");
		if (aux != "")
        {
			TrackerProperties.Instance.confidenceTreshold = int.Parse (aux);
		}
	}

	private void _loadSavedSensors ()
	{
		foreach (String line in ConfigProperties.loadKinects(Application.dataPath + "/" + TrackerProperties.Instance.configFilename))
        {
			string[] values = line.Split (';');

			string id = values [0];

			Vector3 position = new Vector3 (
				                   float.Parse (values [1].Replace (',', '.')),
				                   float.Parse (values [2].Replace (',', '.')),
				                   float.Parse (values [3].Replace (',', '.')));

			Quaternion rotation = new Quaternion (
				                      float.Parse (values [4].Replace (',', '.')),
				                      float.Parse (values [5].Replace (',', '.')),
				                      float.Parse (values [6].Replace (',', '.')),
				                      float.Parse (values [7].Replace (',', '.')));

			Sensors [id] = new Sensor (id, (GameObject)Instantiate (Resources.Load ("Prefabs/KinectSensorPrefab"), position, rotation));
		}
	}

	public void resetBroadcast ()
	{
		_udpBroadcast.reset (TrackerProperties.Instance.broadcastPort);
	}

	public void resetListening ()
	{
		gameObject.GetComponent<UdpListener> ().udpRestart ();
	}

	public void Save ()
	{
		_saveConfig ();
	}

	public void hideAllClouds ()
	{
		foreach (Sensor s in _sensors.Values)
        {
			s.lastCloud.hideFromView ();
		}
		UdpClient udp = new UdpClient ();
		string message = CloudMessage.createRequestMessage (2,Network.player.ipAddress, TrackerProperties.Instance.listenPort); 
		byte[] data = Encoding.UTF8.GetBytes(message);
		IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, TrackerProperties.Instance.sensorListenPort);
		udp.Send(data, data.Length, remoteEndPoint);
	}

	public void broadCastCloudRequests (bool continuous)
	{
		UdpClient udp = new UdpClient ();
		string message = CloudMessage.createRequestMessage (continuous ? 1 : 0, Network.player.ipAddress, TrackerProperties.Instance.listenPort); 
		byte[] data = Encoding.UTF8.GetBytes (message);
		IPEndPoint remoteEndPoint = new IPEndPoint (IPAddress.Broadcast, TrackerProperties.Instance.sensorListenPort);
		udp.Send (data, data.Length, remoteEndPoint);
	}

    public void processAvatarMessage(AvatarMessage av)
    {
        UdpClient udp = new UdpClient();
       
        // Calibration
        string message = av.createCalibrationMessage(_sensors);
        byte[] data = Encoding.UTF8.GetBytes(message);
        IPEndPoint remoteEndPoint = new IPEndPoint(av.replyIPAddress, av.port);
        Debug.Log("Sent reply with calibration data " + message);
        udp.Send(data, data.Length, remoteEndPoint);
        
        // Broadcast
        string message2 = CloudMessage.createRequestMessage(av.mode, av.replyIPAddress.ToString(), av.port);
        byte[] data2 = Encoding.UTF8.GetBytes(message2);
        IPEndPoint remoteEndPoint2 = new IPEndPoint(IPAddress.Broadcast, TrackerProperties.Instance.sensorListenPort);
        udp.Send(data2, data2.Length, remoteEndPoint2);
        Debug.Log("Forwarded request to clients " + message2);
    }

    public void processSurfaceMessage(SurfaceMessage sm)
    {
        UdpClient udp = new UdpClient();
        string message = sm.createSurfaceMessage(_surfaces);
        message += MessageSeparators.L0 + _calibrationToMessage();
        byte[] data = Encoding.UTF8.GetBytes(message);
        IPEndPoint remoteEndPoint = new IPEndPoint(sm.replyIPAddress, sm.port);
        Debug.Log("Sent reply with surface's data " + message);
        udp.Send(data, data.Length, remoteEndPoint);
    }

    private string _calibrationToMessage()
    {
        string ret = "";
        foreach (Sensor s in _sensors.Values)
        {
            if (s.Active)
            {
                Vector3 p = s.SensorGameObject.transform.position;
                Quaternion r = s.SensorGameObject.transform.rotation;
                char c = MessageSeparators.L3;
                ret += s.SensorID + c + p.x + c + p.y + c + p.z + c + r.x + c + r.y + c + r.z + c + r.w + MessageSeparators.L2;
            }
        }
        return ret;
    }

    internal void LoadSurfaces()
    {
        _surfaces = new List<Surface>(Surface.loadSurfaces("Surfaces"));
        foreach (Surface s in _surfaces)
        {
            if (_sensors.ContainsKey(s.sensorid))
            {
                s.surfaceGO = __createSurfaceGos(s.name, Vector3.zero, _sensors[s.sensorid].SensorGameObject.transform);
                GameObject bl = __createSurfaceGos("bl", s.BottomLeft, s.surfaceGO.transform);
                GameObject br = __createSurfaceGos("br", s.BottomRight, s.surfaceGO.transform);
                GameObject tl = __createSurfaceGos("tl", s.TopLeft, s.surfaceGO.transform);
                GameObject tr = __createSurfaceGos("tr", s.TopRight, s.surfaceGO.transform);


                gameObject.GetComponent<DoNotify>().notifySend(NotificationLevel.INFO, "New Surface", "Surface " + s.name + " added", 5000);

                s.saveSurface(bl, br, tl, tr);

                MeshFilter meshFilter = (MeshFilter)s.surfaceGO.AddComponent(typeof(MeshFilter));
                Mesh m = new Mesh();
                m.name = s.name + "MESH";
                m.vertices = new Vector3[]
                {
                    bl.transform.localPosition, br.transform.localPosition, tr.transform.localPosition, tl.transform.localPosition
                };

                m.triangles = new int[]
                {
                    0, 1, 3,
                    1, 2, 3,
                    3, 1, 0,
                    3, 2, 1
                };
                m.RecalculateNormals();
                m.RecalculateBounds();

                meshFilter.mesh = m;
                MeshRenderer renderer = s.surfaceGO.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                renderer.material = SurfaceMaterial;
            }
        }
    }

    internal GameObject __createSurfaceGos(string name, Vector3 position, Transform parent)
    {
        GameObject g = new GameObject();
        g.transform.parent = parent;
        g.transform.localRotation = Quaternion.identity;
        g.name = name + "lol";
        g.transform.localPosition = CommonUtils.pointKinectToUnity(position); 
        return g;
    }
}