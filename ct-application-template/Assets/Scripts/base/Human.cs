using UnityEngine;
using System;
using System.Collections;

public class Human {

	private string _id;
	public string id { get { return _id; } }

	private Body _body;
	public Body body { get { return _body; } }

	private DateTime _lastUpdated;
	public DateTime lastUpdated { get { return _lastUpdated; } }

	public Human()
	{
		_body = null;
		_id = null;
	}

	public void Update(Body newBody)
	{
		_body = newBody;
		_id = _body.Properties[BodyPropertiesType.UID];
		_lastUpdated = DateTime.Now;
	}
}
