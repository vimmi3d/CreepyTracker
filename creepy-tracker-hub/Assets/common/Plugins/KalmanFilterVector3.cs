using UnityEngine;

public class KalmanFilterVector3
{
	private KalmanFilterFloat _x;
	private KalmanFilterFloat _y;
	private KalmanFilterFloat _z;

	public Vector3 Value
	{
		get 
		{ 
			return new Vector3(_x.Value, _y.Value, _z.Value);
		}
		set 
		{ 
			_update(value); 
		}
	}

	public KalmanFilterVector3()
	{
		_x = new KalmanFilterFloat();
		_y = new KalmanFilterFloat();
		_z = new KalmanFilterFloat();
	}

	private void _update(Vector3 v)
	{
		_x.Value = v.x;
		_y.Value = v.y;
		_z.Value = v.z;
	}
}