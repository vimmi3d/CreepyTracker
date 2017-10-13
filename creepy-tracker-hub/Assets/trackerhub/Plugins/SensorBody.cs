using UnityEngine;
using System;

public class SensorBody
{
    public string ID;
    public string sensorID;
    public DateTime lastUpdated;
    private Vector3 position;
    public Skeleton skeleton;
    public GameObject gameObject;
    public bool updated;
    public Vector3 LocalPosition
    {
        get
        {
            return position;
        }

        set
        {
            position = value;
            gameObject.transform.localPosition = position;
        }
    }
    public Vector3 WorldPosition
    { get { return gameObject.transform.position; } }

    public int Confidence
    { get { return int.Parse(skeleton.bodyProperties[BodyPropertiesTypes.Confidence]); } }

    public SensorBody(string ID, Transform parent)
    {
        this.ID = ID;
        gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        gameObject.GetComponent<CapsuleCollider>().enabled = false;
        gameObject.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
        gameObject.name = this.ID;
        gameObject.transform.parent = parent;
        gameObject.GetComponent<Renderer>().enabled = false;
    }
}
