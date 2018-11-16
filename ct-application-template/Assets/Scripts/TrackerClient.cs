using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class TrackerClient : MonoBehaviour {

	private Dictionary<string, Human> _humans;
    
    //example
    GameObject _righthand;
    GameObject _head;

    void Start () {
		_humans = new Dictionary<string, Human>();
        //example
        _righthand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _righthand.name = "Right Hand";
        _righthand.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _head.name = "Head";
        _head.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    void Update () {

        //Example of how to iterate the humans.
		foreach (Human h in _humans.Values)
		{
			// get human properties:
			string id = h.id;

            // get human joints positions:
            _righthand.transform.position = h.body.Joints[BodyJointType.head];
            _head.transform.position = h.body.Joints[BodyJointType.rightHand];

            //just because it's an example :)
            break;
        }

		// finally
		_cleanDeadHumans();
	}

	public void setNewFrame (Body[] bodies)
    {
        foreach (Body b in bodies)
		{
			try
			{
			string bodyID = b.Properties[BodyPropertiesType.UID];
			if (!_humans.Keys.Contains(bodyID))
			{
				_humans.Add(bodyID, new Human());
			}
			_humans[bodyID].Update(b);
			}
			catch (Exception) { }
		}
	}

	void _cleanDeadHumans ()
	{
		List<Human> deadhumans = new List<Human>();

		foreach (Human h in _humans.Values)
		{
			if (DateTime.Now > h.lastUpdated.AddMilliseconds(1000))
				deadhumans.Add(h);
		}

		foreach (Human h in deadhumans)
		{
			_humans.Remove(h.id);
		}
	}

	void OnGUI()
	{
		GUI.Label(new Rect(10, 10, 200, 35), "Number of users: " + _humans.Count);
	}
}
