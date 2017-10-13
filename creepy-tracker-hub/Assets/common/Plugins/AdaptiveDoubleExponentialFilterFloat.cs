using UnityEngine;

public class AdaptiveDoubleExponentialFilterFloat
{
	// as described in: msdn.microsoft.com/en-us/library/jj131429.aspx

	private static float deltaLow = 0.02f;
	private static float deltaHigh = 0.05f;
	private static float gainLow = 0.1f;
	private static float gainHigh = 0.15f;
	private static float trendGain = 0.1f;

	public static bool filtering = true;

	public static float DeltaLow {
		get { return deltaLow; }
		set { deltaLow = value; }
	}

	public static float DeltaHigh {
		get { return deltaHigh; }
		set { deltaHigh = value; }
	}

	public static float GainLow {
		get { return gainLow; }
		set { gainLow = Mathf.Clamp (value, 0.0f, 1.0f); }
	}

	public static float GainHigh {
		get { return gainHigh; }
		set { gainHigh = Mathf.Clamp (value, 0.0f, 1.0f); }
	}

	public static float TrendGain {
		get { return trendGain; }
		set { trendGain = Mathf.Clamp (value, 0.0f, 1.0f); }
	}

	private float filteredValue;

	public float Value {
		get { return filteredValue; }
		set { filteredValue = Update (value); }
	}

	private float lastValue;
	private float trend;

	public AdaptiveDoubleExponentialFilterFloat ()
	{
		filteredValue = float.NaN;
		lastValue = float.NaN;
		trend = 0.0f;
	}

	private float Update (float newValue)
	{
		if (float.IsNaN (filteredValue) || !filtering) {
			lastValue = newValue;
			return newValue;
		} else {
			float gain;
			float delta = Mathf.Abs (newValue - lastValue);

			if (delta <= deltaLow) { // Low velocity
				gain = gainLow;
			} else if (delta >= deltaHigh) { // High velocity
				gain = gainHigh;
			} else { // Medium velocity
				gain = gainHigh + ((delta - deltaHigh) / (deltaLow - deltaHigh)) * (gainLow - gainHigh);
			}

			float newFilteredValue = gain * newValue + (1 - gain) * (filteredValue + trend);
			trend = trendGain * (newFilteredValue - filteredValue) + (1 - trendGain) * trend;

			lastValue = newValue;
			return newFilteredValue;
		}
	}
}