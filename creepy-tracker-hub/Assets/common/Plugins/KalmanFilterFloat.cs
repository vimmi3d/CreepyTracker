using UnityEngine;

public class KalmanFilterFloat
{
	private static float _gain;
	public static float Gain
	{
		get 
		{
			return _gain;
		}

		set 
		{
			_gain = Mathf.Min(0, Mathf.Max(1, value));
		}
	}

	private float _value;
	public float Value
	{
		get { return _value; }
		set 
		{ 
			_value = (float) this.update(value); 
		}
	}

	public KalmanFilterFloat()
	{
		_gain = 0.5f;
		_value = float.NegativeInfinity;
	}

	private double update(float newValue)
	{
		if (float.IsNegativeInfinity(_value))
		{
			return newValue;
		}
		else
		{
			return _value + Gain * (newValue - _value);
		}
	}
}

public class KalmanFilterFloatOriginal
{
	private float _value;
	
	public float Value
	{
		get { return _value; }
		set 
		{ 
			_value = (float) this.update(value); 
		}
	}
	
	private double Q;
	private double R;
	private double P;
	private double X;
	private double K;
	
	public KalmanFilterFloatOriginal()
	{
		_value = 0f;
		
		//Q = 0.00001;
		Q = 0.01;
		R = 0.1;
		P = 1;
		X = 0;
	}
	
	private void measurementUpdate()
	{
		K = (P + Q) / (P + Q + R);
		P = R * K;
	}
	
	private double update(double measurement)
	{
		measurementUpdate();
		double result = X + (measurement - X) * K;
		X = result;
		return result;
	}	
}